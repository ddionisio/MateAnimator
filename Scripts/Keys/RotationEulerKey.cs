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

            if(!canTween) {
                switch((track as RotationEulerTrack).axis) {
                    case RotationEulerTrack.Axis.X:
                        float _x = rotation.x;
                        var tweenX = DOTween.To(new TweenPlugValueSet<float>(), () => _x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, _x, getTime(frameRate));
                        tweenX.plugOptions.SetSequence(seq);
                        seq.Insert(this, tweenX);
                        break;
                    case RotationEulerTrack.Axis.Y:
                        float _y = rotation.y;
                        var tweenY = DOTween.To(new TweenPlugValueSet<float>(), () => _y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, _y, getTime(frameRate));
                        tweenY.plugOptions.SetSequence(seq);
                        seq.Insert(this, tweenY);
                        break;
                    case RotationEulerTrack.Axis.Z:
                        float _z = rotation.z;
                        var tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => _z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, _z, getTime(frameRate));
                        tweenZ.plugOptions.SetSequence(seq);
                        seq.Insert(this, tweenZ);
                        break;
                    default:
                        var tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => rotation, (r) => { target.localEulerAngles = r; }, rotation, getTime(frameRate));
                        tweenV.plugOptions.SetSequence(seq);
                        seq.Insert(this, tweenV);
                        break;
                }
            }
            else if(endFrame == -1) return;
            else {
                Vector3 endRotation = (track.keys[index + 1] as RotationEulerKey).rotation;

                Tweener tween;

                switch((track as RotationEulerTrack).axis) {
                    case RotationEulerTrack.Axis.X:
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, getTime(frameRate));
                        break;
                    case RotationEulerTrack.Axis.Y:
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, getTime(frameRate));
                        break;
                    case RotationEulerTrack.Axis.Z:
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, getTime(frameRate));
                        break;
                    default:
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (x) => target.localEulerAngles = x, endRotation, getTime(frameRate));
                        break;
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