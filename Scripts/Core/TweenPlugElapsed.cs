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
    public class TweenPlugElapsedCounter {
        private bool mIsRefreshed;

        public void Reset() {
            mIsRefreshed = false;
        }

        public bool Refresh() {
            if(!mIsRefreshed) {
                mIsRefreshed = true;
                return true;
            }

            return false;
        }
    }

    public struct TweenPlugElapsedOptions : IPlugOptions {
        private TweenPlugElapsedCounter mCounter;

        public void Reset() {
            if(mCounter != null)
                mCounter.Reset();
        }

        public TweenPlugElapsedOptions(TweenPlugElapsedCounter counter) {
            mCounter = counter;
        }

        public bool Refresh() {
            return mCounter.Refresh();
        }
    }

    /// <summary>
    /// setter is passed the normalized elapsed time, getter is not used
    /// </summary>
    public class TweenPlugElapsed : ABSTweenPlugin<float, float, TweenPlugElapsedOptions> {

        private static TweenPlugElapsed mInstance;

        public static TweenPlugElapsed Get() {
            if(mInstance == null)
                mInstance = new TweenPlugElapsed();
            return mInstance;
        }

        public override float ConvertToStartValue(TweenerCore<float, float, TweenPlugElapsedOptions> t, float value) {
            return value;
        }

        public override void EvaluateAndApply(TweenPlugElapsedOptions options, Tween t, bool isRelative, DOGetter<float> getter, DOSetter<float> setter, float elapsed, float startValue, float changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            if(updateNotice == UpdateNotice.RewindStep)
                options.Reset();
            else if(options.Refresh())
                setter(elapsed);
        }

        public override float GetSpeedBasedDuration(TweenPlugElapsedOptions options, float unitsXSecond, float changeValue) {
            return 1.0f / unitsXSecond;
        }

        public override void Reset(TweenerCore<float, float, TweenPlugElapsedOptions> t) {
        }

        public override void SetChangeValue(TweenerCore<float, float, TweenPlugElapsedOptions> t) { }

        public override void SetFrom(TweenerCore<float, float, TweenPlugElapsedOptions> t, bool isRelative) { }
        public override void SetFrom(TweenerCore<float, float, TweenPlugElapsedOptions> t, float fromValue, bool setImmediately) { }

        public override void SetRelativeEndValue(TweenerCore<float, float, TweenPlugElapsedOptions> t) { }
    }
}