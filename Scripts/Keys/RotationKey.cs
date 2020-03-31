using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using DG.Tweening.CustomPlugins;

namespace M8.Animator {
    [System.Serializable]
    public class RotationKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Rotation; } }

        public const int pathResolution = 10;

        //public int type = 0; // 0 = Rotate To, 1 = Look At
        public Quaternion rotation;

        public int endFrame;

        //curve-related TODO: use proper rotational spline
        public Vector3[] path; //euler angles

        public bool isClosed { get { return path.Length > 1 && path[0] == path[path.Length - 1]; } }

        private TweenerCore<Vector3, Path, PathOptions> mPathPreviewTween;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(RotationTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    path = new Vector3[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    path = new Vector3[0];

                    if(keyInd + 1 < track.keys.Count) {
                        var nextKey = (RotationKey)track.keys[keyInd + 1];
                        endFrame = nextKey.frame;
                    }
                    else { //fail-safe
                        endFrame = -1;
                    }
                    break;

                case Interpolation.Curve:
                    var pathList = new List<Vector3>();

                    for(int i = keyInd; i < track.keys.Count; i++) {
                        var key = (RotationKey)track.keys[i];

                        pathList.Add(key.rotation.eulerAngles);
                        endFrame = key.frame;

                        if(key.interp != Interpolation.Curve)
                            break;
                    }

                    if(pathList.Count > 1)
                        path = pathList.ToArray();
                    else {
                        endFrame = -1;
                        path = new Vector3[0];
                    }
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

            var tween = DOTween.To(PathPlugin.Get(), _PathGetter, _PathSetterNull, pathData, getTime(frameRate));

            tween.SetRelative(false).SetOptions(isClosed);

            return tween;
        }

        Vector3 _PathGetter() { return rotation.eulerAngles; }

        void _PathSetterNull(Vector3 val) { }

        public void ClearCache() {
            if(mPathPreviewTween != null) {
                mPathPreviewTween.Kill();
                mPathPreviewTween = null;
            }
        }

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Quaternion GetRotationFromPath(Transform transform, int frameRate, float t) {
            if((mPathPreviewTween == null || !mPathPreviewTween.active) && path.Length > 1)
                mPathPreviewTween = CreatePathTween(frameRate);

            if(mPathPreviewTween == null) //not tweenable
                return rotation;

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
                    return rotation;
            }

            return Quaternion.Euler(mPathPreviewTween.PathGetPoint(finalT));
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            RotationKey a = key as RotationKey;

            //a.type = type;
            a.rotation = rotation;
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
            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(endFrame == -1) return;
                if(interp == Interpolation.Curve && path.Length <= 1) return;
            }

            Transform trans = obj as Transform;
            var transParent = trans.parent;

            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;
            float time = getTime(frameRate);

            switch(interp) {
                case Interpolation.None:
                    TweenerCore<Quaternion, Quaternion, TWeenPlugNoneOptions> valueTween;

                    if(body2D)
                        valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => body2D.rotation = (x * transParent.rotation).eulerAngles.z, rotation, time);
                    else if(body)
                        valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => body.rotation = x * transParent.rotation, rotation, time);
                    else
                        valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => trans.localRotation = x, rotation, time);

                    seq.Insert(this, valueTween);
                    break;

                case Interpolation.Linear:
                    Quaternion endRotation = (track.keys[index + 1] as RotationKey).rotation;

                    TweenerCore<Quaternion, Quaternion, NoOptions> linearTween;

                    if(body2D)
                        linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => body2D.MoveRotation((x * transParent.rotation).eulerAngles.z), endRotation, time);
                    else if(body)
                        linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => body.MoveRotation(x * transParent.rotation), endRotation, time);
                    else
                        linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => trans.localRotation = x, endRotation, time);

                    if(hasCustomEase())
                        linearTween.SetEase(easeCurve);
                    else
                        linearTween.SetEase(easeType, amplitude, period);

                    seq.Insert(this, linearTween);
                    break;

                case Interpolation.Curve:
                    var pathTween = CreatePathTween(frameRate);

                    if(body2D) {
                        pathTween.setter = x => body2D.MoveRotation(transParent.eulerAngles.z + x.z);
                        pathTween.SetTarget(body2D);
                    }
                    else if(body) {
                        pathTween.setter = x => body.MoveRotation(Quaternion.Euler(x) * transParent.rotation);
                        pathTween.SetTarget(body);
                    }
                    else {
                        pathTween.setter = x => trans.localRotation = Quaternion.Euler(x);
                        pathTween.SetTarget(trans);
                    }

                    if(hasCustomEase())
                        pathTween.SetEase(easeCurve);
                    else
                        pathTween.SetEase(easeType, amplitude, period);

                    seq.Insert(this, pathTween);
                    break;
            }
        }
        #endregion
    }
}
 