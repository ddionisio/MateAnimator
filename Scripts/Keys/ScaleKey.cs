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

        public override int keyCount { get { return path != null ? path.wps.Length : 1; } }

        public const int pathResolution = 10;

        public Vector3 scale;

        public int endFrame;

        public TweenPlugPath path { get { return _paths.Length > 0 ? _paths[0] : null; } } //save serialize size by using array (path has a lot of serialized fields)
        [SerializeField] TweenPlugPath[] _paths;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(ScaleTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    _paths = new TweenPlugPath[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    _paths = new TweenPlugPath[0];

                    if(keyInd + 1 < track.keys.Count)
                        endFrame = track.keys[keyInd + 1].frame;
                    else //fail-safe
                        endFrame = -1;
                    break;

                case Interpolation.Curve:
                    //if there's more than 2 keys, and next key is curve, then it's more than 2 pts.
                    if(keyInd + 2 < track.keys.Count && track.keys[keyInd + 1].interp == Interpolation.Curve) {
                        var pathList = new List<TweenPlugPathPoint>();

                        for(int i = keyInd; i < track.keys.Count; i++) {
                            var key = (ScaleKey)track.keys[i];

                            pathList.Add(new TweenPlugPathPoint(key.scale));
                            endFrame = key.frame;

                            if(key.interp != Interpolation.Curve)
                                break;
                        }

                        var newPath = new TweenPlugPath(TweenPlugPathType.CatmullRom, pathList.ToArray(), pathResolution);
                        newPath.Init(newPath.isClosed);

                        _paths = new TweenPlugPath[] { newPath };
                    }
                    else {
                        if(keyInd + 1 < track.keys.Count) {
                            endFrame = track.keys[keyInd + 1].frame;
                            _paths = new TweenPlugPath[0];
                        }
                        else
                            Invalidate();
                    }
                    break;
            }
        }

        public void Invalidate() {
            endFrame = -1;
            _paths = new TweenPlugPath[0];
        }

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Vector3 GetScaleFromPath(float t) {
            if(path == null) //not tweenable
                return scale;

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return scale;
            }

            var pt = path.GetPoint(finalT, true);

            return pt.valueVector3;
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
            }

            Transform target = obj as Transform;

            int frameRate = seq.take.frameRate;

            var scaleTrack = (ScaleTrack)track;
            var axis = scaleTrack.axis;

            float timeLength = getTime(frameRate);

            if(interp == Interpolation.None) {
                if(axis == AxisFlags.X) {
                    float _x = scale.x;
                    var tweenX = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localScale.x, (x) => { var a = target.localScale; a.x = x; target.localScale = a; }, _x, timeLength);
                    seq.Insert(this, tweenX);
                }
                else if(axis == AxisFlags.Y) {
                    float _y = scale.y;
                    var tweenY = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localScale.y, (y) => { var a = target.localScale; a.y = y; target.localScale = a; }, _y, timeLength);
                    seq.Insert(this, tweenY);
                }
                else if(axis == AxisFlags.Z) {
                    float _z = scale.z;
                    var tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localScale.z, (z) => { var a = target.localScale; a.z = z; target.localScale = a; }, _z, timeLength);
                    seq.Insert(this, tweenZ);
                }
                else if(axis == AxisFlags.All) {
                    var tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localScale, (s) => { target.localScale = s; }, scale, timeLength);
                    seq.Insert(this, tweenV);
                }
                else {
                    var tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(),
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
            }
            else if(interp == Interpolation.Linear || path == null) {
                Vector3 endScale = ((ScaleKey)track.keys[index + 1]).scale;

                Tweener tween;

                if(axis == AxisFlags.X)
                    tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => scale.x, (x) => { var a = target.localScale; a.x = x; target.localScale = a; }, endScale.x, timeLength);
                else if(axis == AxisFlags.Y)
                    tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => scale.y, (y) => { var a = target.localScale; a.y = y; target.localScale = a; }, endScale.y, timeLength);
                else if(axis == AxisFlags.Z)
                    tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => scale.z, (z) => { var a = target.localScale; a.z = z; target.localScale = a; }, endScale.z, timeLength);
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
            }
            else if(interp == Interpolation.Curve) {
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart, isClosedPath = path.isClosed };

                DOSetter<Vector3> setter;

                if(axis == AxisFlags.X)
                    setter = (s) => { var a = target.localScale; a.x = s.x; target.localScale = a; };
                else if(axis == AxisFlags.Y)
                    setter = (s) => { var a = target.localScale; a.y = s.y; target.localScale = a; };
                else if(axis == AxisFlags.Z)
                    setter = (s) => { var a = target.localScale; a.z = s.z; target.localScale = a; };
                else if(axis == AxisFlags.All)
                    setter = (s) => target.localScale = s;
                else
                    setter = (s) => {
                        var ls = target.localScale;
                        if((axis & AxisFlags.X) != AxisFlags.None)
                            ls.x = s.x;
                        if((axis & AxisFlags.Y) != AxisFlags.None)
                            ls.y = s.y;
                        if((axis & AxisFlags.Z) != AxisFlags.None)
                            ls.z = s.z;
                        target.localScale = ls;
                    };

                var tweenPath = DOTween.To(TweenPlugPathVector3.Get(), () => scale, setter, path, timeLength);

                if(hasCustomEase())
                    tweenPath.SetEase(easeCurve);
                else
                    tweenPath.SetEase(easeType, amplitude, period);

                seq.Insert(this, tweenPath);
            }
        }
        #endregion
    }
}