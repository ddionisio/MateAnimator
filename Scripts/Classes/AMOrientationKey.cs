using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace MateAnimator{
    public struct AMPlugOrientationOptions {
        public Transform start;
        public Transform end;
    }

    public class AMPlugOrientation : ABSTweenPlugin<Quaternion, Quaternion, AMPlugOrientationOptions> {

        public override Quaternion ConvertToStartValue(TweenerCore<Quaternion, Quaternion, AMPlugOrientationOptions> t, Quaternion value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugOrientationOptions options, Tween t, bool isRelative, DOGetter<Quaternion> getter, DOSetter<Quaternion> setter, float elapsed, Quaternion startValue, Quaternion changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            Transform targetTrans = t.target as Transform;
            Transform sTarget = options.start, eTarget = options.end;

            if(sTarget == null && eTarget == null)
                return;
            else if(sTarget == null)
                targetTrans.LookAt(eTarget);
            else if(eTarget == null || sTarget == eTarget)
                targetTrans.LookAt(sTarget);
            else {
                float time = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);

                Quaternion s = Quaternion.LookRotation(sTarget.position - t.position);
                Quaternion e = Quaternion.LookRotation(eTarget.position - t.position);

                setter(Quaternion.LerpUnclamped(s, e, time));
            }
        }

        public override float GetSpeedBasedDuration(AMPlugOrientationOptions options, float unitsXSecond, Quaternion changeValue) {
            return changeValue.eulerAngles.magnitude / unitsXSecond;
        }

        public override void Reset(TweenerCore<Quaternion, Quaternion, AMPlugOrientationOptions> t) {
        }

        public override void SetChangeValue(TweenerCore<Quaternion, Quaternion, AMPlugOrientationOptions> t) {
            if(t.plugOptions.start && t.plugOptions.end)
                t.changeValue = t.plugOptions.end.rotation * Quaternion.Inverse(t.plugOptions.start.rotation);
        }

        public override void SetFrom(TweenerCore<Quaternion, Quaternion, AMPlugOrientationOptions> t, bool isRelative) {
        }

        public override void SetRelativeEndValue(TweenerCore<Quaternion, Quaternion, AMPlugOrientationOptions> t) {
        }
	}

	[AddComponentMenu("")]
	public class AMOrientationKey : AMKey {

		[SerializeField]
	    Transform target;
		[SerializeField]
		string targetPath;

	    public int endFrame;

		public void SetTarget(AMITarget itarget, Transform t) {
			if(itarget.isMeta) {
	            target = null;
	            targetPath = AMUtil.GetPath(itarget.root, t);
				itarget.SetCache(targetPath, t);
			}
			else {
				target = t;
				targetPath = "";
			}
		}
		public Transform GetTarget(AMITarget itarget) {
			Transform ret = null;
			if(itarget.isMeta) {
				if(!string.IsNullOrEmpty(targetPath)) {
					ret = itarget.GetCache(targetPath);
					if(ret == null) {
						ret = AMUtil.GetTarget(itarget.root, targetPath);
	                    itarget.SetCache(targetPath, ret);
					}
				}
			}
			else
				ret = target;
			return ret;
		}

		public override void maintainKey(AMITarget itarget, UnityEngine.Object targetObj) {
			if(itarget.isMeta) {
				if(string.IsNullOrEmpty(targetPath)) {
					if(target) {
						targetPath = AMUtil.GetPath(itarget.root, target);
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
							target = AMUtil.GetTarget(itarget.root, targetPath);
					}
				}

				targetPath = "";
			}
		}

	    public override void CopyTo(AMKey key) {
			AMOrientationKey a = key as AMOrientationKey;
	        a.enabled = false;
	        a.frame = frame;
	        a.target = target;
			a.targetPath = targetPath;
	        a.easeType = easeType;
	        a.customEase = new List<float>(customEase);
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
				time = AMUtil.EaseCustom(0.0f, 1.0f, percentage, easeCurve);
			}
			else {
				var ease = AMUtil.GetEasingFunction((Ease)easeType);
				time = ease(percentage, 1.0f, amplitude, period);
			}
			
			return Quaternion.LerpUnclamped(s, e, time);
		}

	    #region action
	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
	        if(!obj || (canTween && endFrame == -1)) return;

            int frameRate = seq.take.frameRate;

            Transform trans = obj as Transform;

            Transform sTarget = GetTarget(seq.target);
            Transform eTarget = canTween ? (track.keys[index+1] as AMOrientationKey).GetTarget(seq.target) : null;

            var tween = DOTween.To(new AMPlugOrientation(), () => trans.rotation, (x) => trans.rotation=x, trans.rotation, getTime(frameRate));

            tween.plugOptions = new AMPlugOrientationOptions() { start=sTarget, end=eTarget };

            if(sTarget != eTarget) {
                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase((Ease)easeType, amplitude, period);
            }
            else
                tween.SetEase(Ease.Linear);

            seq.Insert(this, tween);
	    }
	    #endregion
	}
}
