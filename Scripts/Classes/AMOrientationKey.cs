using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMOrientationKey : AMKey {

	public Transform target;
	
	public bool setTarget(Transform target) {
		if(target != this.target) {
			this.target = target;
			return true;	
		}
		return false;
	}
	
	public override AMKey CreateClone ()
	{
		
		AMOrientationKey a = ScriptableObject.CreateInstance<AMOrientationKey>();
		a.frame = frame;
		a.target = target;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		return a;
	}
}
