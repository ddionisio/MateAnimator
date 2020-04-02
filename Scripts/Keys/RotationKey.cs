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

        public override int keyCount { get { return path != null ? path.wps.Length : 1; } }

        public const int pathResolution = 10;

        //public int type = 0; // 0 = Rotate To, 1 = Look At
        public Quaternion rotation;

        public int endFrame;

        //curve-related TODO: use proper rotational spline
        public TweenPlugPath path { get { return _paths.Length > 0 ? _paths[0] : null; } } //save serialize size by using array (path has a lot of serialized fields)
        [SerializeField] TweenPlugPath[] _paths;

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(RotationTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    _paths = new TweenPlugPath[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    _paths = new TweenPlugPath[0];

                    if(keyInd + 1 < track.keys.Count) {
                        var nextKey = (RotationKey)track.keys[keyInd + 1];
                        endFrame = nextKey.frame;
                    }
                    else { //fail-safe
                        endFrame = -1;
                    }
                    break;

                case Interpolation.Curve:
                    //if there's more than 2 keys, and next key is curve, then it's more than 2 pts.
                    if(keyInd + 2 < track.keys.Count && track.keys[keyInd + 1].interp == Interpolation.Curve) {
                        var pathList = new List<TweenPlugPathPoint>();

                        for(int i = keyInd; i < track.keys.Count; i++) {
                            var key = (RotationKey)track.keys[i];

                            pathList.Add(new TweenPlugPathPoint(key.rotation.eulerAngles));
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
        public Quaternion GetRotationFromPath(float t) {
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

            return Quaternion.Euler(pt.valueVector3);
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
            }

            Transform trans = obj as Transform;
            var transParent = trans.parent;

            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;
            float time = getTime(frameRate);

            if(interp == Interpolation.None) {
                TweenerCore<Quaternion, Quaternion, TWeenPlugNoneOptions> valueTween;

                if(body2D)
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => body2D.rotation = (x * transParent.rotation).eulerAngles.z, rotation, time);
                else if(body)
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => body.rotation = x * transParent.rotation, rotation, time);
                else
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => trans.localRotation = x, rotation, time);

                seq.Insert(this, valueTween);
            }
            else if(interp == Interpolation.Linear || path == null) {
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
            }
            else if(interp == Interpolation.Curve) {
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart, isClosedPath = path.isClosed };

                DOSetter<Quaternion> setter;
                if(body2D)
                    setter = x => body2D.MoveRotation((x * transParent.rotation).eulerAngles.z);
                else if(body)
                    setter = x => body.MoveRotation(x * transParent.rotation);
                else
                    setter = x => trans.localRotation = x;

                var pathTween = DOTween.To(TweenPlugPathEuler.Get(), () => rotation, setter, path, time);

                if(hasCustomEase())
                    pathTween.SetEase(easeCurve);
                else
                    pathTween.SetEase(easeType, amplitude, period);

                seq.Insert(this, pathTween);
            }
        }
        #endregion
    }
}
 