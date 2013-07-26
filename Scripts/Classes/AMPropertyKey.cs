using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[AddComponentMenu("")]
public class AMPropertyKey : AMKey {
    //union for single values
    public double val;	// value as double

    //union for vectors, color, rect
    public Vector4 vect4;
    public Vector2 vect2 { get { return new Vector2(vect4.x, vect4.y); } set { vect4.Set(value.x, value.y, 0, 0); } }
    public Vector3 vect3 { get { return new Vector3(vect4.x, vect4.y, vect4.z); } set { vect4.Set(value.x, value.y, value.z, 0); } }
    public Color color { get { return new Color(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.r, value.g, value.b, value.a); } }
    public Rect rect { get { return new Rect(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.xMin, value.yMin, value.width, value.height); } }
    public Quaternion quat { get { return new Quaternion(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.x, value.y, value.z, value.w); } }

    public bool setValue(float val) {
        if(this.val != (double)val) {
            this.val = (double)val;
            return true;
        }
        return false;
    }
    public bool setValue(Quaternion quat) {
        if(this.quat != quat) {
            this.quat = quat;
            return true;
        }
        return false;
    }
    public bool setValue(Vector4 vect4) {
        if(this.vect4 != vect4) {
            this.vect4 = vect4;
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
    public override AMKey CreateClone() {

        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = frame;
        a.val = val;
        a.vect4 = vect4;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);

        return a;
    }
}
