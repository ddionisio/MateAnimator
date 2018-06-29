using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
    /// <summary>
    /// This is mostly used for compatibility when using DOTween Hyper Compatible
    /// </summary>
    public struct TweenPluginFactory {
#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector4WrapperPlugin CreateVector4() {
            return new Vector4WrapperPlugin();
        }
#else
        public static Vector4Plugin CreateVector4() {
            return new Vector4Plugin();
        }
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector3WrapperPlugin CreateVector3() {
            return new Vector3WrapperPlugin();
        }
#else
        public static Vector3Plugin CreateVector3() {
            return new Vector3Plugin();
        }
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static Vector2WrapperPlugin CreateVector2() {
            return new Vector2WrapperPlugin();
        }
#else
        public static Vector2Plugin CreateVector2() {
            return new Vector2Plugin();
        }
#endif

#if DOTWEEN_HYPER_COMPATIBLE
        public static ColorWrapperPlugin CreateColor() {
            return new ColorWrapperPlugin();
        }
#else
        public static ColorPlugin CreateColor() {
            return new ColorPlugin();
        }
#endif
    }
}