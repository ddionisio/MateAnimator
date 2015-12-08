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

namespace MateAnimator {
    public struct AMPlugValueSetOptions {
        private Sequence mSeq;

        public AMPlugValueSetOptions(Sequence seq) {
            mSeq = seq;
        }

        public bool Refresh(ref int counter) {
            int _c = mSeq.CompletedLoops();
            if(counter != _c) {
                counter = _c;

                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Note: Set getter as the value to be passed to setter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AMPlugValueSet<T> : ABSTweenPlugin<T, T, AMPlugValueSetOptions> {
        private int mCounter = -1;

        public override T ConvertToStartValue(TweenerCore<T, T, AMPlugValueSetOptions> t, T value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugValueSetOptions options, Tween t, bool isRelative, DOGetter<T> getter, DOSetter<T> setter, float elapsed, T startValue, T changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            if(updateNotice == UpdateNotice.RewindStep)
                mCounter = -1;
            else if(options.Refresh(ref mCounter)) {
                setter(getter());
            }
        }

        public override float GetSpeedBasedDuration(AMPlugValueSetOptions options, float unitsXSecond, T changeValue) {
            return 1.0f/unitsXSecond;
        }

        public override void Reset(TweenerCore<T, T, AMPlugValueSetOptions> t) {
            mCounter = -1;
        }

        public override void SetChangeValue(TweenerCore<T, T, AMPlugValueSetOptions> t) { }

        public override void SetFrom(TweenerCore<T, T, AMPlugValueSetOptions> t, bool isRelative) { }

        public override void SetRelativeEndValue(TweenerCore<T, T, AMPlugValueSetOptions> t) { }
    }

    /// <summary>
    /// setter is passed the elapsed time, getter is not used
    /// </summary>
    public class AMPlugValueSetElapsed : ABSTweenPlugin<float, float, AMPlugValueSetOptions> {
        private int mCounter = -1;

        public override float ConvertToStartValue(TweenerCore<float, float, AMPlugValueSetOptions> t, float value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugValueSetOptions options, Tween t, bool isRelative, DOGetter<float> getter, DOSetter<float> setter, float elapsed, float startValue, float changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            if(updateNotice == UpdateNotice.RewindStep)
                mCounter = -1;
            else if(options.Refresh(ref mCounter))
                setter(elapsed);
        }

        public override float GetSpeedBasedDuration(AMPlugValueSetOptions options, float unitsXSecond, float changeValue) {
            return 1.0f/unitsXSecond;
        }

        public override void Reset(TweenerCore<float, float, AMPlugValueSetOptions> t) {
            mCounter = -1;
        }

        public override void SetChangeValue(TweenerCore<float, float, AMPlugValueSetOptions> t) { }

        public override void SetFrom(TweenerCore<float, float, AMPlugValueSetOptions> t, bool isRelative) { }

        public override void SetRelativeEndValue(TweenerCore<float, float, AMPlugValueSetOptions> t) { }
    }
}
