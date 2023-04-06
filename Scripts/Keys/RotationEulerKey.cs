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
    public class RotationEulerKey : PathKeyBase {
        public override SerializeType serializeType { get { return SerializeType.RotationEuler; } }

        public Vector3 rotation;

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Vector3 GetRotationFromPath(float t) {
            if(path == null) //not tweenable
                return rotation;

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return rotation;
            }

            var pt = path.GetPoint(finalT);

            return pt.valueVector3;
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as RotationEulerKey;

            a.rotation = rotation;
        }

        protected override TweenPlugPathPoint GeneratePathPoint(Track track) {
            return new TweenPlugPathPoint(rotation);
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

#if !M8_PHYSICS_DISABLED
            Rigidbody body = target.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? target.GetComponent<Rigidbody2D>() : null;
#else
            Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
#endif

            int frameRate = seq.take.frameRate;

            var rotTrack = (RotationEulerTrack)track;
            var axis = rotTrack.axis;

            float timeLength = getTime(frameRate);//1.0f / frameRate;

            if(interp == Interpolation.None) {
                if(axis == AxisFlags.X) {
                    float _x = rotation.x;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenX;

#if !M8_PHYSICS_DISABLED
                    if(body)
                        tweenX = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.x, 
                            (x) => {
                                var a = target.localEulerAngles;
                                var parent = target.parent;
                                if(parent)
                                    body.rotation = Quaternion.Euler(x, a.y, a.z) * parent.rotation;
                                else
                                    body.rotation = Quaternion.Euler(x, a.y, a.z);
                            }, _x, timeLength);
                    else
#endif
                        tweenX = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, _x, timeLength);

                    seq.Insert(this, tweenX);
                }
                else if(axis == AxisFlags.Y) {
                    float _y = rotation.y;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenY;

#if !M8_PHYSICS_DISABLED
                    if(body)
                        tweenY = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.y, 
                            (y) => { 
                                var a = target.localEulerAngles;
                                var parent = target.parent;
                                if(parent)
                                    body.rotation = Quaternion.Euler(a.x, y, a.z) * parent.rotation;
                                else
                                    body.rotation = Quaternion.Euler(a.x, y, a.z);
                            }, _y, timeLength);
                    else
#endif
                        tweenY = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, _y, timeLength);

                    seq.Insert(this, tweenY);
                }
                else if(axis == AxisFlags.Z) {
                    float _z = rotation.z;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenZ;

                    if(body2D)
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, 
                            (z) => {
                                var parent = target.parent;
                                if(parent)
                                    body2D.rotation = z + parent.eulerAngles.z;
                                else
                                    body2D.rotation = z;
                            }, _z, timeLength);
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, 
                            (z) => { 
                                var a = target.localEulerAngles;
                                var parent = target.parent;
                                if(parent)
                                    body.rotation = Quaternion.Euler(a.x, a.y, z) * parent.rotation; 
                                else
                                    body.rotation = Quaternion.Euler(a.x, a.y, z);
                            }, _z, timeLength);
#endif
                    else
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, _z, timeLength);

                    seq.Insert(this, tweenZ);
                }
                else if(axis == AxisFlags.All) {
                    TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweenV;

                    if(body2D)
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localEulerAngles, 
                            (r) => {
                                var parent = target.parent;
                                if(parent)
                                    body2D.rotation = r.z + parent.eulerAngles.z;
                                else
                                    body2D.rotation = r.z;
                            }, rotation, timeLength);
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localEulerAngles, 
                            (r) => {
                                var parent = target.parent;
                                if(parent)
                                    body.rotation = Quaternion.Euler(r) * parent.rotation;
                                else
                                    body.rotation = Quaternion.Euler(r);
                            }, rotation, timeLength);
#endif
                    else
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localEulerAngles, (r) => { target.localEulerAngles = r; }, rotation, timeLength);

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
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), getter, (r) => {
                            if((axis & AxisFlags.Z) != AxisFlags.None) {
                                var parent = target.parent;
                                if(parent)
                                    body2D.rotation = r.z + parent.eulerAngles.z;
                                else
                                    body2D.rotation = r.z;
                            }
                        }, rotation, timeLength);
                    }
#if !M8_PHYSICS_DISABLED
                    else if(body) {
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), getter, (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;

                            var parent = target.parent;
                            if(parent)
                                body.rotation = Quaternion.Euler(rot) * parent.rotation;
                            else
                                body.rotation = Quaternion.Euler(rot);
                        }, rotation, timeLength);
                    }
#endif
                    else {
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), getter, (r) => {
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
            else if(interp == Interpolation.Linear || path == null) {
                Vector3 endRotation = (track.keys[index + 1] as RotationEulerKey).rotation;

                Tweener tween;

                if(axis == AxisFlags.X) {
#if !M8_PHYSICS_DISABLED
                    if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.x, (x) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(x, a.y, a.z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(x, a.y, a.z));
                        }, endRotation.x, timeLength);
                    else
#endif
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, timeLength);
                }
                else if(axis == AxisFlags.Y) {
#if !M8_PHYSICS_DISABLED
                    if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.y, (y) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(a.x, y, a.z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(a.x, y, a.z));
                        }, endRotation.y, timeLength);
                    else
