using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// holds an action to be parsed in game view
[System.Serializable]
public class AMAction : ScriptableObject  {
	public int startFrame;
	public int easeType = (int)AMTween.EaseType.linear; 			// ease type, AMTween.EaseType enum
	public List<float> customEase = new List<float>();
	private AnimationCurve _cachedEaseCurve;
	public AnimationCurve easeCurve {
		get {
			if(_cachedEaseCurve == null || _cachedEaseCurve.keys.Length <= 0) _cachedEaseCurve = getCustomEaseCurve();
			return _cachedEaseCurve;
		}
	}
	
	public virtual string ToString(int codeLanguage, int frameRate) {
		return "(Error: No override for ToString)";
	}
	public virtual void execute(int frameRate, float delayModifier) {
		Debug.LogError ("Animator: No override for execute.");	
	}
	
	public float getWaitTime(int frameRate, float delay) {
		return ((float)startFrame-1f)/(float)frameRate - delay;
	}
	
	public virtual int getNumberOfFrames() {
		return 1;
	}
	public void destroy() {
		Object.DestroyImmediate(this);	
	}
	
	public virtual AnimatorTimeline.JSONAction getJSONAction(int frameRate) {
		return null;
	}
	
	public void setCustomEase(AnimationCurve curve) {
		customEase = new List<float>();
		foreach(Keyframe k in curve.keys) {
			customEase.Add(k.time);
			customEase.Add(k.value);
			customEase.Add(k.inTangent);
			customEase.Add(k.outTangent);
		}
	}
	
	public AnimationCurve getCustomEaseCurve() {
		
		AnimationCurve curve = new AnimationCurve();
		if(customEase.Count < 0) {
			return curve;
		}
		if(customEase.Count % 4 != 0) {
			Debug.LogError("Animator: Error retrieving custom ease.");
			return curve;
		}
		for(int i=0;i<customEase.Count;i+=4) {
			curve.AddKey(new Keyframe(customEase[i],customEase[i+1],customEase[i+2],customEase[i+3]));
		}
		return curve;
	}
	
	public bool hasCustomEase() {
		if(easeType == 32) return true;
		return false;
	}
	
	public string getEaseString(int codeLanguage)
	{
		string s = "";
		if(hasCustomEase()) {
			if(codeLanguage == 0) {
				s += "\"easecurve\", AMTween.GenerateCurve(new float[]{";
				for(int i=0;i<easeCurve.keys.Length;i++) {
					s += easeCurve.keys[i].time.ToString()+"f, ";
					s += easeCurve.keys[i].value.ToString()+"f, ";
					s += easeCurve.keys[i].inTangent.ToString()+"f, ";
					s += easeCurve.keys[i].outTangent.ToString()+"f";
					if(i < easeCurve.keys.Length-1) s+= ", ";
				}	
				s += "})";				
			} else {
				s += "\"easecurve\": AMTween.GenerateCurve([";
				for(int i=0;i<easeCurve.keys.Length;i++) {
					s += easeCurve.keys[i].time.ToString()+", ";
					s += easeCurve.keys[i].value.ToString()+", ";
					s += easeCurve.keys[i].inTangent.ToString()+", ";
					s += easeCurve.keys[i].outTangent.ToString();
					if(i < easeCurve.keys.Length-1) s+= ", ";
				}
				s += "])";
			}
		} else {
			AMTween.EaseType ease = (AMTween.EaseType)easeType;
			s += "\"easetype\", \""+ease.ToString()+"\"";
		}
		return s;
	}
	
	public void setupJSONActionEase(AnimatorTimeline.JSONAction a) {
		a.easeType = easeType;
		if(hasCustomEase()) a.customEase = customEase.ToArray();
		else a.customEase = new float[]{};
	}
}
