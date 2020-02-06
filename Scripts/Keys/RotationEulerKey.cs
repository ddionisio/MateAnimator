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

        public const int pathResolution = 10;

        public Vector3 rotation;

        public int endFrame;

        public Vector3[] path;

        public bool isClosed { get { return path.Length > 1 && path[0] == path[path.Length - 1]; } }

        private TweenerCore<Vector3, Path, PathOptions> mPathTween;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(RotationEulerTrack track, int keyInd) {
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
                        var key = (RotationEulerKey)track.keys[i];

                        pathList.Add(key.rotation);
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
        /// Grab tweener, create if it doesn't exists. keyInd is the index of this key in the track.
        /// </summary>
        TweenerCore<Vector3, Path, PathOptions> GetPathTween(int frameRate) {
            if((mPathTween == null || !mPathTween.active) && path.Length > 1) {
                var pathType = path.Length == 2 ? PathType.Linear : PathType.CatmullRom;

                var pathData = new Path(pathType, path, pathResolution);

                mPathTween = DOTween.To(PathPlugin.Get(), _Getter, _SetterNull, pathData, getTime(frameRate));

                mPathTween.SetRelative(false).SetOptions(isClosed);
            }

            return mPathTween;
        }

        Vector3 _Getter() { return rotation; }

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
        public Vector3 GetRotationFromPath(Transform transform, int frameRate, float t) {
            var tween = GetPathTween(frameRate);
            if(tween == null) //not tweenable
                return rotation;

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
                    return rotation;
            }

            return tween.PathGetPoint(finalT);
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
                if(interp == Interpolation.Curve && path.Length <= 1) return;
            }

            Transform target = obj as Transform;
            var transParent = target.parent;

            Rigidbody body = target.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? target.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;

            var rotTrack = (RotationEulerTrack)track;
            var axis = rotTrack.axis;

            float timeLength = getTime(frameRate);//1.0f / frameRate;

            switch(interp) {
                case Interpolation.None:
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
                    break;

                case Interpolation.Linear:
                    Vector3 endRotation = (track.keys[index + 1] as RotationEulerKey).rotation;

                    Tweener tween;

                    if(axis == AxisFlags.X) {
                        if(body)
                            tween = DOTween.To(new FloatPlugin(), () => rotation.x, (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(x, a.y, a.z) * transParent.rotation); }, endRotation.x, timeLength);
                        else
                            tween = DOTween.To(new FloatPlugin(), () => rotation.x, (x) => { var a = target.localEulerAngles; a.x = x; target.localEulerAngles = a; }, endRotation.x, timeLength);
                    }
                    else if(axis == AxisFlags.Y) {
                        if(body)
                            tween = DOTween.To(new FloatPlugin(), () => rotation.y, (y) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, y, a.z) * transParent.rotation); }, endRotation.y, timeLength);
                        else
                            tween = DOTween.To(new FloatPlugin(), () => rotation.y, (y) => { var a = target.localEulerAngles; a.y = y; target.localEulerAngles = a; }, endRotation.y, timeLength);
                    }
                    else if(axis == AxisFlags.Z) {
                        if(body2D)
                            tween = DOTween.To(new FloatPlugin(), () => rotation.z, (z) => { body2D.rotation = z + transParent.eulerAngles.z; }, endRotation.z, timeLength);
                        else if(body)
                            tween = DOTween.To(new FloatPlugin(), () => rotation.z, (z) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, a.y, z) * transParent.rotation); }, endRotation.z, timeLength);
                        else
                            tween = DOTween.To(new FloatPlugin(), () => rotation.z, (z) => { var a = target.localEulerAngles; a.z = z; target.localEulerAngles = a; }, endRotation.z, timeLength);
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
                    break;

                case Interpolation.Curve:
                    var pathTween = GetPathTween(frameRate);

                    if(axis == AxisFlags.X) {
                        if(body) {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(x.x, a.y, a.z) * transParent.rotation); };
                            pathTween.SetTarget(body);
                        }
                        else {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; a.x = x.x; target.localEulerAngles = a; };
                            pathTween.SetTarget(target);
                        }
                    }
                    else if(axis == AxisFlags.Y) {
                        if(body) {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, x.y, a.z) * transParent.rotation); };
                            pathTween.SetTarget(body);
                        }
                        else {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; a.y = x.y; target.localEulerAngles = a; };
                            pathTween.SetTarget(target);
                        }
                    }
                    else if(axis == AxisFlags.Z) {
                        if(body2D) {
                            pathTween.setter = (x) => { body2D.rotation = x.z + transParent.eulerAngles.z; };
                            pathTween.SetTarget(body2D);
                        }
                        else if(body) {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; body.MoveRotation(Quaternion.Euler(a.x, a.y, x.z) * transParent.rotation); };
                            pathTween.SetTarget(body);
                        }
                        else {
                            pathTween.setter = (x) => { var a = target.localEulerAngles; a.z = x.z; target.localEulerAngles = a; };
                            pathTween.SetTarget(target);
                        }
                    }
                    else if(axis == AxisFlags.All) {
                        if(body2D) {
                            pathTween.setter = (r) => body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                            pathTween.SetTarget(body2D);
                        }
                        else if(body) {
                            pathTween.setter = (r) => body.MoveRotation(Quaternion.Euler(r) * transParent.rotation);
                            pathTween.SetTarget(body);
                        }
                        else {
                            pathTween.setter = (r) => target.localEulerAngles = r;
                            pathTween.SetTarget(target);
                        }
                    }
                    else {
                        if(body2D) {
                            pathTween.setter = (r) => {
                                if((axis & AxisFlags.Z) != AxisFlags.None)
                                    body2D.MoveRotation(r.z + transParent.eulerAngles.z);
                            };
                            pathTween.SetTarget(body2D);
                        }
                        else if(body) {
                            pathTween.setter = (r) => {
                                var rot = target.localEulerAngles;
                                if((axis & AxisFlags.X) != AxisFlags.None)
                                    rot.x = r.x;
                                if((axis & AxisFlags.Y) != AxisFlags.None)
                                    rot.y = r.y;
                                if((axis & AxisFlags.Z) != AxisFlags.None)
                                    rot.z = r.z;
                                body.MoveRotation(Quaternion.Euler(rot) * transParent.rotation);
                            };
                            pathTween.SetTarget(body);
                        }
                        else {
                            pathTween.setter = (r) => {
                                var rot = target.localEulerAngles;
                                if((axis & AxisFlags.X) != AxisFlags.None)
                                    rot.x = r.x;
                                if((axis & AxisFlags.Y) != AxisFlags.None)
                                    rot.y = r.y;
                                if((axis & AxisFlags.Z) != AxisFlags.None)
                                    rot.z = r.z;
                                target.localEulerAngles = rot;
                            };
                            pathTween.SetTarget(target);
                        }
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