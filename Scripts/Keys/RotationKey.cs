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
    public class RotationKey : PathKeyBase {
        public override SerializeType serializeType { get { return SerializeType.Rotation; } }

        //public int type = 0; // 0 = Rotate To, 1 = Look At
        public Quaternion rotation;

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

            var pt = path.GetPoint(finalT);

            return Quaternion.Euler(pt.valueVector3);
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            RotationKey a = key as RotationKey;

            //a.type = type;
            a.rotation = rotation;
        }

        protected override TweenPlugPathPoint GeneratePathPoint(Track track) {
            return new TweenPlugPathPoint(rotation.eulerAngles);
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

            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            int frameRate = seq.take.frameRate;
            float time = getTime(frameRate);

            if(interp == Interpolation.None) {
                TweenerCore<Quaternion, Quaternion, TWeenPlugNoneOptions> valueTween;

                if(body2D)
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body2D.rotation = (x * parent.rotation).eulerAngles.z;
                        else
                            body2D.rotation = x.eulerAngles.z;
                    }, rotation, time);
                else if(body)
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body.rotation = x * parent.rotation;
                        else
                            body.rotation = x;
                    }, rotation, time);
                else
                    valueTween = DOTween.To(TweenPlugValueSet<Quaternion>.Get(), () => trans.localRotation, (x) => trans.localRotation = x, rotation, time);

                seq.Insert(this, valueTween);
            }
            else if(interp == Interpolation.Linear || path == null) {
                Quaternion endRotation = (track.keys[index + 1] as RotationKey).rotation;

                TweenerCore<Quaternion, Quaternion, NoOptions> linearTween;

                if(body2D)
                    linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body2D.MoveRotation((x * parent.rotation).eulerAngles.z);
                        else
                            body2D.MoveRotation(x.eulerAngles.z);
                    }, endRotation, time);
                else if(body)
                    linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body.MoveRotation(x * parent.rotation);
                        else
                            body.MoveRotation(x);
                    }, endRotation, time);
                else
                    linearTween = DOTween.To(TweenPluginFactory.CreateQuaternion(), () => rotation, (x) => trans.localRotation = x, endRotation, time);

                if(hasCustomEase())
                    linearTween.SetEase(easeCurve);
                else
                    linearTween.SetEase(easeType, amplitude, period);

                seq.Insert(this, linearTween);
            }
            else if(interp == Interpolation.Curve) {
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart };

                DOSetter<Quaternion> setter;
                if(body2D)
                    setter = x => {
                        var parent = trans.parent;
                        if(parent)
                            body2D.MoveRotation((x * parent.rotation).eulerAngles.z);
                        else
                            body2D.MoveRotation(x.eulerAngles.z);
                    };
                else if(body)
                    setter = x => {
                        var parent = trans.parent;
                        if(parent)
                            body.MoveRotation(x * parent.rotation);
                        else
                            body.MoveRotation(x);
                    };
                else
                    setter = x => trans.localRotation = x;

                var pathTween = DOTween.To(TweenPlugPathEuler.Get(), () => rotation, setter, path, time);
                pathTween.plugOptions = options;

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
 