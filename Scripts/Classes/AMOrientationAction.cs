using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMOrientationAction : AMAction {

	public int endFrame;
	public Transform obj;
	public Transform startTarget;
	public Transform endTarget;
	
	public bool isSetStartPosition = false;
	public bool isSetEndPosition = false;
	public Vector3 startPosition;
	public Vector3 endPosition;
	
	public override string ToString (int codeLanguage, int frameRate)
	{
		if(endFrame == -1 || !startTarget) return null;
		string s;
		if(isLookFollow()) {
			if(codeLanguage == 0) {
			// c#
				s = "AMTween.LookFollow (obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"time\", "+getTime(frameRate)+"f, ";
				s += "\"looktarget\", GameObject.Find(\""+startTarget.gameObject.name+"\").transform";
				s += "));";
			} else {
			// js
				s = "AMTween.LookFollow (obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"time\": "+getTime(frameRate)+", ";
				s += "\"looktarget\": GameObject.Find(\""+startTarget.gameObject.name+"\").transform";
				s += "});";
			}
			return s;
		} else {
			if(!endTarget) return null;
			if(codeLanguage == 0) {
			// c#
				s = "AMTween.LookToFollow (obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"time\", "+getTime(frameRate)+"f, ";
				s += "\"looktarget\", GameObject.Find(\""+endTarget.gameObject.name+"\").transform, ";
				if(isSetEndPosition) s += "\"endposition\", new Vector3("+endPosition.x+"f, "+endPosition.y+"f, "+endPosition.z+"f), ";
				s += getEaseString(codeLanguage);
				s += "));";
			} else {
			// js
				s = "AMTween.LookToFollow (obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"time\": "+getTime(frameRate)+", ";
				s += "\"looktarget\": GameObject.Find(\""+endTarget.gameObject.name+"\").transform, ";
				if(isSetEndPosition) s += "\"endposition\", Vector3("+endPosition.x+", "+endPosition.y+", "+endPosition.z+"), ";
				s += getEaseString(codeLanguage);
				s += "});";
			}
			return s;
		}
	}
	
	public override void execute(int frameRate, float delay) {
		if(!obj) return;
		if(endFrame == -1) return;
		// if start and end target are the same, look follow
		if(isLookFollow()) {
			AMTween.LookFollow(obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate, delay),"time",getTime(frameRate),"looktarget",startTarget));
		// look to follow
		} else {
			if(hasCustomEase()) AMTween.LookToFollow(obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate, delay),"time",getTime (frameRate),"looktarget",endTarget,"endposition",(isSetEndPosition ? (Vector3?) endPosition : null),"easecurve",easeCurve));
			else AMTween.LookToFollow(obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate, delay),"time",getTime (frameRate),"looktarget",endTarget,"endposition",(isSetEndPosition ? (Vector3?) endPosition : null),"easetype",(AMTween.EaseType)easeType));
		}		
	}
	
	public override int getNumberOfFrames() {
		return endFrame-startFrame;
	}
	
	public float getTime(int frameRate) {
		return (float)getNumberOfFrames()/(float)frameRate;	
	}
	
	public bool isLookFollow() {
		if(startTarget != endTarget) return false;
		return true;
	}

	public Quaternion getQuaternionAtPercent(float percentage, /*Vector3 startPosition, Vector3 endPosition,*/ Vector3? startVector = null, Vector3? endVector = null) {
		if(isLookFollow()) {
			obj.LookAt(startTarget);
			return obj.rotation;
		}
		
		Vector3 _temp = obj.position;
		if(isSetStartPosition) obj.position = (Vector3) startPosition;
		obj.LookAt(startVector ?? startTarget.position);
		Vector3 eStart = obj.eulerAngles;
		if(isSetEndPosition) obj.position = (Vector3) endPosition;
		obj.LookAt(endVector ?? endTarget.position);
		Vector3 eEnd = obj.eulerAngles;
		obj.position = _temp;
		eEnd=new Vector3(AMTween.clerp(eStart.x,eEnd.x,1),AMTween.clerp(eStart.y,eEnd.y,1),AMTween.clerp(eStart.z,eEnd.z,1));

		Vector3 eCurrent = new Vector3();
		
		AMTween.EasingFunction ease;
		AnimationCurve curve = null;
		if(hasCustomEase()) {
			curve = easeCurve;
			ease = AMTween.customEase;
		} else {
			ease = AMTween.GetEasingFunction((AMTween.EaseType)easeType);
		}
		
		eCurrent.x = ease(eStart.x,eEnd.x,percentage,curve);
		eCurrent.y = ease(eStart.y,eEnd.y,percentage,curve);
		eCurrent.z = ease(eStart.z,eEnd.z,percentage,curve);
		
		
		return Quaternion.Euler(eCurrent);
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(!obj || endFrame == -1) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.go = obj.gameObject.name;
		a.delay = getWaitTime(frameRate,0f);
		a.time = getTime(frameRate);
		if(isLookFollow()) {
			a.method = "lookfollow";
			a.strings = new string[]{startTarget.gameObject.name};

		} else {
			a.method = "looktofollow";	
			a.strings = new string[]{endTarget.gameObject.name};
			if(isSetEndPosition) {
				a.setPath(new Vector3[]{endPosition});
			}
		}
		
		setupJSONActionEase(a);
		
		return a;
	}
}
