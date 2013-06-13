using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class AMPropertyKey : AMKey {
	public double val;	// value as double
	public Vector2 vect2;
	public Vector3 vect3;
	public Color color;
	public Rect rect;
	public List<float> morph;
	
	public bool setValueMegaMorph(List<float> morph) {
		bool shouldUpdate = false;
		if(this.morph == null || this.morph.Count != morph.Count) shouldUpdate = true;
		else {
			for(int i=0;i<morph.Count;i++) {
				if(this.morph[i] != morph[i]) {
					shouldUpdate = true;
					break;
				}
			}
		}
		if(shouldUpdate) {
			this.morph = new List<float>(morph);	
			return true;
		}
		return false;
	}
	
	public bool setValue(float val) {
		if(this.val != (double)val) {
			this.val = (double)val;	
			return true;
		}
		return false;
	}
	public bool setValue(Vector3 vect3) {
		if(this.vect3 != vect3) {
			this.vect3 = vect3;	
			return true;
		}
		return false;
	}
	public bool setValue(Color color) {
		if(this.color != color) {
			this.color = color;	
			return true;
		}
		return false;
	}
	public bool setValue(Rect rect) {
		if(this.rect != rect) {
			this.rect = rect;	
			return true;
		}
		return false;
	}
	public bool setValue(Vector2 vect2) {
		if(this.vect2 != vect2) {
			this.vect2 = vect2;	
			return true;
		}
		return false;
	}
	// set value from double
	public bool setValue(double val) {
		if(this.val != val) {
			this.val = val;	
			return true;
		}
		return false;
	}
	// set value from int
	public bool setValue(int val) {
		if(this.val != (double)val) {
			this.val = (double)val;	
			return true;
		}
		return false;
	}
	// set value from long
	public bool setValue(long val) {
		if(this.val != (double)val) {
			this.val = (double)val;	
			return true;
		}
		return false;
	}

	// copy properties from key
	public override AMKey CreateClone ()
	{
		
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = frame;
		a.val = val;
		a.vect2 = vect2;
		a.vect3 = vect3;
		a.color = color;
		a.rect = rect;
		if(morph != null) a.morph = new List<float>(morph);
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		
		return a;
	}
}
