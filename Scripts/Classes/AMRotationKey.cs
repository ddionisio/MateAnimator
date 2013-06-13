using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMRotationKey : AMKey {
	
	//public int type = 0; // 0 = Rotate To, 1 = Look At
	public Quaternion rotation;

	public bool setRotation(Vector3 rotation) {
		if(this.rotation != Quaternion.Euler(rotation)) {
			this.rotation = Quaternion.Euler(rotation);
			return true;	
		}
		return false;
	}
	public Vector3 getRotation() {
		return 	rotation.eulerAngles;
	}
	public bool setRotationQuaternion(Vector4 rotation) {
		Quaternion q = new Quaternion(rotation.x,rotation.y,rotation.z,rotation.w);
		if(this.rotation != q) {
			this.rotation = q;
			return true;
		}
		return false;
	}
	public Vector4 getRotationQuaternion() {
		return new Vector4(rotation.x,rotation.y,rotation.z,rotation.w);	
	}
	/*public bool setType(int type) {
		if(this.type != type) {
			this.type = type;
			return true;
		}
		return false;
	}*/
	// copy properties from key
	public override AMKey CreateClone ()
	{
		
		AMRotationKey a = ScriptableObject.CreateInstance<AMRotationKey>();
		a.frame = frame;
		//a.type = type;
		a.rotation = rotation;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		
		return a;
	}
	
}
