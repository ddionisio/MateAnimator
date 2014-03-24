using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAnimationKey : AMKey {
    public GameObject obj;

    public WrapMode wrapMode;	// animation wrap mode
    public AnimationClip amClip;
    public bool crossfade = true;
    public float crossfadeTime = 0.3f;

    public bool setWrapMode(WrapMode wrapMode) {
        if(this.wrapMode != wrapMode) {
            this.wrapMode = wrapMode;
            return true;
        }
        return false;
    }

    public bool setAmClip(AnimationClip clip) {
        if(amClip != clip) {
            amClip = clip;
            return true;
        }
        return false;
    }

    public bool setCrossFade(bool crossfade) {
        if(this.crossfade != crossfade) {
            this.crossfade = crossfade;
            return true;
        }
        return false;
    }

    public bool setCrossfadeTime(float crossfadeTime) {
        if(this.crossfadeTime != crossfadeTime) {
            this.crossfadeTime = crossfadeTime;
            return true;
        }
        return false;
    }

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
        if(obj != null && obj.animation != null && amClip != null) {
            float elapsed = dat.tween.elapsed;
            float frameRate = (float)dat.parms[0];
            float curFrame = frameRate * elapsed;
            float numFrames = getNumberOfFrames(Mathf.RoundToInt(frameRate));

            if(numFrames > 0.0f && curFrame > frame + numFrames) return;

            Animation anm = obj.animation;

            if(wrapMode != WrapMode.Default)
                anm.wrapMode = wrapMode;

            if(crossfade) {
                anm.CrossFade(amClip.name, crossfadeTime);
            }
            else {
                anm.Play(amClip.name);
            }
        }
    }

    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackParams, (float)frameRate);

        return null;
    }
    #endregion
}
