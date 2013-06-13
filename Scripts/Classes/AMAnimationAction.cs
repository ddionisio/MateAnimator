using UnityEngine;
using System.Collections;

[System.Serializable]
public class AMAnimationAction : AMAction {
	public AnimationClip amClip;
	public WrapMode wrapMode;
	public GameObject obj;
	public bool crossfade;
	public float crossfadeTime;
	
	public override void execute(int frameRate, float delay) {
		if(!amClip || !obj) return;
		AMTween.PlayAnimation(obj, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "animation", amClip.name, "wrapmode", wrapMode, "crossfade", crossfade, "fadeLength", crossfadeTime));
	}
	
	public override string ToString(int codeLanguage, int frameRate) {
		string s = "";
		if(!amClip) return null;
		if(codeLanguage == 0) {
			// c#
			s += "AMTween.PlayAnimation(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"animation\", \""+amClip.name+"\", \"wrapmode\", "+"WrapMode."+wrapMode.ToString()+",\"crossfade\", "+crossfade.ToString ().ToLower();
			if(crossfade) s += ", \"fadeLength\", "+crossfadeTime.ToString()+"f";
			s += "));";
		} else {
			// js
			s += "AMTween.PlayAnimation(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"animation\": \""+amClip.name+"\", \"wrapmode\": "+"WrapMode."+wrapMode.ToString()+",\"crossfade\": "+crossfade.ToString ().ToLower();
			if(crossfade) s += ", \"fadeLength\": "+crossfadeTime.ToString();
			s += "});";
		}
		return s;
	}
	
	// get number of frames, -1 is infinite
	public int getNumberOfFrames(int frameRate) {
		if(!amClip) return -1;
		if(wrapMode != WrapMode.Once) return -1;
		return Mathf.CeilToInt(amClip.length*frameRate);
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(!amClip || !obj) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.method = "playanimation";
		a.go = obj.gameObject.name;
		a.delay = getWaitTime(frameRate,0f);
		a.strings = new string[]{amClip.name};
		a.floats = new float[]{(float)wrapMode, crossfadeTime};
		a.bools = new bool[]{crossfade};
		
		return a;
	}
}
