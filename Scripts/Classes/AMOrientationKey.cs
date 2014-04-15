using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;
using Holoville.HOTween.Plugins.Core;

public class AMPlugOrientation : ABSTweenPlugin {
    internal static System.Type[] validPropTypes = { typeof(Quaternion) };
    internal static System.Type[] validValueTypes = { typeof(Quaternion) };

    Transform sTarget;
    Transform eTarget;

    Quaternion changeVal;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugOrientation(Transform start, Transform end)
        : base(null, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, bool isRelative)
        : base(null, isRelative) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, EaseType easeType)
        : base(null, easeType, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, EaseType easeType, bool isRelative)
        : base(null, easeType, isRelative) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, AnimationCurve curve)
        : base(null, curve, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, AnimationCurve curve, bool isRelative)
        : base(null, curve, isRelative) { this.sTarget = start; this.eTarget = end; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
    }

    protected override void SetIncremental(int p_diffIncr) {
    }

    protected override void DoUpdate(float p_totElapsed) {
        Transform t = tweenObj.target as Transform;

        if(sTarget == null && eTarget == null)
            return;
        else if(sTarget == null)
            t.LookAt(eTarget);
        else if(eTarget == null || sTarget == eTarget)
            t.LookAt(sTarget);
        else {
            float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

            Quaternion s = Quaternion.LookRotation(sTarget.position - t.position);
            Quaternion e = Quaternion.LookRotation(eTarget.position - t.position);

            t.rotation = Quaternion.Slerp(s, e, time);
        }
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

[AddComponentMenu("")]
public class AMOrientationKey : AMKey {

	[SerializeField]
    Transform target;
	[SerializeField]
	string targetPath;
	[SerializeField]
	Transform endTarget;
	[SerializeField]
	string endTargetPath;

    public int endFrame;

	public void SetTarget(AMITarget itarget, Transform t) {
		targetPath = AMUtil.GetPath(itarget.TargetGetHolder(), t);
		if(itarget.TargetIsMeta()) {
			target = null;
			itarget.TargetSetCache(targetPath, t);
		}
		else {
			target = t;
		}
	}
	public Transform GetTarget(AMITarget itarget) {
		Transform ret = null;
		if(itarget.TargetIsMeta()) {
			ret = itarget.TargetGetCache(targetPath) as Transform;
			if(ret == null) {
				GameObject go = AMUtil.GetTarget(itarget.TargetGetHolder(), targetPath);
				if(go) {
					ret = go.transform;
					itarget.TargetSetCache(targetPath, ret);
				}
			}
		}
		else
			ret = target;
		return ret;
	}

	public void SetTargetEnd(AMITarget itarget, Transform t) {
		endTargetPath = AMUtil.GetPath(itarget.TargetGetHolder(), t);
		if(itarget.TargetIsMeta()) {
			endTarget = null;
			itarget.TargetSetCache(endTargetPath, t);
		}
		else {
			endTarget = t;
		}
	}
	public void SetTargetEnd(AMOrientationKey nextKey) {
		endTarget = nextKey.target;
		endTargetPath = nextKey.targetPath;
	}
	public Transform GetTargetEnd(AMITarget itarget) {
		Transform ret = null;
		if(itarget.TargetIsMeta()) {
			ret = itarget.TargetGetCache(endTargetPath) as Transform;
			if(ret == null) {
				GameObject go = AMUtil.GetTarget(itarget.TargetGetHolder(), endTargetPath);
				if(go) {
					ret = go.transform;
					itarget.TargetSetCache(endTargetPath, ret);
				}
			}
		}
		else
			ret = endTarget;
		return ret;
	}
        
    public override AMKey CreateClone(GameObject go) {

		AMOrientationKey a = go ? go.AddComponent<AMOrientationKey>() : gameObject.AddComponent<AMOrientationKey>();
        a.enabled = false;
        a.frame = frame;
        a.target = target;
		a.targetPath = targetPath;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
        return a;
    }

	public override int getNumberOfFrames() {
		return endFrame - frame;
	}
	
	public float getTime(int frameRate) {
		return (float)getNumberOfFrames() / (float)frameRate;
	}
	
	public bool isLookFollow(AMITarget itarget) {
		Transform tgt = GetTarget(itarget);
		Transform tgte = GetTargetEnd(itarget);
		return tgt == tgte;
	}
	
	public Quaternion getQuaternionAtPercent(AMITarget itarget, Transform obj, float percentage) {
		Transform tgt = GetTarget(itarget);
		Transform tgte = GetTargetEnd(itarget);
		if(tgt == tgte || easeType == EaseTypeNone) {
			return Quaternion.LookRotation(tgt.position - obj.position);
		}
		
		Quaternion s = Quaternion.LookRotation(tgt.position - obj.position);
		Quaternion e = Quaternion.LookRotation(tgte.position - obj.position);
		
		float time = 0.0f;
		
		if(hasCustomEase()) {
			time = AMUtil.EaseCustom(0.0f, 1.0f, percentage, easeCurve);
		}
		else {
			TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)easeType);
			time = ease(percentage, 0.0f, 1.0f, 1.0f, amplitude, period);
		}
		
		return Quaternion.Slerp(s, e, time);
	}

    #region action
    public override Tweener buildTweener(AMITarget itarget, Sequence sequence, UnityEngine.Object obj, int frameRate) {
        if(!obj) return null;
		if(easeType == EaseTypeNone) {
			return HOTween.To(obj, endFrame == -1 ? 1.0f/(float)frameRate : getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(GetTarget(itarget), null)));
		}
        if(endFrame == -1) return null;
		Transform tgt = GetTarget(itarget), tgte = GetTargetEnd(itarget);
		if(tgt == tgte) {
			return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, null)));
        }
        else {
            if(hasCustomEase()) {
				return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, tgte)).Ease(easeCurve));
            }
            else {
				return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, tgte)).Ease((EaseType)easeType, amplitude, period));
            }
        }
    }
    #endregion
}
