using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AMCameraSwitcherAction : AMAction {
	
	public int endFrame;
	public int cameraFadeType;
	public List<float> cameraFadeParameters;
	public Texture2D irisShape;
	public bool still;
	public int startTargetType;	// 0 = camera, 1 = color
	public int endTargetType;	// 0 = camera, 1 = color
	public Camera startCamera;
	public Camera endCamera;
	public Color startColor;
	public Color endColor;
	
	public override string ToString (int codeLanguage, int frameRate)
	{
		if(endFrame == -1 || !hasStartTarget() || targetsAreEqual()) return null;
		string s;
		bool noFade = (cameraFadeType == (int)AMTween.Fade.None);
		if(codeLanguage == 0) {
		// implement to string
		// c#
			s = "AMTween.CameraFade (AMTween.Fade."+Enum.GetName(typeof(AMTween.Fade),cameraFadeType)+", "+((noFade ? false : !still)).ToString().ToLower()+", "+getParametersString(codeLanguage)+", "+
				"AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"time\", "+getTime(frameRate)+"f, ";
			if(!noFade) s += getEaseString(codeLanguage) +", ";
			if(isReversed()) s+= "\"reversed\", true, ";
			if(AMCameraFade.needsTexture(cameraFadeType)) {
				s += "\"texture\", AMTween.LoadTexture2D(\""+irisShape.name+"\"), ";
			}
			if(!noFade) {
				if(startTargetType == 0) if(startCamera) s+= "\"camera1\", GameObject.Find(\""+startCamera.gameObject.name+"\").camera, "; else s+= "\"camera1\", null /* Missing Camera */";
				else s+= "\"color1\", new Color("+startColor.r+"f, "+startColor.g+"f, "+startColor.b+"f, "+startColor.a+"f), ";
			}
			if(endTargetType == 0) if(endCamera) s+= "\"camera2\", GameObject.Find(\""+endCamera.gameObject.name+"\").camera"; else s+= "\"camera2\", null /* Missing Camera */";
			else s+= "\"color2\", new Color("+endColor.r+"f, "+endColor.g+"f, "+endColor.b+"f, "+endColor.a+"f)";
			
			if((!noFade && startTargetType == 0) || endTargetType == 0) s+= ", \"allcameras\", csCameras";
			s += "));";
		} else {
		// js
			s = "AMTween.CameraFade (AMTween.Fade."+Enum.GetName(typeof(AMTween.Fade),cameraFadeType)+", "+((noFade ? false : !still)).ToString().ToLower()+", "+getParametersString(codeLanguage)+", "+
				"{\"delay\": "+getWaitTime(frameRate,0f)+", \"time\": "+getTime(frameRate)+", ";
			if(!noFade) s += getEaseString(codeLanguage) +", ";
			if(isReversed()) s+= "\"reversed\": true, ";
			if(AMCameraFade.needsTexture(cameraFadeType)) {
				s += "\"texture\": AMTween.LoadTexture2D(\""+irisShape.name+"\"), ";
			}
			if(!noFade) {
				if(startTargetType == 0) if(startCamera) s+= "\"camera1\": GameObject.Find(\""+startCamera.gameObject.name+"\").camera, "; else s+= "\"camera1\", null /* Missing Camera */";
				else s+= "\"color1\": Color("+startColor.r+", "+startColor.g+", "+startColor.b+", "+startColor.a+"), ";
			}
			if(endTargetType == 0) if(endCamera) s+= "\"camera2\", GameObject.Find(\""+endCamera.gameObject.name+"\").camera"; else s+= "\"camera2\", null /* Missing Camera */";
			else s+= "\"color2\": Color("+endColor.r+", "+endColor.g+", "+endColor.b+", "+endColor.a+")";
			if((!noFade && startTargetType == 0) || endTargetType == 0) s+= ", \"allcameras\", csCameras";
			s += "));";
		}
		return s;
	}
	
	public void execute(int frameRate, float delay, Camera[] allCameras) {
		// if targets are equal do nothing
		if(endFrame == -1 || !hasTargets() || targetsAreEqual()) return;
		float[] parameters = cameraFadeParameters.ToArray();
		Hashtable hash = new Hashtable();
		hash.Add("time",getTime(frameRate));
		hash.Add("delay",getWaitTime(frameRate, delay));
		if(easeType == 32) hash.Add("easecurve", easeCurve);
		else hash.Add("easetype",(AMTween.EaseType)easeType);
		hash.Add("reversed",AMTween.isTransitionReversed(cameraFadeType, parameters));
		hash.Add("allcameras",allCameras);
		if(startTargetType == 0) hash.Add("camera1",startCamera);
		else hash.Add("color1",startColor);
		if(endTargetType == 0) hash.Add("camera2",endCamera);
		else hash.Add("color2",endColor);
		if(AMCameraFade.needsTexture(cameraFadeType)) hash.Add("texture",irisShape);
		AMTween.CameraFade(cameraFadeType, !still, parameters, hash);
	}
	
	
	
	public string getParametersString(int codeLanguage) {
		string s = "";
		s += (codeLanguage == 0 ? "new float[]{" : "[");
		for(int i=0;i<cameraFadeParameters.Count;i++) {
			s += cameraFadeParameters[i].ToString();
			if(codeLanguage == 0) s+= "f";
			if(i<=cameraFadeParameters.Count-2) s+= ", ";
		}
		s += (codeLanguage == 0 ? "}" : "]");
		return s;
	}
	public override int getNumberOfFrames() {
		return endFrame-startFrame;
	}
	public float getTime(int frameRate) {
		return (float)getNumberOfFrames()/(float)frameRate;	
	}
	public bool hasTargets() {
		if(hasStartTarget() && hasEndTarget()) return true;
		return false;
	}
	public bool hasStartTarget() {
		if(startTargetType == 0 && !startCamera) return false;
		//else if(!startColor) return false;
		return true;
	}
	public bool hasEndTarget() {
		if(endFrame == -1 ||(endTargetType == 0 && !endCamera)) return false;
		//else if(!endColor) return false;
		return true;
	}
	public bool targetsAreEqual() {
		if(startTargetType != endTargetType) return false;
		if(startTargetType == 0 && startCamera != endCamera) return false;
		else if(startTargetType == 1 && startColor != endColor) return false;
		return true;
	}
	public string getStartTargetName() {
		if(startTargetType == 0)
			if(startCamera) return startCamera.gameObject.name;
			else return "None";
		else
			return "Color";
	}
	public string getEndTargetName() {
		if(endTargetType == 0)
			if(endCamera) return endCamera.gameObject.name;
			else return "None";
		else
			return "Color";
	}
	
	public bool isReversed() {
		return AMTween.isTransitionReversed(cameraFadeType,cameraFadeParameters.ToArray());	
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(endFrame == -1 || !hasTargets() || targetsAreEqual()) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.method = "camerafade";
		a.delay = getWaitTime(frameRate,0f);
		a.time = getTime(frameRate);
		setupJSONActionEase(a);
		a.ints = new int[]{cameraFadeType,startTargetType,endTargetType};
		List<string> strings = new List<string>();
		List<AnimatorTimeline.JSONColor> colors = new List<AnimatorTimeline.JSONColor>();
		if(startTargetType == 0) {
			strings.Add(startCamera.gameObject.name);
			colors.Add(null);
		} else {
			AnimatorTimeline.JSONColor c = new AnimatorTimeline.JSONColor();
			c.setValue(startColor);
			colors.Add(c);
			strings.Add(null);
		}
		if(endTargetType == 0) {
			strings.Add(endCamera.gameObject.name);
			colors.Add(null);
		} else {
			AnimatorTimeline.JSONColor c = new AnimatorTimeline.JSONColor();
			c.setValue(endColor);
			colors.Add(c);
			strings.Add(null);
		}
		a.strings = strings.ToArray();
		a.colors = colors.ToArray();
		// reversed, rendertex
		a.bools = new bool[]{isReversed(), !still};
		// textures
		if(AMCameraFade.needsTexture(cameraFadeType)) a.stringsExtra = new string[]{irisShape.name};//hash.Add("texture",irisShape);
		// parameters
		a.floats = cameraFadeParameters.ToArray();
		return a;
	}
}
