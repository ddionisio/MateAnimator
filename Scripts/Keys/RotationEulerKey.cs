using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
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
            var transParent = target.parent;

            Rigidbody body = target.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? target.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;

            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;

            var rotTrack = (RotationEulerTrack)track;
            var axis = rotTrack.axis;

            if(!canTween) {
                float timeLength = getTime(frameRate);//1.0f / frameRate;

                if(axis == AxisFlags.X) {
                    float _x = rotation.x;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenX;

                    if(body)
                        tweenX = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(x, a.y, a.z) * transParent.rotation; }, _x, timeLength);
                    else
                        tweenX = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, _x, timeLength);

                    seq.Insert(this, tweenX);
                }
                else if(axis == AxisFlags.Y) {
                    float _y = rotation.y;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenY;

                    if(body)
                        tweenY = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(a.x, y, a.z) * transParent.rotation; }, _y, timeLength);
                    else
                        tweenY = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, _y, timeLength);

                    seq.Insert(this, tweenY);
                }
                else if(axis == AxisFlags.Z) {
                    float _z = rotation.z;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenZ;

                    if(body2D)
                        tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.z, (z) => { body2D.rotation = z + transParent.eulerAngles.z; }, _z, timeLength);
                    else if(body)
                        tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(a.x, a.y, z) * transParent.rotation; }, _z, timeLength);
                    else
                        tweenZ = DOTween.To(new TweenPlugValueSet<float>(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, _z, timeLength);

                    seq.Insert(this, tweenZ);
                }
                else if(axis == AxisFlags.All) {
                    TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweenV;

                    if(body2D)
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => target.localEulerAngles, (r) => { body2D.rotation = r.z + transParent.eulerAngles.z; }, rotation, timeLength);
                    else if(body)
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => target.localEulerAngles, (r) => { body.rotation = Quaternion.Euler(r) * transParent.rotation; }, rotation, timeLength);
                    else
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), () => target.localEulerAngles, (r) => { target.localEulerAngles = r; }, rotation, timeLength);

                    seq.Insert(this, tweenV);
                }
                else {
                    TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweenV;

                    DOGetter<Vector3> getter = () => {
                        var ret = rotation;
                        var rot = target.localEulerAngles;
                        if((axis & AxisFlags.X) != AxisFlags.None)
                            ret.x = rot.x;
                        if((axis & AxisFlags.Y) != AxisFlags.None)
                            ret.y = rot.y;
                        if((axis & AxisFlags.Z) != AxisFlags.None)
                            ret.z = rot.z;
                        return ret;
                    };

                    if(body2D) {
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), getter, (r) => {
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                body2D.rotation = r.z + transParent.eulerAngles.z;
                        }, rotation, timeLength);
                    }
                    else if(body) {
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), getter, (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            body.rotation = Quaternion.Euler(rot) * transParent.rotation;
                        }, rotation, timeLength);
                    }
                    else {
                        tweenV = DOTween.To(new TweenPlugValueSet<Vector3>(), getter, (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            target.localEulerAngles = rot;
                        }, rotation, timeLength);
                    }

                    seq.Insert(this, tweenV);
                }
            }
            else if(endFrame == -1) return;
            else {
                float timeLength = getTime(frameRate);
                Vector3 endRotation = (track.keys[index + 1] as RotationEulerKey).rotation;

                Tweener tween;

                if(axis == AxisFlags.X) {
                    if(body)
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(x, a.y, a.z) * transParent.rotation); }, endRotation.x, timeLength);
                    else
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, timeLength);
                }
                else if(axis == AxisFlags.Y) {
                    if(body)
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, y, a.z) * transParent.rotation); }, endRotation.y, timeLength);
                    else
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, timeLength);
                }
                else if(axis == AxisFlags.Z) {
                    if(body2D)
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.z, (z) => { body2D.rotation = z + transParent.eulerAngles.z; }, endRotation.z, timeLength);
                    else if(body)
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, a.y, z) * transParent.rotation); }, endRotation.z, timeLength);
                    else
                        tween = DOTween.To(new FloatPlugin(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, timeLength);
                }
                else if(axis == AxisFlags.All) {
                    if(body2D)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => body2D.MoveRotation(r.z + transParent.eulerAngles.z), endRotation, timeLength);
                    else if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => body.MoveRotation(Quaternion.Euler(r) * transParent.rotation), endRotation, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => target.localEulerAngles = r, endRotation, timeLength);
                }
                else {
                    if(body2D) {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => {
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                        }, endRotation, timeLength);
                    }
                    else if(body) {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => target.localEulerAngles, (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            body.MoveRotation(Quaternion.Euler(rot) * transParent.rotation);
                        }, endRotation, timeLength);
                    }
                    else {
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
                    }
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