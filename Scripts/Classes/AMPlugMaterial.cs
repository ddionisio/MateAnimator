using UnityEngine;
using System;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;

namespace MateAnimator{
	public abstract class AMPlugMaterial : ABSTweenPlugin {
	    protected Material mMat;
	    protected int mPropId;

	    protected AMPlugMaterial(Material mat, string prop, object end, bool p_isRelative)
	        : base(end, p_isRelative) {
	        ignoreAccessor = true;
	        mMat = mat;
	        mPropId = Shader.PropertyToID(prop);
	    }

	    protected AMPlugMaterial(Material mat, string prop, object end, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(end, p_easeAnimCurve, p_isRelative) {
	        ignoreAccessor = true;
	        mMat = mat;
	        mPropId = Shader.PropertyToID(prop);
	    }

	    protected AMPlugMaterial(Material mat, string prop, object end, EaseType p_easeType, bool p_isRelative)
	        : base(end, p_easeType, p_isRelative) {
	        ignoreAccessor = true;
	        mMat = mat;
	        mPropId = Shader.PropertyToID(prop);
	    }
	}

	public class AMPlugMaterialFloat : AMPlugMaterial {
	    protected override object startVal {
	        get {
	            return _startVal;
	        }
	        set {
	            if(tweenObj.isFrom && isRelative) {
	                _startVal = mStart = mEnd + Convert.ToSingle(value);
	            }
	            else {
	                _startVal = mStart = Convert.ToSingle(value);
	            }
	        }
	    }

	    protected override object endVal {
	        get {
	            return _endVal;
	        }
	        set {
	            _endVal = mEnd = Convert.ToSingle(value);
	        }
	    }

	    private float mStart;
	    private float mEnd;
	    private float mChanged;

	    public AMPlugMaterialFloat(Material mat, string prop, float end)
	        : base(mat, prop, end, false) {
	    }

	    public AMPlugMaterialFloat(Material mat, string prop, float end, bool p_isRelative)
	        : base(mat, prop, end, p_isRelative) {
	    }

	    public AMPlugMaterialFloat(Material mat, string prop, float end, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, end, p_easeAnimCurve, p_isRelative) {
	    }

	    public AMPlugMaterialFloat(Material mat, string prop, float end, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, end, p_easeType, p_isRelative) {
	    }

	    protected override void SetValue(object p_value) {
	        mMat.SetFloat(mPropId, Convert.ToSingle(p_value));
	    }

	    protected override object GetValue() {
	        return mMat.GetFloat(mPropId);
	    }

	    protected override float GetSpeedBasedDuration(float p_speed) {
	        float speedDur = mChanged/p_speed;
	        if(speedDur < 0) {
	            speedDur = -speedDur;
	        }
	        return speedDur;
	    }

	    protected override void SetChangeVal() {
	        if(isRelative && !tweenObj.isFrom) {
	            mChanged = mEnd;
	            endVal = mStart + mEnd;
	        }
	        else {
	            mChanged = mEnd - mStart;
	        }
	    }

	    protected override void SetIncremental(int p_diffIncr) {
	        mStart += mChanged*p_diffIncr;
	    }

	    protected override void SetIncrementalRestart() {
	        float prevStartVal = mStart;
	        startVal = GetValue();
	        float diff = mStart - prevStartVal;
	        mEnd = mStart + diff;
	    }

	    protected override void DoUpdate(float p_totElapsed) {
	        float val = ease(p_totElapsed, mStart, mChanged, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);
	        if(tweenObj.pixelPerfect) val = (int)(val);
	        SetValue(val);
	    }
	}

	public class AMPlugMaterialVector4 : AMPlugMaterial {
	    protected override object startVal {
	        get {
	            return _startVal;
	        }
	        set {
	            if(tweenObj.isFrom && isRelative) {
	                _startVal = mStart = mEnd + (Vector4)value;
	            }
	            else {
	                _startVal = mStart = (Vector4)value;
	            }
	        }
	    }

	    protected override object endVal {
	        get {
	            return _endVal;
	        }
	        set {
	            _endVal = mEnd = (Vector4)value;
	        }
	    }

	    private Vector4 mStart;
	    private Vector4 mEnd;
	    private Vector4 mChanged;

	    public AMPlugMaterialVector4(Material mat, string prop, Vector4 p_endVal)
	        : base(mat, prop, p_endVal, false) { }
	        
	    public AMPlugMaterialVector4(Material mat, string prop, Vector4 p_endVal, EaseType p_easeType)
	        : base(mat, prop, p_endVal, p_easeType, false) { }

	    public AMPlugMaterialVector4(Material mat, string prop, Vector4 p_endVal, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_isRelative) { }

	    public AMPlugMaterialVector4(Material mat, string prop, Vector4 p_endVal, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeType, p_isRelative) { }

	    public AMPlugMaterialVector4(Material mat, string prop, Vector4 p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeAnimCurve, p_isRelative) { }

