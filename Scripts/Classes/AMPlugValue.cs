using System;
using UnityEngine;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;

/// <summary>
/// TODO: figure out using Play elegantly
/// </summary>
public class AMPlugAnimation : ABSTweenPlugin {
    Animation anim;
    AnimationState animState;
    WrapMode wrap;
    bool crossFade;
    float crossFadeTime;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugAnimation(Animation aAnim, string clipName, WrapMode aWrap, bool aCrossFade, float aCrossFadeTime) : base(null, false) {
        ignoreAccessor = true;
        anim = aAnim;
        animState = anim[clipName];
        wrap = aWrap;
        crossFade = aCrossFade;
        crossFadeTime = aCrossFadeTime;
    }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
    }

    protected override void SetIncremental(int p_diffIncr) { }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        if(crossFade) {
            Debug.Log("TODO " + crossFadeTime);
        }
        else {
            animState.enabled = true;
            animState.wrapMode = wrap;
            animState.time = p_totElapsed;
            animState.weight = 1.0f;
            anim.Sample();
            animState.enabled = false;
        }
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

public class AMPlugDouble : ABSTweenPlugin {
    internal static Type[] validPropTypes = { typeof(double) };
    internal static Type[] validValueTypes = { typeof(double) };

    double typedStartVal;
    double typedEndVal;
    double changeVal;

    protected override object startVal {
        get {
            return _startVal;
        }
        set {
            if(tweenObj.isFrom && isRelative) {
                _startVal = typedStartVal = typedEndVal + Convert.ToDouble(value);
            }
            else {
                _startVal = typedStartVal = Convert.ToDouble(value);
            }
        }
    }

    protected override object endVal {
        get {
            return _endVal;
        }
        set {
            _endVal = typedEndVal = Convert.ToDouble(value);
        }
    }
    
    public AMPlugDouble(double p_endVal)
        : base(p_endVal, false) {
    }

    public AMPlugDouble(double p_endVal, EaseType p_easeType)
        : base(p_endVal, p_easeType, false) {
    }

    public AMPlugDouble(double p_endVal, bool p_isRelative)
        : base(p_endVal, p_isRelative) {
    }

    public AMPlugDouble(double p_endVal, EaseType p_easeType, bool p_isRelative)
        : base(p_endVal, p_easeType, p_isRelative) {
    }

    public AMPlugDouble(double p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
        : base(p_endVal, p_easeAnimCurve, p_isRelative) { }

    protected override float GetSpeedBasedDuration(float p_speed) {
        float speedDur = Convert.ToSingle(changeVal / (double)p_speed);
        if(speedDur < 0) {
            speedDur = -speedDur;
        }
        return speedDur;
    }

    protected override void SetChangeVal() {
        if(isRelative && !tweenObj.isFrom) {
            changeVal = typedEndVal;
        }
        else {
            changeVal = typedEndVal - typedStartVal;
        }
    }

    protected override void SetIncremental(int p_diffIncr) {
        typedStartVal += changeVal * p_diffIncr;
    }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        float t = ease(p_totElapsed, 0.0f, 1.0f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);
        SetValue(typedStartVal + ((double)t) * (typedEndVal - typedStartVal));
    }
}

public class AMPlugLong : ABSTweenPlugin {
    internal static Type[] validPropTypes = { typeof(long) };
    internal static Type[] validValueTypes = { typeof(long) };

    double typedStartVal;
    double typedEndVal;
    double changeVal;

    protected override object startVal {
        get {
            return _startVal;
        }
        set {
            if(tweenObj.isFrom && isRelative) {
                _startVal = typedStartVal = typedEndVal + Convert.ToDouble(value);
            }
            else {
                _startVal = typedStartVal = Convert.ToDouble(value);
            }
        }
    }

    protected override object endVal {
        get {
            return _endVal;
        }
        set {
            _endVal = typedEndVal = Convert.ToDouble(value);
        }
    }

    public AMPlugLong(long p_endVal)
        : base(p_endVal, false) {
    }

    public AMPlugLong(long p_endVal, EaseType p_easeType)
        : base(p_endVal, p_easeType, false) {
    }

    public AMPlugLong(long p_endVal, bool p_isRelative)
        : base(p_endVal, p_isRelative) {
    }

    public AMPlugLong(long p_endVal, EaseType p_easeType, bool p_isRelative)
        : base(p_endVal, p_easeType, p_isRelative) {
    }

    public AMPlugLong(long p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
        : base(p_endVal, p_easeAnimCurve, p_isRelative) { }

    protected override float GetSpeedBasedDuration(float p_speed) {
        float speedDur = Convert.ToSingle(changeVal / (double)p_speed);
        if(speedDur < 0) {
            speedDur = -speedDur;
        }
        return speedDur;
    }

    protected override void SetChangeVal() {
        if(isRelative && !tweenObj.isFrom) {
            changeVal = typedEndVal;
        }
        else {
            changeVal = typedEndVal - typedStartVal;
        }
    }

    protected override void SetIncremental(int p_diffIncr) {
        typedStartVal += changeVal * p_diffIncr;
    }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        float t = ease(p_totElapsed, 0.0f, 1.0f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);
        SetValue(Convert.ToInt64(typedStartVal + ((double)t) * (typedEndVal - typedStartVal)));
    }
}