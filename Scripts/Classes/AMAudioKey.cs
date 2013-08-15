using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAudioKey : AMKey {

    public AudioSource audioSource;
    public AudioClip audioClip;
    public bool loop;

    public bool setAudioClip(AudioClip audioClip) {
        if(this.audioClip != audioClip) {
            this.audioClip = audioClip;
            return true;
        }
        return false;
    }

    public bool setLoop(bool loop) {
        if(this.loop != loop) {
            this.loop = loop;
            return true;
        }
        return false;
    }

    // copy properties from key
    public override AMKey CreateClone() {

        AMAudioKey a = gameObject.AddComponent<AMAudioKey>();
        a.enabled = false;
        a.frame = frame;
        a.audioClip = audioClip;
        a.loop = loop;

        return a;
    }

    #region action
    void OnMethodCallbackParams(TweenEvent dat) {
        if(audioSource) {
            float elapsed = dat.tween.elapsed;
            float frameRate = (float)dat.parms[0];
            float curFrame = frameRate * elapsed;
            float endFrame = frame + getNumberOfFrames(Mathf.RoundToInt(frameRate));

            //Debug.Log("audio t: "+elapsed+" play: " + curFrame + " end: " + endFrame);

            if(!loop && curFrame > endFrame) return;
            if(loop && audioSource.isPlaying && audioSource.clip == audioClip) return;

            audioSource.loop = loop;

            if(!dat.tween.isReversed)
                audioSource.time = (curFrame - ((float)frame)) / frameRate;
            else //TODO: not possible when we are playing the sequence in reverse, so whatever
                audioSource.time = 0.0f;

            audioSource.clip = audioClip;
            audioSource.pitch = 1.0f;

            audioSource.Play();
        }
    }

    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackParams, (float)frameRate);

        return null;
    }

    public ulong getTimeInSamples(int frequency, float time) {
        return (ulong)((44100 / frequency) * frequency * time);
    }
    public int getNumberOfFrames(int frameRate) {
        if(!audioClip) return -1;
        if(loop) return -1;
        return Mathf.CeilToInt(audioClip.length * frameRate);
    }
    #endregion
}
