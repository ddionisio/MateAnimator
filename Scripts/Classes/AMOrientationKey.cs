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

    public Transform target;

    public int endFrame;
    public Transform obj;
    public Transform endTarget;
    
    public bool setTarget(Transform target) {
        if(target != this.target) {
            this.target = target;
            return true;
        }
        return false;
    }

    public override AMKey CreateClone() {

        AMOrientationKey a = gameObject.AddComponent<AMOrientationKey>();
        a.enabled = false;
        a.frame = frame;
        a.target = target;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
        return a;
    }

    #region action
    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        if(!obj) return null;
        if(endFrame == -1) return null;
        if(isLookFollow()) {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(target, null)));
        }
        else {
            if(hasCustomEase()) {
                return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(target, endTarget)).Ease(easeCurve));
            }
            else {
                return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(target, endTarget)).Ease((EaseType)easeType));
            }
        }
    }

    public override int getNumberOfFrames() {
        return endFrame - frame;
    }

    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public bool isLookFollow() {
        if(target != endTarget) return false;
        return true;
    }

    public Quaternion getQuaternionAtPercent(float percentage) {
        if(isLookFollow()) {
            return Quaternion.LookRotation(target.position - obj.position);
        }

        Quaternion s = Quaternion.LookRotation(target.position - obj.position);
        Quaternion e = Quaternion.LookRotation(endTarget.position - obj.position);

        float time = 0.0f;

        if(hasCustomEase()) {
            time = AMUtil.EaseCustom(0.0f, 1.0f, percentage, easeCurve);
        }
        else {
            TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)easeType);
            time = ease(percentage, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f);
        }

        return Quaternion.Slerp(s, e, time);
    }
    #endregion
}
