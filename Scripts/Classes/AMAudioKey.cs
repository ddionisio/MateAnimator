using UnityEngine;
using System.Collections;

using DG.Tweening;

namespace MateAnimator{
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

            Sequence _seq = seq.sequence;
            AudioSource _src = target as AudioSource;

            _seq.InsertCallback(sTime, () => {
                //don't play when going backwards
                if(_seq.isBackwards) return;

                _src.pitch = _seq.timeScale;

                if(oneShot)
                    _src.PlayOneShot(audioClip);
                else {
                    if((_src.isPlaying && _src.clip == audioClip)) return;
                    _src.loop = loop;
                    _src.clip = audioClip;
                    _src.Play();
                }
            });
	    }

	    public ulong getTimeInSamples(int frequency, float time) {
	        return (ulong)((44100 / frequency) * frequency * time);
	    }
	    public override int getNumberOfFrames(int frameRate) {
	        if(!audioClip || loop) return -1;
	        return Mathf.CeilToInt(audioClip.length * (float)frameRate);
	    }
	    #endregion
	}
}
