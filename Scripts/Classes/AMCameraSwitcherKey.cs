using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMCameraSwitcherKey : AMKey {
	
	public int type = 0;		// 0 = camera, 1 = color
	public Camera camera;
	public Color color;
	
	public int cameraFadeType = (int)AMTween.Fade.CrossFade;
	public List<float> cameraFadeParameters = new List<float>();
	public Texture2D irisShape;
	public bool still = false;	// is it still or does it use render texture
	
	public bool setCamera(Camera camera) {
		if(camera != this.camera) {
			this.camera = camera;
			return true;	
		}
		return false;
	}
	
	public bool setColor(Color color) {
		if(color != this.color) {
			this.color = color;
			return true;	
		}
		return false;
	}
	
	public bool setType(int type) {
		if(type != this.type) {
			this.type = type;
			return true;	
		}
		return false;
	}
	
	public bool setStill(bool still) {
		if(still != this.still) {
			this.still = still;
			return true;	
		}
		return false;
	}
	
	public bool setCameraFadeType(int cameraFadeType) {
		if(cameraFadeType != this.cameraFadeType) {
			this.cameraFadeType = cameraFadeType;
			return true;	
		}
		return false;
	}
	
	public override AMKey CreateClone ()
	{
		
		AMCameraSwitcherKey a = ScriptableObject.CreateInstance<AMCameraSwitcherKey>();
		a.frame = frame;
		a.type = type;
		a.camera = camera;
		a.color = color;
		a.cameraFadeType = cameraFadeType;
		a.cameraFadeParameters = new List<float>(cameraFadeParameters);
		a.irisShape = irisShape;
		a.still = still;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		return a;
	}
}