	    protected override void SetValue(object p_value) {
	        mMat.SetVector(mPropId, (Vector4)p_value);
	    }

	    protected override void SetValue(Vector4 p_value) {
	        mMat.SetVector(mPropId, p_value);
	    }

	    protected override object GetValue() {
	        return mMat.GetVector(mPropId);
	    }

	    protected override float GetSpeedBasedDuration(float p_speed) {
	        float speedDur = mChanged.magnitude/p_speed;
	        if(speedDur < 0) {
	            speedDur = -speedDur;
	        }
	        return speedDur;
	    }

	    protected override void SetChangeVal() {
	        if(isRelative && !tweenObj.isFrom) {
	            mChanged = mEnd;
	            endVal = mStart + mEnd;
	        }
	        else {
	            mChanged = new Vector4(mEnd.x - mStart.x, mEnd.y - mStart.y, mEnd.z - mStart.z, mEnd.w - mStart.w);
	        }
	    }

	    protected override void SetIncremental(int p_diffIncr) {
	        mStart += mChanged*p_diffIncr;
	    }

	    protected override void SetIncrementalRestart() {
	        Vector4 prevStartVal = mStart;
	        startVal = GetValue();
	        Vector4 diff = mStart - prevStartVal;
	        mEnd = mStart + diff;
	    }

	    protected override void DoUpdate(float p_totElapsed) {
	        float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

	        SetValue(new Vector4(
	            mStart.x + mChanged.x * time,
	            mStart.y + mChanged.y * time,
	            mStart.z + mChanged.z * time,
	            mStart.w + mChanged.w * time));
	    }
	}

	public class AMPlugMaterialColor : AMPlugMaterial {
	    protected override object startVal {
	        get {
	            return _startVal;
	        }
	        set {
	            if(tweenObj.isFrom && isRelative) {
	                _startVal = mStart = mEnd + (Color)value;
	            }
	            else {
	                _startVal = mStart = (Color)value;
	            }
	        }
	    }

	    protected override object endVal {
	        get {
	            return _endVal;
	        }
	        set {
	            _endVal = mEnd = (Color)value;
	        }
	    }

	    private Color mStart;
	    private Color mEnd;
	    private Color mChanged;

	    public AMPlugMaterialColor(Material mat, string prop, Color p_endVal)
	        : base(mat, prop, p_endVal, false) {
	    }

	    public AMPlugMaterialColor(Material mat, string prop, Color p_endVal, EaseType p_easeType)
	        : base(mat, prop, p_endVal, p_easeType, false) { }

	    public AMPlugMaterialColor(Material mat, string prop, Color p_endVal, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_isRelative) { }

	    public AMPlugMaterialColor(Material mat, string prop, Color p_endVal, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeType, p_isRelative) { }

	    public AMPlugMaterialColor(Material mat, string prop, Color p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeAnimCurve, p_isRelative) { }

	    protected override void SetValue(object p_value) {
	        mMat.SetColor(mPropId, (Color)p_value);
	    }

	    protected override void SetValue(Color p_value) {
	        mMat.SetColor(mPropId, p_value);
	    }

	    protected override object GetValue() {
	        return mMat.GetColor(mPropId);
	    }

	    protected override float GetSpeedBasedDuration(float p_speed) {
	        float speedDur = 1f/p_speed;
	        if(speedDur < 0) {
	            speedDur = -speedDur;
	        }
	        return speedDur;
	    }

	    protected override void SetChangeVal() {
	        if(isRelative && !tweenObj.isFrom) {
	            mEnd = mStart + mEnd;
	        }

	        mChanged = mEnd - mStart;
	    }

	    protected override void SetIncremental(int p_diffIncr) {
	        mStart += mChanged*p_diffIncr;
	        mEnd += mChanged*p_diffIncr;
	    }

	    protected override void SetIncrementalRestart() {
	        Color prevStartVal = mStart;
	        startVal = (Color)GetValue();
	        Color diff = mStart - prevStartVal;
	        mEnd = mStart + diff;
	    }

	    protected override void DoUpdate(float p_totElapsed) {
	        float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

	        SetValue(new Color(
	            mStart.r + mChanged.r * time,
	            mStart.g + mChanged.g * time,
	            mStart.b + mChanged.b * time,
	            mStart.a + mChanged.a * time));
	    }
	}

	public class AMPlugMaterialVector2 : AMPlugMaterial {
	    protected override object startVal {
	        get {
	            return _startVal;
	        }
	        set {
	            if(tweenObj.isFrom && isRelative) {
	                _startVal = mStart = mEnd + (Vector2)value;
	            }
	            else {
	                _startVal = mStart = (Vector2)value;
	            }
	        }
	    }

	    protected override object endVal {
	        get {
	            return _endVal;
	        }
	        set {
	            _endVal = mEnd = (Vector2)value;
	        }
	    }

