using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[System.Serializable]
public class AMAudioAction : AMAction {

    public AudioSource audioSource;
    public AudioClip audioClip;
    public bool loop;

    public override void execute(int frameRate, float delay) {
        if(!audioSource || !audioClip) return;
        Debug.LogError("need implement");
        //AMTween.PlayAudio(audioSource, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "audioclip", audioClip, "loop", loop));
    }

    void OnMethodCallbackParams(TweenEvent dat) {
        if(audioSource) {
            float elapsed = dat.tween.elapsed;
            float frameRate = (float)dat.parms[0];
            float curFrame = frameRate * elapsed;
            float endFrame = startFrame + getNumberOfFrames(Mathf.RoundToInt(frameRate));

            //Debug.Log("audio t: "+elapsed+" play: " + curFrame + " end: " + endFrame);

            if(!loop && curFrame > endFrame) return;
            if(loop && audioSource.isPlaying && audioSource.clip == audioClip) return;

            audioSource.loop = loop;

            if(!dat.tween.isReversed)
                audioSource.time = (curFrame - ((float)startFrame)) / frameRate;
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

    public string ToString(int codeLanguage, int frameRate, string audioClipVarName) {
        if((audioClip == null) || (audioSource == null)) return null;
        string s = "";

        /*if(codeLanguage == 0) {
            // c#
            s += "AMTween.PlayAudio(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"audioclip\", "+audioClipVarName+", \"loop\", "+loop.ToString ().ToLower()+"));";
        } else {
            // js
            s += "AMTween.PlayAudio(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"audioclip\": "+audioClipVarName+", \"loop\": "+loop.ToString ().ToLower()+"});";

        }*/
        Debug.LogError("need implement");
        return s;
    }

    public ulong getTimeInSamples(int frequency, float time) {
        return (ulong)((44100 / frequency) * frequency * time);
    }
    public int getNumberOfFrames(int frameRate) {
        if(!audioClip) return -1;
        if(loop) return -1;
        return Mathf.CeilToInt(audioClip.length * frameRate);
    }

    public override AnimatorTimeline.JSONAction getJSONAction(int frameRate) {
        if(!audioSource || !audioClip) return null;
        AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
        a.method = "playaudio";
        a.go = audioSource.gameObject.name;
        a.delay = getWaitTime(frameRate, 0f);
        a.strings = new string[] { audioClip.name };
        a.bools = new bool[] { loop };

        return a;
    }
}
