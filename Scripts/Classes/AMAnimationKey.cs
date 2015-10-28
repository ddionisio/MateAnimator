using UnityEngine;
using System.Collections;

using DG.Tweening;
using DG.Tweening.Plugins.Core;

namespace MateAnimator{
	[AddComponentMenu("")]
	public class AMAnimationKey : AMKey {
	    public WrapMode wrapMode;	// animation wrap mode
	    public AnimationClip amClip;
	    public bool crossfade = true;
	    public float crossfadeTime = 0.3f;

	    // copy properties from key
	    public override void CopyTo(AMKey key) {

	        AMAnimationKey a = key as AMAnimationKey;
	        a.enabled = false;
	        a.frame = frame;
	        a.wrapMode = wrapMode;
	        a.amClip = amClip;
	        a.crossfade = crossfade;
	    }

	    // get number of frames, -1 is infinite
	    public override int getNumberOfFrames(int frameRate) {
	        if(!amClip) return -1;
	        //if(wrapMode != WrapMode.Once) return -1;
	        return Mathf.CeilToInt(amClip.length * (float)frameRate);
	    }

	    #region action
	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
	        int frameRate = seq.take.frameRate;
	        float waitTime = getWaitTime(frameRate, 0.0f);
	        Animation anim = (target as GameObject).GetComponent<Animation>();

	        float duration = wrapMode == WrapMode.Once ? amClip.length : ((seq.take.getLastFrame()-frame)+1)/(float)frameRate;

	        if(crossfade) {
	            if(index > 0) {
	                AMAnimationKey prevKey = track.keys[index - 1] as AMAnimationKey;

                    var tween = DOTween.To(new AMPlugAnimationCrossFade(), () => 0, (x) => { }, 0, duration);
                    tween.plugOptions = new AMPlugAnimationCrossFadeOptions() {
                        anim=anim,
                        crossFadeTime=crossfadeTime,
                        prevAnimState=anim[prevKey.amClip.name],
                        prevWrap=prevKey.wrapMode,
                        prevStartTime=prevKey.getWaitTime(frameRate, 0.0f),
                        animState=anim[amClip.name],
                        wrap=wrapMode,
                        startTime=waitTime
                    };

                    seq.Insert(this, tween);
	            }
                else {
                    var tween = DOTween.To(new AMPlugAnimation(), () => 0, (x) => { }, 0, duration);
                    tween.plugOptions = new AMPlugAnimationOptions() {
                        anim=anim,
                        animState=anim[amClip.name],
                        wrap=wrapMode,
                        fadeIn=true,
                        fadeInTime=crossfadeTime
                    };

                    seq.Insert(this, tween);
                }
	        }
            else {
                var tween = DOTween.To(new AMPlugAnimation(), () => 0, (x) => { }, 0, duration);
                tween.plugOptions = new AMPlugAnimationOptions() {
                    anim=anim,
                    animState=anim[amClip.name],
                    wrap=wrapMode,
                    fadeIn=false,
                    fadeInTime=0f
                };

                seq.Insert(this, tween);
            }
	    }
	    #endregion
	}
}
