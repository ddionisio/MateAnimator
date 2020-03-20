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

namespace M8.Animator {
    [System.Serializable]
    public class TranslationKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Translation; } }

        public const int pathResolution = 10;

        public Vector3 position;

        public int endFrame;
        public Vector3[] path;

        public bool isConstSpeed = true;

        public bool isClosed { get { return path.Length > 1 && path[0] == path[path.Length - 1]; } }

        private TweenerCore<Vector3, Path, PathOptions> mPathPreviewTween;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(TranslationTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    path = new Vector3[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    if(keyInd + 1 < track.keys.Count) {
                        path = new Vector3[2];

                        path[0] = position;

                        var nextKey = (TranslationKey)track.keys[keyInd + 1];

                        path[1] = nextKey.position;

                        endFrame = nextKey.frame;
                    }
                    else { //fail-safe
                        path = new Vector3[0];
                        endFrame = frame;
                    }
                    break;

                case Interpolation.Curve:
                    var pathList = new List<Vector3>();

                    for(int i = keyInd; i < track.keys.Count; i++) {
                        var key = (TranslationKey)track.keys[i];

                        pathList.Add(key.position);
                        endFrame = key.frame;

                        if(key.interp != Interpolation.Curve)
                            break;
                    }

                    if(pathList.Count > 1)
                        path = pathList.ToArray();
                    else
                        path = new Vector3[0];
                    break;
            }
        }

        /// <summary>
        /// Check if all points of path are equal.
        /// </summary>
        bool IsPathPointsEqual() {
            for(int i = 0; i < path.Length; i++) {
                if(i > 0 && path[i - 1] != path[i])
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Create the tween based on the path, there must at least be two points
        /// </summary>
        TweenerCore<Vector3, Path, PathOptions> CreatePathTween(int frameRate) {
            //if all points are equal, then set to Linear to prevent error from DOTween
            var pathType = path.Length == 2 || IsPathPointsEqual() ? PathType.Linear : PathType.CatmullRom;

            var pathData = new Path(pathType, path, pathResolution);

            var tween = DOTween.To(PathPlugin.Get(), _Getter, _SetterNull, pathData, getTime(frameRate));

            tween.SetRelative(false).SetOptions(isClosed);

            return tween;
        }

        Vector3 _Getter() { return position; }

        void _SetterNull(Vector3 val) { }

        public void ClearCache() {
            if(mPathPreviewTween != null) {
                mPathPreviewTween.Kill();
                mPathPreviewTween = null;
            }
        }

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Vector3 GetPoint(Transform transform, int frameRate, float t) {
            if((mPathPreviewTween == null || !mPathPreviewTween.active) && path.Length > 1)
                mPathPreviewTween = CreatePathTween(frameRate);

            if(mPathPreviewTween == null) //not tweenable
                return position;

            if(mPathPreviewTween.target == null) //this is just a placeholder to prevent error exception
                mPathPreviewTween.SetTarget(transform);

            if(!mPathPreviewTween.IsInitialized())
                mPathPreviewTween.ForceInit();

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return position;
            }

            return mPathPreviewTween.PathGetPoint(finalT);
        }

        public void DrawGizmos(Transform transform, float ptSize, int frameRate) {
            Matrix4x4 mtx = transform.parent ? transform.parent.localToWorldMatrix : Matrix4x4.identity;

            //draw path
            switch(interp) {
                case Interpolation.None:
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(mtx.MultiplyPoint3x4(position), ptSize);
                    break;

                case Interpolation.Linear:
                    if(path.Length <= 0)
                        return;
                    else if(path.Length == 1) {
                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(mtx.MultiplyPoint3x4(position), ptSize);
                    }
                    else {
                        Vector3 pt1 = mtx.MultiplyPoint3x4(path[0]), pt2 = mtx.MultiplyPoint3x4(path[1]);

                        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

                        Gizmos.DrawLine(pt1, pt2);

                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(pt1, ptSize);
                        Gizmos.DrawSphere(pt2, ptSize);
                    }
                    break;

                case Interpolation.Curve:
                    if(path.Length <= 0)
                        return;

                    if((mPathPreviewTween == null || !mPathPreviewTween.active) && path.Length > 1)
                        mPathPreviewTween = CreatePathTween(frameRate);

                    if(mPathPreviewTween == null)
                        return;

                    if(mPathPreviewTween.target == null)
                        mPathPreviewTween.SetTarget(transform);

                    if(!mPathPreviewTween.IsInitialized())
                        mPathPreviewTween.ForceInit();

                    Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

                    int subdivisions = pathResolution * path.Length;
                    for(int i = 0; i < subdivisions; i++) {
                        var pt1 = mPathPreviewTween.PathGetPoint(i / (float)subdivisions);
                        var pt2 = mPathPreviewTween.PathGetPoint((i + 1) / (float)subdivisions);

                        Gizmos.DrawLine(mtx.MultiplyPoint3x4(pt1), mtx.MultiplyPoint3x4(pt2));
                    }

                    Gizmos.color = Color.green;

                    for(int i = 0; i < path.Length; i++)
                        Gizmos.DrawSphere(mtx.MultiplyPoint3x4(path[i]), ptSize);
                    break;
            }
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            TranslationKey a = key as TranslationKey;

            a.position = position;

            a.isConstSpeed = isConstSpeed;
        }

        #region action
        
        public override int getNumberOfFrames(int frameRate) {
            if(!canTween && (endFrame == -1 || endFrame == frame))
                return 1;
            else if(endFrame == -1)
                return -1;
            return endFrame - frame;
        }
        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object obj) {
            int frameRate = seq.take.frameRate;

            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(path.Length <= 1) return;
                if(getNumberOfFrames(frameRate) <= 0) return;
            }

            var trans = obj as Transform;
            var transParent = trans.parent;

            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            var tTrack = track as TranslationTrack;
            bool pixelSnap = tTrack.pixelSnap;
            float ppu = tTrack.pixelPerUnit;

            if(!canTween) {
                //TODO: world position
                Vector3 pos = pixelSnap ? new Vector3(Mathf.Round(position.x * ppu) / ppu, Mathf.Round(position.y * ppu) / ppu, Mathf.Round(position.z * ppu) / ppu) : position;

                TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweener;
                                
                if(body2D)
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => trans.localPosition, (x) => body2D.position = transParent.TransformPoint(x), pos, getTime(frameRate)); //1.0f / frameRate
                else if(body)
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => trans.localPosition, (x) => body.position = transParent.TransformPoint(x), pos, getTime(frameRate)); //1.0f / frameRate
                else
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => trans.localPosition, (x) => trans.localPosition = x, pos, getTime(frameRate)); //1.0f / frameRate

                seq.Insert(this, tweener);
            }
            else {
                var tween = CreatePathTween(frameRate);

                if(body2D) {
                    if(pixelSnap)
                        tween.setter = x => body2D.MovePosition(transParent.TransformPoint(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu)));
                    else
                        tween.setter = x => body2D.MovePosition(transParent.TransformPoint(x));

                    tween.SetTarget(body2D);
                }
                else if(body) {
                    if(pixelSnap)
                        tween.setter = x => body.MovePosition(transParent.TransformPoint(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu)));
                    else
                        tween.setter = x => body.MovePosition(transParent.TransformPoint(x));

                    tween.SetTarget(body);
                }
                else {
                    if(pixelSnap)
                        tween.setter = x => trans.localPosition = new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu);
                    else
                        tween.setter = x => trans.localPosition = x;

                    tween.SetTarget(trans);
                }

                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase(easeType, amplitude, period);

                seq.Insert(this, tween);
            }
        }
        #endregion
    }
}