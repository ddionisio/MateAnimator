using UnityEngine;
using System.Collections;

[System.Serializable]
public class AMAudioAction : AMAction {
	
	public AudioSource audioSource;
	public AudioClip audioClip;
	public bool loop;
	
	public override void execute(int frameRate, float delay) {
		if(!audioSource || !audioClip) return;
		AMTween.PlayAudio(audioSource, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "audioclip", audioClip, "loop", loop));
	}
	
	public string ToString(int codeLanguage, int frameRate, string audioClipVarName) {
		if((audioClip == null) || (audioSource == null)) return null;
		string s = "";
	
		if(codeLanguage == 0) {
			// c#
			s += "AMTween.PlayAudio(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"audioclip\", "+audioClipVarName+", \"loop\", "+loop.ToString ().ToLower()+"));";
		} else {
			// js
			s += "AMTween.PlayAudio(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"audioclip\": "+audioClipVarName+", \"loop\": "+loop.ToString ().ToLower()+"});";

		}
		return s;
	}
	
	public ulong getTimeInSamples(int frequency, float time) {
		return (ulong)((44100/frequency)*frequency*time);	
	}
	public int getNumberOfFrames(int frameRate) {
		if(!audioClip) return -1;
		if(loop) return -1;
		return Mathf.CeilToInt(audioClip.length*frameRate);
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(!audioSource || !audioClip) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.method = "playaudio";
		a.go = audioSource.gameObject.name;
		a.delay = getWaitTime(frameRate,0f);
		a.strings = new string[]{audioClip.name};
		a.bools = new bool[]{loop};
		
		return a;
	}
}
