using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;

namespace M8.Animator {
    [System.Serializable]
    public class ScaleKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Scale; } }

        public const int pathResolution = 10;

        public Vector3 scale;

        public int endFrame;

        public Vector3[] path;

        public bool isClosed { get { return path.Length > 1 && path[0] == path[path.Length - 1]; } }

        private TweenerCore<Vector3, Path, PathOptions> mPathTween;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(ScaleTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    path = new Vector3[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    path = new Vector3[0];

                    if(keyInd + 1 < track.keys.Count)
                        endFrame = track.keys[keyInd + 1].frame;
                    else //fail-safe
                        endFrame = -1;
                    break;

                case Interpolation.Curve:
                    var pathList = new List<Vector3>();

                    for(int i = keyInd; i < track.keys.Count; i++) {
                        var key = (ScaleKey)track.keys[i];

                        pathList.Add(key.scale);
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
        /// Grab tweener, create if it doesn't exists. keyInd is the index of this key in the track.
        /// </summary>
        TweenerCore<Vector3, Path, PathOptions> GetPathTween(int frameRate) {
            if((mPathTween == null || !mPathTween.active) && path.Length > 1) {
                //if all points are equal, then set to Linear to prevent error from DOTween
                var pathType = path.Length == 2 || IsPathPointsEqual() ? PathType.Linear : PathType.CatmullRom;

                var pathData = new Path(pathType, path, pathResolution);

                mPathTween = DOTween.To(PathPlugin.Get(), _Getter, _SetterNull, pathData, getTime(frameRate));

                mPathTween.SetRelative(false).SetOptions(isClosed);
            }

            return mPathTween;
        }

        Vector3 _Getter() { return scale; }

        void _SetterNull(Vector3 val) { }

        public void ClearCache() {
            if(mPathTween != null) {
                mPathTween.Kill();
                mPathTween = null;
            }
        }

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Vector3 GetScaleFromPath(Transform transform, int frameRate, float t) {
            var tween = GetPathTween(frameRate);
            if(tween == null) //not tweenable
                return scale;

            if(tween.target == null)
                tween.SetTarget(transform);

            if(!tween.IsInitialized())
                tween.ForceInit();

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return scale;
            }

            return tween.PathGetPoint(finalT);
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as ScaleKey;

            a.scale = scale;
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

            Transform target = obj as Transform;

            int frameRate = seq.take.frameRate;

            var scaleTrack = (ScaleTrack)track;
            var axis = scaleTrack.axis;

            float timeLength = getTime(frameRate);

            switch(interp) {
                case Interpolation.None:
                    if(axis == AxisFlags.X) {
                        float _x = scale.x;
                        var tweenX = DOTween.To(new TweenPlugValueSet<float>(), () => target.localScale.x, (x) => { var a = target.localScale; a.x = x; target.localScale = a; }, _x, timeLength);
                        seq.Insert(this, tweenX);
                    }
                    else if(axis == AxisFlags.Y) {
                        float _y = scale.y;
                        var tweenY = DOTween.To(new TweenPlugValueSet<float>(), () => target.localScale.y, (y) => { var a = target.localScale; a.y = y; target.localScale = a; }, _y, timeLength);
                        seq.Insert(this, tweenY);
                    }
                    else if(axis == AxisFlags.Z) {
                        float _z = scale.z;
                        var tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => target.localScale.z, (z) => { var a = target.localScale; a.z = z; target.localScale = a; }, _z, timeLength);
                        seq.Insert(this, tweenZ);
                    }
                    else if(axis == AxisFlags.All) {
                        var tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => target.localScale, (s) => { target.localScale = s; }, scale, timeLength);
                        seq.Insert(this, tweenV);
                    }
                    else {
                        var tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(),
                            () => {
                                var ls = scale;
                                var curls = target.localScale;
                                if((axis & AxisFlags.X) != AxisFlags.None)
                                    ls.x = curls.x;
                                if((axis & AxisFlags.Y) != AxisFlags.None)
                                    ls.y = curls.y;
                                if((axis & AxisFlags.Z) != AxisFlags.None)
                                    ls.z = curls.z;
                                return ls;
                            },
                            (s) => {
                                var ls = target.localScale;
                                if((axis & AxisFlags.X) != AxisFlags.None)
                                    ls.x = s.x;
                                if((axis & AxisFlags.Y) != AxisFlags.None)
                                    ls.y = s.y;
                                if((axis & AxisFlags.Z) != AxisFlags.None)
                                    ls.z = s.z;
                                target.localScale = ls;
                            }, scale, timeLength);
                        seq.Insert(this, tweenV);
                    }
                    break;

                case Interpolation.Linear:
                    Vector3 endScale = ((ScaleKey)track.keys[index + 1]).scale;

                    Tweener tween;

                    if(axis == AxisFlags.X)
                        tween = DOTween.To(new FloatPlugin(), () => scale.x, (x) => { var a = target.localScale; a.x = x; target.localScale = a; }, endScale.x, timeLength);
                    else if(axis == AxisFlags.Y)
                        tween = DOTween.To(new FloatPlugin(), () => scale.y, (y) => { var a = target.localScale; a.y = y; target.localScale = a; }, endScale.y, timeLength);
                    else if(axis == AxisFlags.Z)
                        tween = DOTween.To(new FloatPlugin(), () => scale.z, (z) => { var a = target.localScale; a.z = z; target.localScale = a; }, endScale.z, timeLength);
                    else if(axis == AxisFlags.All)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => scale, (s) => target.localScale = s, endScale, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => scale, (s) => {
                            var ls = target.localScale;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                ls.x = s.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                ls.y = s.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                ls.z = s.z;
                            target.localScale = ls;
                        }, endScale, timeLength);

                    if(hasCustomEase())
                        tween.SetEase(easeCurve);
                    else
                        tween.SetEase(easeType, amplitude, period);

                    seq.Insert(this, tween);
                    break;

                case Interpolation.Curve:
                    var pathTween = GetPathTween(frameRate);

                    if(axis == AxisFlags.X)
                        pathTween.setter = (s) => { var a = target.localScale; a.x = s.x; target.localScale = a; };
                    else if(axis == AxisFlags.Y)
                        pathTween.setter = (s) => { var a = target.localScale; a.y = s.y; target.localScale = a; };
                    else if(axis == AxisFlags.Z)
                        pathTween.setter = (s) => { var a = target.localScale; a.z = s.z; target.localScale = a; };
                    else if(axis == AxisFlags.All)
                        pathTween.setter = (s) => target.localScale = s;
                    else
                        pathTween.setter = (s) => {
                            var ls = target.localScale;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                ls.x = s.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                ls.y = s.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                ls.z = s.z;
                            target.localScale = ls;
                        };

                    pathTween.SetTarget(target);

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