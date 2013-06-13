using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMTranslationAction : AMAction {

	public int endFrame;
	public Transform obj;
	public Vector3[] path;
	
	public override string ToString (int codeLanguage, int frameRate)
	{
		if(path.Length<=1) return null;
		if(getNumberOfFrames()<=0) return null;
		string s;
		
		if(codeLanguage == 0) {
			// c#
			s = "AMTween.MoveTo (obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"time\", "+getTime(frameRate)+"f, ";
			// if line
			if(path.Length == 2) {
				s += "\"position\", ";
				s += "new Vector3("+path[1].x+"f, "+path[1].y+"f, "+path[1].z+"f), ";
				
			} else {
				// if curve
				s += "\"path\", new Vector3[]{";
				for(int i=0;i<path.Length;i++) {
					s += "new Vector3("+path[i].x+"f, "+path[i].y+"f, "+path[i].z+"f)";
					if(i<path.Length-1) s+= ", ";
				}
				s+= "}, ";
			}
			s += getEaseString(codeLanguage)+"));";
		} else {
			// js
			s = "AMTween.MoveTo (obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"time\": "+getTime(frameRate)+", ";
			// if line
			if(path.Length == 2) {
				s += "\"position\": ";
				s += " Vector3("+path[1].x+", "+path[1].y+", "+path[1].z+"), ";
				
			} else {
				// if curve
				s += "\"path\": [";
				for(int i=0;i<path.Length;i++) {
					s += " Vector3("+path[i].x+", "+path[i].y+", "+path[i].z+")";
					if(i<path.Length-1) s+= ", ";
				}
				s+= "], ";
			}
			s += getEaseString(codeLanguage)+"});";
		}
		return s;	
	}
	public override int getNumberOfFrames() {
		return endFrame-startFrame;
	}
	
	public float getTime(int frameRate) {
		return (float)getNumberOfFrames()/(float)frameRate;	
	}
	
	public override void execute(int frameRate, float delay) {
		if(!obj) return;
		if(path.Length<=1) return;
		if(getNumberOfFrames()<=0) return;
		// if line
		if(path.Length == 2) {
			if(hasCustomEase()) AMTween.MoveTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate,delay),"time",getTime(frameRate),"position",path[1],"easecurve",easeCurve));
			else AMTween.MoveTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate,delay),"time",getTime(frameRate),"position",path[1],"easetype",(AMTween.EaseType)easeType));
			return;	
		}
		// if curve
		if(hasCustomEase()) AMTween.MoveTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate,delay),"time",getTime(frameRate),"path",path,"easecurve",easeCurve));
		else AMTween.MoveTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate,delay),"time",getTime(frameRate),"path",path,"easetype",(AMTween.EaseType)easeType));
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(!obj) return null;
		if(path.Length <=1) return null;
		if(getNumberOfFrames()<=0) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.go = obj.gameObject.name;
		a.method = "moveto";
		a.delay = getWaitTime(frameRate,0f);
		a.time = getTime(frameRate);
		setupJSONActionEase(a);
		// if line
		if(path.Length == 2) {
			a.setPath(new Vector3[]{path[1]});
		// if path
		} else {
			a.setPath(path);
		}
		return a;
	}
	
}
