using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;

namespace MateAnimator{
	[AddComponentMenu("")]
	public class AMTranslationKey : AMKey {
        public const int pathResolution = 10;
	    public const int SUBDIVISIONS_MULTIPLIER = 16;

	    public Vector3 position;
	    
	    public int endFrame;
	    public Vector3[] path;

	    public bool isConstSpeed = true;
	        
	    public bool isClosed { get { return path[0] == path[path.Length - 1]; } }

	    private AMPathPreview mPathPreview;

	    public AMPathPreview pathPreview {
	        get {
	            if(mPathPreview == null) {
	                int indMod = 1;
	                int pAdd = isClosed ? 1 : 0;
	                int len = path.Length;

	                Vector3[] pts = new Vector3[len + 2 + pAdd];
	                for (int i = 0; i < len; ++i)
	                    pts[i + indMod] = path[i];

	                len = pts.Length;

	                if(isClosed) {
	                    // Close path.
	                    pts[len - 2] = pts[1];
	                }

	                // Add control points.
	                if(isClosed) {
	                    pts[0] = pts[len - 3];
	                    pts[len - 1] = pts[2];
	                }
	                else {
	                    pts[0] = pts[1];
	                    Vector3 lastP = pts[len - 2];
	                    Vector3 diffV = lastP - pts[len - 3];
	                    pts[len - 1] = lastP + diffV;
	                }

	                // Create the path.
	                mPathPreview = new AMPathPreview((Interpolation)interp == Interpolation.Curve ? PathType.CatmullRom : PathType.Linear, pts);

	                // Store arc lengths tables for constant speed.
	                mPathPreview.StoreTimeToLenTables(mPathPreview.path.Length * SUBDIVISIONS_MULTIPLIER);
	            }

	            return mPathPreview;
	        }

	        set { mPathPreview = value; }
	    }

	    public Vector3 GetPoint(float t) {
	        return isConstSpeed ? pathPreview.GetConstPoint(t) : pathPreview.GetPoint(t);
	    }

	    // copy properties from key
	    public override void CopyTo(AMKey key) {
			AMTranslationKey a = key as AMTranslationKey;
	        a.enabled = false;
	        a.frame = frame;
	        a.position = position;
	        a.interp = interp;
	        a.easeType = easeType;
	        a.customEase = new List<float>(customEase);
	        a.isConstSpeed = isConstSpeed;
	    }

	    #region action

	    //for pixel-snapping
        public class PlugVector3PathSnap : PathPlugin {
            public float unitConv;

            public PlugVector3PathSnap(float aUnitConv) { unitConv = aUnitConv; }

            public override void EvaluateAndApply(PathOptions options, Tween t, bool isRelative, DOGetter<Vector3> getter, DOSetter<Vector3> setter, float elapsed, Path startValue, Path changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
                if(t.loopType == LoopType.Incremental && !options.isClosedPath) {
                    int increment = (t.isComplete ? t.completedLoops - 1 : t.completedLoops);
                    if(increment > 0) changeValue = changeValue.CloneIncremental(increment);
                }

                float pathPerc = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
                float constantPathPerc = changeValue.ConvertToConstantPathPerc(pathPerc);
                Vector3 newPos = changeValue.GetPoint(constantPathPerc);

                newPos.Set(Mathf.Round(newPos.x*unitConv)/unitConv, Mathf.Round(newPos.y*unitConv)/unitConv, Mathf.Round(newPos.z*unitConv)/unitConv);

                changeValue.targetPosition = newPos; // Used to draw editor gizmos
                setter(newPos);

                if(options.mode != PathMode.Ignore && options.orientType != OrientType.None) SetOrientation(options, t, changeValue, constantPathPerc, newPos, updateNotice);

                // Determine if current waypoint changed and eventually dispatch callback
                bool isForward = !usingInversePosition;
                if(t.isBackwards) isForward = !isForward;
                int newWaypointIndex = changeValue.GetWaypointIndexFromPerc(pathPerc, isForward);
                if(newWaypointIndex != t.miscInt) {
                    t.miscInt = newWaypointIndex;
                    if(t.onWaypointChange != null) Tween.OnTweenCallback(t.onWaypointChange, newWaypointIndex);
                }
            }
	    }

	    public override int getNumberOfFrames(int frameRate) {
	        if(!canTween && (endFrame == -1 || endFrame == frame))
	            return 1;
	        else if(endFrame == -1)
	            return -1;
	        return endFrame - frame;
	    }
	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
	        int frameRate = seq.take.frameRate;

	        //allow tracks with just one key
	        if(track.keys.Count == 1)
	            interp = (int)Interpolation.None;

	        AMTranslationTrack tTrack = track as AMTranslationTrack;
	        bool pixelSnap = tTrack.pixelSnap;
	        float ppu = tTrack.pixelPerUnit;

	        if(!canTween) {
	            //TODO: world position
	            seq.Insert(new AMActionTransLocalPos(this, frameRate, obj as Transform, pixelSnap ? new Vector3(Mathf.Round(position.x*ppu)/ppu, Mathf.Round(position.y*ppu)/ppu, Mathf.Round(position.z*ppu)/ppu) : position));
	        }
	        else {
	            if(path.Length <= 1) return;
	            if(getNumberOfFrames(seq.take.frameRate) <= 0) return;

	            Tweener ret = null;

                Transform trans = obj as Transform;
	            bool isRelative = false;

                PathType pathType = path.Length == 2 ? PathType.Linear : PathType.CatmullRom;

                if(pixelSnap)
                    ret = DOTween.To(new PlugVector3PathSnap(ppu), () => trans.localPosition, x => trans.localPosition=x, new Path(pathType, path, pathResolution), getTime(frameRate)).SetRelative(isRelative).SetOptions(isClosed);
                else
                    ret = trans.DOLocalPath(path, getTime(frameRate), pathType, PathMode.Full3D, pathResolution, null).SetRelative(isRelative).SetOptions(isClosed);
                            
                if(hasCustomEase())
                    ret.SetEase(easeCurve);
                else
                    ret.SetEase((Ease)easeType, amplitude, period);
                
	            seq.Insert(this, ret);
	        }
	    }
	    #endregion
	}
}