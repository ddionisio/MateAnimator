using System;
using UnityEngine;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;

public class AMPlugNoTween : ABSTweenPlugin {

	protected override object startVal { get { return _startVal; } set { _startVal = value; } }
	
	protected override object endVal { get { return _endVal; } set { _endVal = value; } }
	
	public AMPlugNoTween(object val)
	: base(val, false) {  }
	
	protected override float GetSpeedBasedDuration(float p_speed) {
		return p_speed;
	}
	
	protected override void SetChangeVal() {
		SetValue(_endVal);
	}
	
	protected override void SetIncremental(int p_diffIncr) {
		SetValue(_endVal);
	}
	
	protected override void DoUpdate(float p_totElapsed) {
		SetValue(_endVal);
	}
}

public class AMPlugSprite : ABSTweenPlugin {

	private SpriteRenderer mRender;
	private Sprite mSprite;

	protected override object startVal { get { return _startVal; } set { _startVal = value; } }
	
	protected override object endVal { get { return _endVal; } set { _endVal = value; } }
	
	public AMPlugSprite(SpriteRenderer renderer, Sprite val)
	: base(null, false) {  mRender = renderer; mSprite = val; }
	
	protected override float GetSpeedBasedDuration(float p_speed) {
		return p_speed;
	}
	
	protected override void SetChangeVal() {
		mRender.sprite = mSprite;
	}
	
	protected override void SetIncremental(int p_diffIncr) {}
	
	protected override void DoUpdate(float p_totElapsed) {
		mRender.sprite = mSprite;
	}

	protected override void SetValue(object p_value) { }
	protected override object GetValue() { return mSprite; }
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

    protected override void DoUpdate(float p_totElapsed) {
        float t = ease(p_totElapsed, 0.0f, 1.0f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);
        SetValue(Convert.ToInt64(typedStartVal + ((double)t) * (typedEndVal - typedStartVal)));
    }
}