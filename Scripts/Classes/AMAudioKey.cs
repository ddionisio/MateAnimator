using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAudioKey : AMKey {

    public AudioClip audioClip;
    public bool loop;

    // copy properties from key
    public override void CopyTo(AMKey key) {

		AMAudioKey a = key as AMAudioKey;
        a.enabled = false;
        a.frame = frame;
        a.audioClip = audioClip;
        a.loop = loop;
    }

    #region action
    
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        float sTime = getWaitTime(seq.take.frameRate, 0.0f);

        //if loop, get the last frame of the take and use that as the duration
        float f = (float)seq.take.frameRate;
        float eTime = loop ? (float)seq.take.getLastFrame()/f : sTime + (float)getNumberOfFrames(seq.take.frameRate)/f;

        seq.Insert(new AMActionAudioPlay(sTime, eTime, target as AudioSource, audioClip, loop));
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
