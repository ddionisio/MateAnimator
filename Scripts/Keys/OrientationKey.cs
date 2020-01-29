using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace M8.Animator {
    [System.Serializable]
    public class OrientationKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Orientation; } }

        [SerializeField]
        Transform target = null;
        [SerializeField]
        string targetPath = "";

        public bool isTargetPath { get { return !string.IsNullOrEmpty(targetPath); } }

        public int endFrame;

        public void SetTarget(ITarget itarget, Transform t, bool usePath) {
            if(itarget.meta || usePath) {
                target = null;
                targetPath = Utility.GetPath(itarget.root, t);
                itarget.SetCache(targetPath, t);
            }
            else {
                target = t;
                targetPath = "";
            }
        }
        public void SetTargetDirect(Transform t, string path) {
            target = t;
            targetPath = path;
        }
        public Transform GetTarget(ITarget itarget) {
            Transform ret = null;
            if(itarget.meta) {
                if(!string.IsNullOrEmpty(targetPath)) {
                    ret = itarget.GetCache(targetPath);
                    if(ret == null) {
                        ret = Utility.GetTarget(itarget.root, targetPath);
                        itarget.SetCache(targetPath, ret);
                    }
                }
            }
            else
                ret = target;
            return ret;
        }

        public override void maintainKey(ITarget itarget, UnityEngine.Object targetObj) {
            if(itarget.meta) {
                if(string.IsNullOrEmpty(targetPath)) {
                    if(target) {
                        targetPath = Utility.GetPath(itarget.root, target);
                        itarget.SetCache(targetPath, target);
                    }
                }

                target = null;
            }
            else {
                if(!target) {
                    if(!string.IsNullOrEmpty(targetPath)) {
                        target = itarget.GetCache(targetPath);
                        if(!target)
                            target = Utility.GetTarget(itarget.root, targetPath);
                    }
                }

                targetPath = "";
            }
        }

        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as OrientationKey;

            a.target = target;
            a.targetPath = targetPath;
        }

        public override int getNumberOfFrames(int frameRate) {
            if(!canTween && (endFrame == -1 || endFrame == frame))
                return 1;
            else if(endFrame == -1)
                return -1;
            return endFrame - frame;
        }

        public Quaternion getQuaternionAtPercent(Transform obj, Transform tgt, Transform tgte, float percentage) {
            if(tgt == tgte || !canTween) {
                return Quaternion.LookRotation(tgt.position - obj.position);
            }

            Quaternion s = Quaternion.LookRotation(tgt.position - obj.position);
            Quaternion e = Quaternion.LookRotation(tgte.position - obj.position);

            float time = 0.0f;

            if(hasCustomEase()) {
                time = Utility.EaseCustom(0.0f, 1.0f, percentage, easeCurve);
            }
            else {
                var ease = Utility.GetEasingFunction(easeType);
                time = ease(percentage, 1.0f, amplitude, period);
            }

            return Quaternion.LerpUnclamped(s, e, time);
        }

        #region action
        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object obj) {
            if(!obj || (canTween && endFrame == -1)) return;

            int frameRate = seq.take.frameRate;

            Transform trans = obj as Transform;

            Transform sTarget = GetTarget(seq.target);
            Transform eTarget = canTween ? (track.keys[index + 1] as OrientationKey).GetTarget(seq.target) : null;

            var tween = DOTween.To(new FloatPlugin(), () => 0f, (x) => {
                if(sTarget == null && eTarget == null)
                    return;
                else if(sTarget == null)
                    trans.LookAt(eTarget);
                else if(eTarget == null || sTarget == eTarget)
                    trans.LookAt(sTarget);
                else {
                    Quaternion s = Quaternion.LookRotation(sTarget.position - trans.position);
                    Quaternion e = Quaternion.LookRotation(eTarget.position - trans.position);

                    trans.rotation = Quaternion.Lerp(s, e, x);
                }
            }, 1f, getTime(frameRate));

            if(sTarget != eTarget) {
                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase(easeType, amplitude, period);
            }
            else
                tween.SetEase(Ease.Linear);

            seq.Insert(this, tween);
        }
        #endregion
    }
}