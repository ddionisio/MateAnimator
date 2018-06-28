using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
    [System.Serializable]
    public class RotationEulerKey : Key {
        public override SerializeType serializeType { get { return SerializeType.RotationEuler; } }

        public Vector3 rotation;

        public int endFrame;

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as RotationEulerKey;

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
            Transform target = obj as Transform;

            int frameRate = seq.take.frameRate;

            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;

            var rotTrack = (RotationEulerTrack)track;
            var axis = rotTrack.axis;

            if(!canTween) {
                float timeLength = 1.0f / frameRate;

                if(axis == AxisFlags.X) {
                    float _x = rotation.x;
                    var tweenX = DOTween.To(new TweenPlugValueSet<float>(), () => _x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, _x, timeLength);
                    tweenX.plugOptions.SetSequence(seq);
                    seq.Insert(this, tweenX);
                }
                else if(axis == AxisFlags.Y) {
                    float _y = rotation.y;
                    var tweenY = DOTween.To(new TweenPlugValueSet<float>(), () => _y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, _y, timeLength);
                    tweenY.plugOptions.SetSequence(seq);
                    seq.Insert(this, tweenY);
                }
                else if(axis == AxisFlags.Z) {
                    float _z = rotation.z;
                    var tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => _z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, _z, timeLength);
                    tweenZ.plugOptions.SetSequence(seq);
                    seq.Insert(this, tweenZ);
                }
                else if(axis == AxisFlags.All) {
                    var tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => rotation, (r) => { target.localEulerAngles = r; }, rotation, timeLength);
                    tweenV.plugOptions.SetSequence(seq);
                    seq.Insert(this, tweenV);
                }
                else {
                    var tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => rotation, (r) => {
                        var rot = target.localEulerAngles;
                        if((axis & AxisFlags.X) != AxisFlags.None)
                            rot.x = r.x;
                        if((axis & AxisFlags.Y) != AxisFlags.None)
                            rot.y = r.y;
                        if((axis & AxisFlags.Z) != AxisFlags.None)
                            rot.z = r.z;
                        target.localEulerAngles = rot;
                    }, rotation, timeLength);
                    tweenV.plugOptions.SetSequence(seq);
                    seq.Insert(this, tweenV);
                }
            }
            else if(endFrame == -1) return;
            else {
                float timeLength = getTime(frameRate);
                Vector3 endRotation = (track.keys[index + 1] as RotationEulerKey).rotation;

                Tweener tween;

                if(axis == AxisFlags.X)
                    tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, timeLength);
                else if(axis == AxisFlags.Y)
                    tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, timeLength);
                else if(axis == AxisFlags.Z)
                    tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, timeLength);
                else if(axis == AxisFlags.All)
                    tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => target.localEulerAngles = r, endRotation, timeLength);
                else
                    tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => {
                        var rot = target.localEulerAngles;
                        if((axis & AxisFlags.X) != AxisFlags.None)
                            rot.x = r.x;
                        if((axis & AxisFlags.Y) != AxisFlags.None)
                            rot.y = r.y;
                        if((axis & AxisFlags.Z) != AxisFlags.None)
                            rot.z = r.z;
                        target.localEulerAngles = rot;
                    }, endRotation, timeLength);

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