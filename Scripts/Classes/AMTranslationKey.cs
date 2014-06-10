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
    public bool isLocal;
    public Vector3[] path;

    // copy properties from key
    public override void CopyTo(AMKey key) {
		AMTranslationKey a = key as AMTranslationKey;
        a.enabled = false;
        a.frame = frame;
        a.position = position;
        a.interp = interp;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
    }

    #region action
    public override int getStartFrame() {
        return startFrame;
    }

    public override int getNumberOfFrames(int frameRate) {
        if(easeType == EaseTypeNone && (endFrame == -1 || endFrame == startFrame))
            return 1;
        return  endFrame - startFrame;
    }
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
        int frameRate = seq.take.frameRate;
        if(easeType == EaseTypeNone) {
            //TODO: world position
            seq.Insert(new AMActionTransLocalPos(this, frameRate, obj as Transform, position));
        }
        else {
            if(path.Length <= 1) return;
            if(getNumberOfFrames(seq.take.frameRate) <= 0) return;

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

            seq.Insert(this, ret);
        }
    }
    #endregion
}
