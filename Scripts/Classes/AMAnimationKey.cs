using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAnimationKey : AMKey {
    public WrapMode wrapMode;	// animation wrap mode
    public AnimationClip amClip;
    public bool crossfade = true;
    public float crossfadeTime = 0.3f;

    // copy properties from key
    public override AMKey CreateClone(GameObject go) {

		AMAnimationKey a = go ? go.AddComponent<AMAnimationKey>() : gameObject.AddComponent<AMAnimationKey>();
        a.enabled = false;
        a.frame = frame;
        a.wrapMode = wrapMode;
        a.amClip = amClip;
        a.crossfade = crossfade;

        return a;
    }

    // get number of frames, -1 is infinite
    public int getNumberOfFrames(int frameRate) {
        if(!amClip) return -1;
        if(wrapMode != WrapMode.Once) return -1;
        return Mathf.CeilToInt(amClip.length * frameRate);
    }

    #region action
    void OnMethodCallbackParams(TweenEvent dat) {
		Animation anim = dat.parms[0] as Animation;
        if(anim != null && amClip != null) {
            float elapsed = dat.tween.elapsed;
            float frameRate = (float)dat.parms[1];
            float curFrame = frameRate * elapsed;
            float numFrames = getNumberOfFrames(Mathf.RoundToInt(frameRate));

            if(numFrames > 0.0f && curFrame > frame + numFrames) return;

            if(wrapMode != WrapMode.Default)
				anim.wrapMode = wrapMode;

            if(crossfade) {
				anim.CrossFade(amClip.name, crossfadeTime);
            }
            else {
				anim.Play(amClip.name);
            }
        }
    }

    public override Tweener buildTweener(Sequence sequence, UnityEngine.Object target, int frameRate) {
        sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackParams, target, (float)frameRate);

        return null;
    }
    #endregion
}