#endif
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, timeLength);
                }
                else if(axis == AxisFlags.Z) {
                    if(body2D)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => {
                            var parent = target.parent;
                            if(parent)
                                body2D.MoveRotation(z + parent.eulerAngles.z); 
                            else
                                body2D.MoveRotation(z);
                        }, endRotation.z, timeLength);
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(a.x, a.y, z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(a.x, a.y, z));
                        }, endRotation.z, timeLength);
#endif
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, timeLength);
                }
                else if(axis == AxisFlags.All) {
                    if(body2D)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
                            var parent = target.parent;
                            if(parent)
                                body2D.MoveRotation(r.z + parent.eulerAngles.z); 
                            else
                                body2D.MoveRotation(r.z);
                        }, endRotation, timeLength);
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(r) * parent.rotation);
                            else
                                body.MoveRotation(Quaternion.Euler(r));
                        }, endRotation, timeLength);
#endif
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => target.localEulerAngles = r, endRotation, timeLength);
                }
                else {
                    if(body2D) {
                        if((axis & AxisFlags.Z) != AxisFlags.None) {
                            tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
                                var parent = target.parent;
                                if(parent)
                                    body2D.MoveRotation(r.z + parent.eulerAngles.z);
                                else
                                    body2D.MoveRotation(r.z);
                            }, endRotation, timeLength);
                        }
                        else
                            tween = null;
                    }
#if !M8_PHYSICS_DISABLED
                    else if(body) {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(rot) * parent.rotation);
                            else
                                body.MoveRotation(Quaternion.Euler(rot));
                        }, endRotation, timeLength);
                    }
#endif
                    else {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
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

                if(tween != null) {
                    if(hasCustomEase())
                        tween.SetEase(easeCurve);
                    else
                        tween.SetEase(easeType, amplitude, period);

                    seq.Insert(this, tween);
                }
            }
            else if(interp == Interpolation.Curve) {
                DOSetter<Vector3> setter;
                if(axis == AxisFlags.X) {
#if !M8_PHYSICS_DISABLED
                    if(body) 
                        setter = (x) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(x.x, a.y, a.z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(x.x, a.y, a.z));
                        };
                    else
#endif
                        setter = (x) => { var a = target.localEulerAngles; a.x = x.x; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.Y) {
#if !M8_PHYSICS_DISABLED
                    if(body)
                        setter = (x) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(a.x, x.y, a.z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(a.x, x.y, a.z));
                        };
                    else
#endif
                        setter = (x) => { var a = target.localEulerAngles; a.y = x.y; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.Z) {
                    if(body2D)
                        setter = (x) => {
                            var parent = target.parent;
                            if(parent)
                                body2D.MoveRotation(x.z + parent.eulerAngles.z); 
                            else
                                body2D.MoveRotation(x.z);
                        };
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        setter = (x) => { 
                            var a = target.localEulerAngles;
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(a.x, a.y, x.z) * parent.rotation); 
                            else
                                body.MoveRotation(Quaternion.Euler(a.x, a.y, x.z));
                        };
#endif
                    else
                        setter = (x) => { var a = target.localEulerAngles; a.z = x.z; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.All) {
                    if(body2D)
                        setter = (r) => {
                            var parent = target.parent;
                            if(parent)
                                body2D.MoveRotation(r.z + parent.eulerAngles.z);
                            else
                                body2D.MoveRotation(r.z);
                        };
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        setter = (r) => {
                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(r) * parent.rotation);
                            else
                                body.MoveRotation(Quaternion.Euler(r));
                        };
#endif
                    else
                        setter = (r) => target.localEulerAngles = r;
                }
                else {
                    if(body2D) {
                        if((axis & AxisFlags.Z) != AxisFlags.None)
                            setter = (r) => {
                                var parent = target.parent;
                                if(parent)
                                    body2D.MoveRotation(r.z + parent.eulerAngles.z);
                                else
                                    body2D.MoveRotation(r.z);
                            };
                        else
                            return;
                    }
#if !M8_PHYSICS_DISABLED
                    else if(body)
                        setter = (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;

                            var parent = target.parent;
                            if(parent)
                                body.MoveRotation(Quaternion.Euler(rot) * parent.rotation);
                            else
                                body.MoveRotation(Quaternion.Euler(rot));
                        };
#endif
                    else
                        setter = (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            target.localEulerAngles = rot;
                        };
                }

                var tweenPath = DOTween.To(TweenPlugPathVector3.Get(), () => rotation, setter, path, timeLength);

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