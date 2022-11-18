using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace M8.Animator {
    public struct TWeenPlugNoneOptions : IPlugOptions {
        void IPlugOptions.Reset() { }
    }

    /// <summary>
    /// Use to apply a single value within a tween.
    /// Note: Set getter as the current value to be compaired to the target value. setter is used to apply the target value to current value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TweenPlugValueSet<T> : ABSTweenPlugin<T, T, TWeenPlugNoneOptions> {
        public static TweenPlugValueSet<T> Get() {
            if(mInstance == null)
                mInstance = new TweenPlugValueSet<T>();
            return mInstance;
        }
        private static TweenPlugValueSet<T> mInstance;

        public override T ConvertToStartValue(TweenerCore<T, T, TWeenPlugNoneOptions> t, T value) {
            return t.endValue; //start value is the same as the end value
        }

        public override void EvaluateAndApply(TWeenPlugNoneOptions options, Tween t, bool isRelative, DOGetter<T> getter, DOSetter<T> setter, float elapsed, T startValue, T changeValue, float duration, bool usingInversePosition, int newCompletedSteps, UpdateNotice updateNotice) {            
            var curVal = getter();
            if(!curVal.Equals(startValue)) {
                setter(startValue);
            }
        }

        public override float GetSpeedBasedDuration(TWeenPlugNoneOptions options, float unitsXSecond, T changeValue) {
            return 1.0f / unitsXSecond;
        }

        public override void Reset(TweenerCore<T, T, TWeenPlugNoneOptions> t) { }
        public override void SetChangeValue(TweenerCore<T, T, TWeenPlugNoneOptions> t) { }
        public override void SetFrom(TweenerCore<T, T, TWeenPlugNoneOptions> t, bool isRelative) { }
        public override void SetFrom(TweenerCore<T, T, TWeenPlugNoneOptions> t, T fromValue, bool setImmediately, bool isRelative) { }
        public override void SetRelativeEndValue(TweenerCore<T, T, TWeenPlugNoneOptions> t) { }
    }
}
