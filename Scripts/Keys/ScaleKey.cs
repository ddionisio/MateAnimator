using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
    [System.Serializable]
    public class ScaleKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Scale; } }

        public Vector3 scale;

        public int endFrame;

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
            Transform target = obj as Transform;

            int frameRate = seq.take.frameRate;

            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;

            var scaleTrack = (ScaleTrack)track;
            var axis = scaleTrack.axis;

            if(!canTween) {
                float timeLength = getTime(frameRate);

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
            }
            else if(endFrame == -1) return;
            else {
                float timeLength = getTime(frameRate);
                Vector3 endScale = ((ScaleKey)track.keys[index + 1]).scale;

                Tweener tween;

                if(axis == AxisFlags.X)
                    tween = DOTween.To(new FloatPlugin(), () => target.localScale.x, (x) => { var a = target.localScale; a.x = x; target.localScale = a; }, endScale.x, timeLength);
                else if(axis == AxisFlags.Y)
                    tween = DOTween.To(new FloatPlugin(), () => target.localScale.y, (y) => { var a = target.localScale; a.y = y; target.localScale = a; }, endScale.y, timeLength);
                else if(axis == AxisFlags.Z)
                    tween = DOTween.To(new FloatPlugin(), () => target.localScale.z, (z) => { var a = target.localScale; a.z = z; target.localScale = a; }, endScale.z, timeLength);
                else if(axis == AxisFlags.All)
                    tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localScale, (s) => target.localScale = s, endScale, timeLength);
                else
                    tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localScale, (s) => {
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
        }
        #endregion
    }
}