	    private Vector2 mStart;
	    private Vector2 mEnd;
	    private Vector2 mChanged;

	    public AMPlugMaterialVector2(Material mat, string prop, Vector2 p_endVal)
	        : base(mat, prop, p_endVal, false) { }

	    public AMPlugMaterialVector2(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType)
	        : base(mat, prop, p_endVal, p_easeType, false) { }

	    public AMPlugMaterialVector2(Material mat, string prop, Vector2 p_endVal, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_isRelative) { }

	    public AMPlugMaterialVector2(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeType, p_isRelative) { }

	    public AMPlugMaterialVector2(Material mat, string prop, Vector2 p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeAnimCurve, p_isRelative) { }

	    protected override void SetValue(object p_value) {
	        mMat.SetVector(mPropId, (Vector2)p_value);
	    }

	    protected override void SetValue(Vector2 p_value) {
	        mMat.SetVector(mPropId, p_value);
	    }

	    protected override object GetValue() {
	        return mMat.GetVector(mPropId);
	    }

	    protected override float GetSpeedBasedDuration(float p_speed) {
	        float speedDur = mChanged.magnitude/p_speed;
	        if(speedDur < 0) {
	            speedDur = -speedDur;
	        }
	        return speedDur;
	    }

	    protected override void SetChangeVal() {
	        if(isRelative && !tweenObj.isFrom) {
	            mChanged = mEnd;
	            endVal = mStart + mEnd;
	        }
	        else {
	            mChanged = new Vector2(mEnd.x - mStart.x, mEnd.y - mStart.y);
	        }
	    }

	    protected override void SetIncremental(int p_diffIncr) {
	        mStart += mChanged*p_diffIncr;
	    }

	    protected override void SetIncrementalRestart() {
	        Vector2 prevStartVal = mStart;
	        startVal = GetValue();
	        Vector2 diff = mStart - prevStartVal;
	        mEnd = mStart + diff;
	    }

	    protected override void DoUpdate(float p_totElapsed) {
	        float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

	        if(tweenObj.pixelPerfect) {
	            SetValue(new Vector2(
	                (int)(mStart.x + mChanged.x * time),
	                (int)(mStart.y + mChanged.y * time)
	            ));
	        }
	        else {
	            SetValue(new Vector2(
	                mStart.x + mChanged.x * time,
	                mStart.y + mChanged.y * time
	            ));
	        }
	    }
	}

	public class AMPlugMaterialTexOfs : AMPlugMaterialVector2 {
	    private string mPropName;

	    public AMPlugMaterialTexOfs(Material mat, string prop, Vector2 p_endVal)
	        : base(mat, prop, p_endVal, false) { mPropName = prop; }

	    public AMPlugMaterialTexOfs(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType)
	        : base(mat, prop, p_endVal, p_easeType, false) { mPropName = prop; }

	    public AMPlugMaterialTexOfs(Material mat, string prop, Vector2 p_endVal, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_isRelative) { mPropName = prop; }

	    public AMPlugMaterialTexOfs(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeType, p_isRelative) { mPropName = prop; }

	    public AMPlugMaterialTexOfs(Material mat, string prop, Vector2 p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeAnimCurve, p_isRelative) { mPropName = prop; }

	    protected override void SetValue(object p_value) {
	        mMat.SetTextureOffset(mPropName, (Vector2)p_value);
	    }

	    protected override void SetValue(Vector2 p_value) {
	        mMat.SetTextureOffset(mPropName, p_value);
	    }

	    protected override object GetValue() {
	        return mMat.GetTextureOffset(mPropName);
	    }
	}

	public class AMPlugMaterialTexScale : AMPlugMaterialVector2 {
	    private string mPropName;

	    public AMPlugMaterialTexScale(Material mat, string prop, Vector2 p_endVal)
	        : base(mat, prop, p_endVal, false) { mPropName = prop; }

	    public AMPlugMaterialTexScale(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType)
	        : base(mat, prop, p_endVal, p_easeType, false) { mPropName = prop; }

	    public AMPlugMaterialTexScale(Material mat, string prop, Vector2 p_endVal, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_isRelative) { mPropName = prop; }

	    public AMPlugMaterialTexScale(Material mat, string prop, Vector2 p_endVal, EaseType p_easeType, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeType, p_isRelative) { mPropName = prop; }

	    public AMPlugMaterialTexScale(Material mat, string prop, Vector2 p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
	        : base(mat, prop, p_endVal, p_easeAnimCurve, p_isRelative) { mPropName = prop; }

	    protected override void SetValue(object p_value) {
	        mMat.SetTextureScale(mPropName, (Vector2)p_value);
	    }

	    protected override void SetValue(Vector2 p_value) {
	        mMat.SetTextureScale(mPropName, p_value);
	    }

	    protected override object GetValue() {
	        return mMat.GetTextureScale(mPropName);
	    }
	}
}
