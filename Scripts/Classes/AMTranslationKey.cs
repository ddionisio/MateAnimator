using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMTranslationKey : AMKey {

    public enum Interpolation {
        Curve = 0,
        Linear = 1
    }
    public static string[] InterpolationNames = new string[] { "Curve", "Linear" };
    public Vector3 position;
    public int interp = 0;			// interpolation

    public int startFrame;
    public int endFrame;
    public Transform obj;
    public bool isLocal;
    public Vector3[] path;

    public bool setInterpolation(int _interp) {
        if(_interp != interp) {
            interp = _interp;
            return true;
        }
        return false;
    }
    public bool setPosition(Vector3 position) {
        if(position != this.position) {
            this.position = position;
            return true;
        }
        return false;
    }

    // copy properties from key
    public override AMKey CreateClone() {

        AMTranslationKey a = gameObject.AddComponent<AMTranslationKey>();
        a.enabled = false;
        a.frame = frame;
        a.position = position;
        a.interp = interp;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);

        return a;
    }

    #region action
    public override int getStartFrame() {
        return startFrame;
    }

    public override int getNumberOfFrames() {
        return endFrame - startFrame;
    }

    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        if(!obj) return null;
        if(path.Length <= 1) return null;
        if(getNumberOfFrames() <= 0) return null;

        object tweenTarget = obj;
        string tweenProp = isLocal ? "localPosition" : "position";

        Tweener ret = null;

        if(hasCustomEase()) {
            if(path.Length == 2)
                ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false, PathType.Linear)).Ease(easeCurve));
            else
                ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false)).Ease(easeCurve));
        }
        else {
            if(path.Length == 2)
                ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false, PathType.Linear)).Ease((EaseType)easeType, amplitude, period));
            else
                ret = HOTween.To(tweenTarget, getTime(frameRate), new TweenParms().Prop(tweenProp, new PlugVector3Path(path, false)).Ease((EaseType)easeType, amplitude, period));
        }

        return ret;
    }
    #endregion
}
