using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;

public class AMPlugQuaternionSlerp : ABSTweenPlugin {
    // VARS ///////////////////////////////////////////////////

    internal static System.Type[] validPropTypes = { typeof(Quaternion) };
    internal static System.Type[] validValueTypes = { typeof(Quaternion) };

    Quaternion typedStartVal;
    Quaternion typedEndVal;
    Quaternion changeVal;

    // GETS/SETS //////////////////////////////////////////////

    /// <summary>
    /// Gets the untyped start value,
    /// sets both the untyped and the typed start value.
    /// </summary>
    protected override object startVal {
        get {
            return _startVal;
        }
        set {
            if(tweenObj.isFrom && isRelative) {
                typedStartVal = typedEndVal * ((Quaternion)value);
                _startVal = typedStartVal;
            }
            else {
                _startVal = value;
                typedStartVal = (Quaternion)value;
                //                    _startVal = value;
                //                    typedStartVal = (value is Quaternion ? ((Quaternion)value).eulerAngles : (Vector3)value);
            }
        }
    }

    /// <summary>
    /// Gets the untyped end value,
    /// sets both the untyped and the typed end value.
    /// </summary>
    protected override object endVal {
        get {
            return _endVal;
        }
        set {
            _endVal = value;
            typedEndVal = (Quaternion)value;

            //                _endVal = value;
            //                typedEndVal = (value is Quaternion ? ((Quaternion)value).eulerAngles : (Vector3)value);
        }
    }


    // ***********************************************************************************
    // CONSTRUCTOR
    // ***********************************************************************************

    /// <summary>
    /// Creates a new instance of this plugin using the main ease type.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Quaternion"/> value to tween to.
    /// </param>
    public AMPlugQuaternionSlerp(Quaternion p_endVal)
        : base(p_endVal, false) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Quaternion"/> value to tween to.
    /// </param>
    /// <param name="p_easeType">
    /// The <see cref="EaseType"/> to use.
    /// </param>
    public AMPlugQuaternionSlerp(Quaternion p_endVal, EaseType p_easeType)
        : base(p_endVal, p_easeType, false) { }

    /// <summary>
    /// Creates a new instance of this plugin using the main ease type.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Quaternion"/> value to tween to.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Quaternion p_endVal, bool p_isRelative)
        : base(p_endVal, p_isRelative) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Quaternion"/> value to tween to.
    /// </param>
    /// <param name="p_easeType">
    /// The <see cref="EaseType"/> to use.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Quaternion p_endVal, EaseType p_easeType, bool p_isRelative)
        : base(p_endVal, p_easeType, p_isRelative) { }

    /// <summary>
    /// Creates a new instance of this plugin using the main ease type.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Vector3"/> euler angles to tween to.
    /// </param>
    public AMPlugQuaternionSlerp(Vector3 p_endVal)
        : base(Quaternion.Euler(p_endVal), false) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Vector3"/> euler angles to tween to.
    /// </param>
    /// <param name="p_easeType">
    /// The <see cref="EaseType"/> to use.
    /// </param>
    public AMPlugQuaternionSlerp(Vector3 p_endVal, EaseType p_easeType)
        : base(Quaternion.Euler(p_endVal), p_easeType, false) { }

    /// <summary>
    /// Creates a new instance of this plugin using the main ease type.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Vector3"/> euler angles to tween to.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Vector3 p_endVal, bool p_isRelative)
        : base(Quaternion.Euler(p_endVal), p_isRelative) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Vector3"/> euler angles to tween to.
    /// </param>
    /// <param name="p_easeType">
    /// The <see cref="EaseType"/> to use.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Vector3 p_endVal, EaseType p_easeType, bool p_isRelative)
        : base(Quaternion.Euler(p_endVal), p_easeType, p_isRelative) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Vector3"/> value to tween to.
    /// </param>
    /// <param name="p_easeAnimCurve">
    /// The <see cref="AnimationCurve"/> to use for easing.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Vector3 p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
        : base(Quaternion.Euler(p_endVal), p_easeAnimCurve, p_isRelative) { }

    /// <summary>
    /// Creates a new instance of this plugin.
    /// </summary>
    /// <param name="p_endVal">
    /// The <see cref="Quaternion"/> value to tween to.
    /// </param>
    /// <param name="p_easeAnimCurve">
    /// The <see cref="AnimationCurve"/> to use for easing.
    /// </param>
    /// <param name="p_isRelative">
    /// If <c>true</c>, the given end value is considered relative instead than absolute.
    /// </param>
    public AMPlugQuaternionSlerp(Quaternion p_endVal, AnimationCurve p_easeAnimCurve, bool p_isRelative)
        : base(p_endVal, p_easeAnimCurve, p_isRelative) { }

    // ===================================================================================
    // PARAMETERS ------------------------------------------------------------------------

    // ===================================================================================
    // METHODS ---------------------------------------------------------------------------

    /// <summary>
    /// Returns the speed-based duration based on the given speed x second.
    /// </summary>
    protected override float GetSpeedBasedDuration(float p_speed) {
        //TODO: not quite sure
        float speedDur = changeVal.eulerAngles.magnitude / (p_speed * 360);
        if(speedDur < 0) {
            speedDur = -speedDur;
        }
        return speedDur;
    }

    /// <summary>
    /// Sets the typed changeVal based on the current startVal and endVal.
    /// </summary>
    protected override void SetChangeVal() {
        //TODO: not quite sure
        changeVal = typedEndVal;
    }

    /// <summary>
    /// Sets the correct values in case of Incremental loop type.
    /// </summary>
    /// <param name="p_diffIncr">
    /// The difference from the previous loop increment.
    /// </param>
    protected override void SetIncremental(int p_diffIncr) {
        //???
        typedStartVal = Quaternion.RotateTowards(typedStartVal, typedEndVal, (float)p_diffIncr);
    }

    /// <summary>
    /// Updates the tween.
    /// </summary>
    /// <param name="p_totElapsed">
    /// The total elapsed time since startup.
    /// </param>
    protected override void DoUpdate(float p_totElapsed) {
        float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

        SetValue(Quaternion.Slerp(typedStartVal, typedEndVal, time));
    }
}
