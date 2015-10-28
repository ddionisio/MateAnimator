using System;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace MateAnimator{
    public struct AMPlugAnimationOptions {
        public Animation anim;
        public AnimationState animState;
        public WrapMode wrap;
        public bool fadeIn;
        public float fadeInTime;
    }

	/// <summary>
	/// TODO: figure out using Play elegantly
	/// </summary>
    public class AMPlugAnimation : ABSTweenPlugin<int, int, AMPlugAnimationOptions> {

        public override int ConvertToStartValue(TweenerCore<int, int, AMPlugAnimationOptions> t, int value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugAnimationOptions options, Tween t, bool isRelative, DOGetter<int> getter, DOSetter<int> setter, float elapsed, int startValue, int changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            options.animState.enabled = true;
            options.animState.wrapMode = options.wrap;
            options.animState.time = elapsed;

            if(options.fadeIn && elapsed < options.fadeInTime)
                options.animState.weight = elapsed/options.fadeInTime;
            else
                options.animState.weight = 1.0f;

            options.anim.Sample();
            options.animState.enabled = false;
        }

        public override float GetSpeedBasedDuration(AMPlugAnimationOptions options, float unitsXSecond, int changeValue) {
            return ((float)changeValue)/unitsXSecond;
        }

        public override void Reset(TweenerCore<int, int, AMPlugAnimationOptions> t) { }
        public override void SetChangeValue(TweenerCore<int, int, AMPlugAnimationOptions> t) { }
        public override void SetFrom(TweenerCore<int, int, AMPlugAnimationOptions> t, bool isRelative) { }
        public override void SetRelativeEndValue(TweenerCore<int, int, AMPlugAnimationOptions> t) { }
	}

    public struct AMPlugAnimationCrossFadeOptions {
        public Animation anim;

        public AnimationState animState;
        public WrapMode wrap;
        public float startTime;
                
        public AnimationState prevAnimState;
        public WrapMode prevWrap;
        public float prevStartTime;

        public float crossFadeTime;
    }

	/// <summary>
	/// TODO: figure out using Play elegantly
	/// </summary>
    public class AMPlugAnimationCrossFade : ABSTweenPlugin<int, int, AMPlugAnimationCrossFadeOptions> {

        public override int ConvertToStartValue(TweenerCore<int, int, AMPlugAnimationCrossFadeOptions> t, int value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugAnimationCrossFadeOptions options, Tween t, bool isRelative, DOGetter<int> getter, DOSetter<int> setter, float elapsed, int startValue, int changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            if(elapsed < options.crossFadeTime) {
                float weight = elapsed / options.crossFadeTime;

                options.prevAnimState.enabled = true;
                options.prevAnimState.wrapMode = options.prevWrap;
                options.prevAnimState.weight = 1.0f - weight;
                options.prevAnimState.time = (options.startTime + elapsed) - options.prevStartTime;

                options.animState.enabled = true;
                options.animState.wrapMode = options.wrap;
                options.animState.weight = weight;
                options.animState.time = elapsed;

                options.anim.Sample();

                options.prevAnimState.enabled = false;
                options.animState.enabled = false;
            }
            else {
                options.animState.enabled = true;
                options.animState.wrapMode = options.wrap;
                options.animState.weight = 1.0f;
                options.animState.time = elapsed;

                options.anim.Sample();

                options.animState.enabled = false;
            }
        }

        public override float GetSpeedBasedDuration(AMPlugAnimationCrossFadeOptions options, float unitsXSecond, int changeValue) {
            return ((float)changeValue)/unitsXSecond;
        }

        public override void Reset(TweenerCore<int, int, AMPlugAnimationCrossFadeOptions> t) { }
        public override void SetChangeValue(TweenerCore<int, int, AMPlugAnimationCrossFadeOptions> t) { }
        public override void SetFrom(TweenerCore<int, int, AMPlugAnimationCrossFadeOptions> t, bool isRelative) { }
        public override void SetRelativeEndValue(TweenerCore<int, int, AMPlugAnimationCrossFadeOptions> t) { }
	}
}