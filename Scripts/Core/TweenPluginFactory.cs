using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Plugins;
using DG.Tweening.CustomPlugins;

namespace M8.Animator {
    /// <summary>
    /// This is mostly used for compatibility when using DOTween Hyper Compatible
    /// </summary>
    public struct TweenPluginFactory {
#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector4WrapperPlugin CreateVector4() {
        if(mVector4Plugin == null)
                mVector4Plugin = new Vector4WrapperPlugin();
            return mVector4Plugin;
        }
        private static Vector4WrapperPlugin mVector4Plugin;
#else
        public static Vector4Plugin CreateVector4() {
            if(mVector4Plugin == null)
                mVector4Plugin = new Vector4Plugin();
            return mVector4Plugin;
        }
        private static Vector4Plugin mVector4Plugin;
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector3WrapperPlugin CreateVector3() {
            if(mVector3Plugin == null)
                mVector3Plugin = new Vector3WrapperPlugin();
            return mVector3Plugin;
        }
        private static Vector3WrapperPlugin mVector3Plugin;
#else
        public static Vector3Plugin CreateVector3() {
            if(mVector3Plugin == null)
                mVector3Plugin = new Vector3Plugin();
            return mVector3Plugin;
        }
        private static Vector3Plugin mVector3Plugin;
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector2WrapperPlugin CreateVector2() {
            if(mVector2Plugin == null)
                mVector2Plugin = new Vector2WrapperPlugin();
            return mVector2Plugin;
        }
        private static Vector2WrapperPlugin mVector2Plugin;
#else
        public static Vector2Plugin CreateVector2() {
            if(mVector2Plugin == null)
                mVector2Plugin = new Vector2Plugin();
            return mVector2Plugin;
        }
        private static Vector2Plugin mVector2Plugin;
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static ColorWrapperPlugin CreateColor() {
            if(mColorPlugin == null)
                mColorPlugin = new ColorWrapperPlugin();
            return mColorPlugin;
        }
        private static ColorWrapperPlugin mColorPlugin;
#else
        public static ColorPlugin CreateColor() {
            if(mColorPlugin == null)
                mColorPlugin = new ColorPlugin();
            return mColorPlugin;
        }
        private static ColorPlugin mColorPlugin;
#endif

        public static FloatPlugin CreateFloat() {
            if(mFloatPlugin == null)
                mFloatPlugin = new FloatPlugin();
            return mFloatPlugin;
        }
        private static FloatPlugin mFloatPlugin;

        public static IntPlugin CreateInt() {
            if(mIntPlugin == null)
                mIntPlugin = new IntPlugin();
            return mIntPlugin;
        }
        private static IntPlugin mIntPlugin;

        public static LongPlugin CreateLong() {
            if(mLongPlugin == null)
                mLongPlugin = new LongPlugin();
            return mLongPlugin;
        }
        private static LongPlugin mLongPlugin;

        public static DoublePlugin CreateDouble() {
            if(mDoublePlugin == null)
                mDoublePlugin = new DoublePlugin();
            return mDoublePlugin;
        }
        private static DoublePlugin mDoublePlugin;

        public static RectPlugin CreateRect() {
            if(mRectPlugin == null)
                mRectPlugin = new RectPlugin();
            return mRectPlugin;
        }
        private static RectPlugin mRectPlugin;

        public static PureQuaternionPlugin CreateQuaternion() {
            if(mQuaternionPlugin == null)
                mQuaternionPlugin = new PureQuaternionPlugin();
            return mQuaternionPlugin;
        }
        private static PureQuaternionPlugin mQuaternionPlugin;
    }
}