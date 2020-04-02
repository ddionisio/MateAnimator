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
    public class RotationEulerKey : Key {
        public override SerializeType serializeType { get { return SerializeType.RotationEuler; } }

        public override int keyCount { get { return path != null ? path.wps.Length : 1; } }

        public const int pathResolution = 10;

        public Vector3 rotation;

        public int endFrame;

        public TweenPlugPath path { get { return _paths.Length > 0 ? _paths[0] : null; } } //save serialize size by using array (path has a lot of serialized fields)
        [SerializeField] TweenPlugPath[] _paths;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(RotationEulerTrack track, int keyInd) {
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
                            var key = (RotationEulerKey)track.keys[i];

                            pathList.Add(new TweenPlugPathPoint(key.rotation));
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

            var pt = path.GetPoint(finalT, true);

            return pt.valueVector3;
        }

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
            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(endFrame == -1) return;
            }

            Transform target = obj as Transform;
            var transParent = target.parent;

            Rigidbody body = target.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? target.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;

            var rotTrack = (RotationEulerTrack)track;
            var axis = rotTrack.axis;

            float timeLength = getTime(frameRate);//1.0f / frameRate;

            if(interp == Interpolation.None) {
                if(axis == AxisFlags.X) {
                    float _x = rotation.x;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenX;

                    if(body)
                        tweenX = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(x, a.y, a.z) * transParent.rotation; }, _x, timeLength);
                    else
                        tweenX = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, _x, timeLength);

                    seq.Insert(this, tweenX);
                }
                else if(axis == AxisFlags.Y) {
                    float _y = rotation.y;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenY;

                    if(body)
                        tweenY = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(a.x, y, a.z) * transParent.rotation; }, _y, timeLength);
                    else
                        tweenY = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, _y, timeLength);

                    seq.Insert(this, tweenY);
                }
                else if(axis == AxisFlags.Z) {
                    float _z = rotation.z;
                    TweenerCore<float, float, TWeenPlugNoneOptions> tweenZ;

                    if(body2D)
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, (z) => { body2D.rotation = z + transParent.eulerAngles.z; }, _z, timeLength);
                    else if(body)
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; body.rotation = Quaternion.Euler(a.x, a.y, z) * transParent.rotation; }, _z, timeLength);
                    else
                        tweenZ = DOTween.To(TweenPlugValueSet<float>.Get(), () => target.localEulerAngles.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, _z, timeLength);

                    seq.Insert(this, tweenZ);
                }
                else if(axis == AxisFlags.All) {
                    TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweenV;

                    if(body2D)
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localEulerAngles, (r) => { body2D.rotation = r.z + transParent.eulerAngles.z; }, rotation, timeLength);
                    else if(body)
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => target.localEulerAngles, (r) => { body.rotation = Quaternion.Euler(r) * transParent.rotation; }, rotation, timeLength);
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
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                body2D.rotation = r.z + transParent.eulerAngles.z;
                        }, rotation, timeLength);
                    }
                    else if(body) {
                        tweenV = DOTween.To(TweenPlugValueSet<Vector3>.Get(), getter, (r) => {
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
                    if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.x, (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(x, a.y, a.z) * transParent.rotation); }, endRotation.x, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, timeLength);
                }
                else if(axis == AxisFlags.Y) {
                    if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.y, (y) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, y, a.z) * transParent.rotation); }, endRotation.y, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, timeLength);
                }
                else if(axis == AxisFlags.Z) {
                    if(body2D)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => { body2D.MoveRotation(z + transParent.eulerAngles.z); }, endRotation.z, timeLength);
                    else if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, a.y, z) * transParent.rotation); }, endRotation.z, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => rotation.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, timeLength);
                }
                else if(axis == AxisFlags.All) {
                    if(body2D)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => body2D.MoveRotation(r.z + transParent.eulerAngles.z), endRotation, timeLength);
                    else if(body)
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => body.MoveRotation(Quaternion.Euler(r) * transParent.rotation), endRotation, timeLength);
                    else
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => target.localEulerAngles = r, endRotation, timeLength);
                }
                else {
                    if(body2D) {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                        }, endRotation, timeLength);
                    }
                    else if(body) {
                        tween = DOTween.To(TweenPluginFactory.CreateVector3(), () => rotation, (r) => {
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

                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase(easeType, amplitude, period);

                seq.Insert(this, tween);
            }
            else if(interp == Interpolation.Curve) {
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart, isClosedPath = path.isClosed };

                DOSetter<Vector3> setter;
                if(axis == AxisFlags.X) {
                    if(body) 
                        setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(x.x, a.y, a.z) * transParent.rotation); };
                    else
                        setter = (x) => { var a = target.localEulerAngles; a.x = x.x; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.Y) {
                    if(body)
                        setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, x.y, a.z) * transParent.rotation); };
                    else
                        setter = (x) => { var a = target.localEulerAngles; a.y = x.y; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.Z) {
                    if(body2D)
                        setter = (x) => { body2D.MoveRotation(x.z + transParent.eulerAngles.z); };
                    else if(body)
                        setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, a.y, x.z) * transParent.rotation); };
                    else
                        setter = (x) => { var a = target.localEulerAngles; a.z = x.z; target.localEulerAngles = a; };
                }
                else if(axis == AxisFlags.All) {
                    if(body2D)
                        setter = (r) => body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                    else if(body)
                        setter = (r) => body.MoveRotation(Quaternion.Euler(r) * transParent.rotation);
                    else
                        setter = (r) => target.localEulerAngles = r;
                }
                else {
                    if(body2D)
                        setter = (r) => {
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                        };
                    else if(body)
                        setter = (r) => {
                            var rot = target.localEulerAngles;
                            if((axis & AxisFlags.X) != AxisFlags.None)
                                rot.x = r.x;
                            if((axis & AxisFlags.Y) != AxisFlags.None)
                                rot.y = r.y;
                            if((axis & AxisFlags.Z) != AxisFlags.None)
                                rot.z = r.z;
                            body.MoveRotation(Quaternion.Euler(rot) * transParent.rotation);
                        };
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