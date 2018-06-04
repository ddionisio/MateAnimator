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
    public struct AMPlugValueSetOptions : IPlugOptions {
        private AMSequence mSeq;
        private int mLoopCount;

        public void Reset() {
            SetSequence(null);
        }

        public void SetSequence(AMSequence seq) {
            if(mSeq != seq) {
                if(mSeq != null)
                    mSeq.stepCompleteCallback -= OnStepComplete;

                mSeq = seq;

                if(mSeq != null)
                    mSeq.stepCompleteCallback += OnStepComplete;
            }

            mLoopCount = 0;
        }

        public bool Refresh(ref int counter) {
            if(counter != mLoopCount) {
                counter = mLoopCount;

                return true;
            }
            return false;
        }

        void OnStepComplete() {
            mLoopCount++;
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
