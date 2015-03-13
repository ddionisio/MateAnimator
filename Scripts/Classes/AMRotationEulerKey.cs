using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMRotationEulerKey : AMKey {

    public Vector3 rotation;

    public int endFrame;

    // copy properties from key
    public override void CopyTo(AMKey key) {
        AMRotationEulerKey a = key as AMRotationEulerKey;
        a.enabled = false;
        a.frame = frame;
        a.rotation = rotation;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
    }

    #region action
    public override int getNumberOfFrames(int frameRate) {
        if(!canTween || (endFrame == -1 || endFrame == frame))
            return 1;
        else if(endFrame == -1)
            return -1;
        return endFrame - frame;
    }
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
        int frameRate = seq.take.frameRate;

        //allow tracks with just one key
        if(track.keys.Count == 1)
            interp = (int)Interpolation.None;

        if(!canTween) {
            seq.Insert(new AMActionTransLocalRotEuler(this, frameRate, obj as Transform, rotation));
        }
        else if(endFrame == -1) return;
        else {
            Vector3 endRotation = (track.keys[index + 1] as AMRotationEulerKey).rotation;

            if(hasCustomEase())
                seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("localEulerAngles", endRotation).Ease(easeCurve)));
            else
                seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("localEulerAngles", endRotation).Ease((EaseType)easeType, amplitude, period)));
        }
    }
    #endregion

}
