using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAudioKey : AMKey {

    public AudioClip audioClip;
    public bool loop;
	public bool oneShot;

    // copy properties from key
    public override void CopyTo(AMKey key) {

		AMAudioKey a = key as AMAudioKey;
        a.enabled = false;
        a.frame = frame;
        a.audioClip = audioClip;
        a.loop = loop;
    	a.oneShot = oneShot;
    }

    #region action
    
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        float sTime = getWaitTime(seq.take.frameRate, 0.0f);

		seq.sequence.InsertCallback(sTime, OnMethodCallbackParams, target as AudioSource);
    }

    public ulong getTimeInSamples(int frequency, float time) {
        return (ulong)((44100 / frequency) * frequency * time);
    }
    public override int getNumberOfFrames(int frameRate) {
        if(!audioClip || loop) return -1;
        return Mathf.CeilToInt(audioClip.length * (float)frameRate);
    }

	void OnMethodCallbackParams(TweenEvent dat) {
		if (dat.tween.isLoopingBack) return;
		AudioSource src = dat.parms[0] as AudioSource;
		if (src == null) return;
		
		// just incase you changed the preview speed before playing the game
		src.pitch = 1f;
		if (oneShot) {
			src.PlayOneShot(audioClip);
		} else {
			if ((src.isPlaying && src.clip == audioClip)) return;
			src.loop = loop;
			src.clip = audioClip;
			src.Play();
		}
	}

    #endregion
}
