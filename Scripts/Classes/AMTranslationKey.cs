using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMTranslationKey : AMKey {
    public const int SUBDIVISIONS_MULTIPLIER = 16;

    public enum Interpolation {
        Curve = 0,
        Linear = 1
    }
    public static string[] InterpolationNames = new string[] { "Curve", "Linear" };
    public Vector3 position;
    public int interp = 0;			// interpolation

    public int startFrame;
    public int endFrame;
    public bool isLocal;
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
                mPathPreview = new AMPathPreview((Interpolation)interp == Interpolation.Curve ? PathType.Curved : PathType.Linear, pts);

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
    public override int getStartFrame() {
        return startFrame;
    }

    public override int getNumberOfFrames(int frameRate) {
        if(easeType == EaseTypeNone && (endFrame == -1 || endFrame == startFrame))
            return 1;
        else if(endFrame == -1)
            return -1;
        return  endFrame - startFrame;
    }
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
        int frameRate = seq.take.frameRate;

        //allow tracks with just one key
        if(track.keys.Count == 1)
            easeType = EaseTypeNone;

        if(easeType == EaseTypeNone) {
            //TODO: world position
            seq.Insert(new AMActionTransLocalPos(this, frameRate, obj as Transform, position));
        }
        else {
            if(path.Length <= 1) return;
            if(getNumberOfFrames(seq.take.frameRate) <= 0) return;

            object tweenTarget = obj;
            string tweenProp = isLocal ? "localPosition" : "position";

            Tweener ret = null;

            if(hasCustomEase()) {
                if(path.Length == 2)
                    ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false, PathType.Linear)).Ease(easeCurve));
                else {
                    PlugVector3Path p = new PlugVector3Path(path, false);
                    p.ClosePath(isClosed);
                    ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, p).Ease(easeCurve));
                }
            }
            else {
                if(path.Length == 2)
                    ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false, PathType.Linear)).Ease((EaseType)easeType, amplitude, period));
                else {
                    PlugVector3Path p = new PlugVector3Path(path, false);
                    p.ClosePath(isClosed);
                    ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, p).Ease((EaseType)easeType, amplitude, period));
                }
            }

            seq.Insert(this, ret);
        }
    }
    #endregion
}
