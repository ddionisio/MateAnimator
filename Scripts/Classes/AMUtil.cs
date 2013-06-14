using UnityEngine;
using System.Collections;

public struct AMUtil {
    public static float EaseCustom(float startValue, float changeValue, float time, AnimationCurve curve) {
        return startValue + changeValue * curve.Evaluate(time);
    }

    public static float EaseInExpoReversed(float start, float end, float value) {
        end -= start;
        return 1 + (Mathf.Log(value - start) / (10 * Mathf.Log(2)));
    }

    public static Holoville.HOTween.Core.TweenDelegate.EaseFunc GetEasingFunction(Holoville.HOTween.EaseType type) {
        switch(type) {
            case Holoville.HOTween.EaseType.Linear:
                return Holoville.HOTween.Core.Easing.Linear.EaseNone;
            case Holoville.HOTween.EaseType.EaseInSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInBack:
                return Holoville.HOTween.Core.Easing.Back.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutBack:
                return Holoville.HOTween.Core.Easing.Back.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutBack:
                return Holoville.HOTween.Core.Easing.Back.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseInOut;
            case Holoville.HOTween.EaseType.AnimationCurve:
                return null;
        }

        return null;
    }
}
