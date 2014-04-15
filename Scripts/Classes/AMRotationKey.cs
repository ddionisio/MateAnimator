using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMRotationKey : AMKey {

    //public int type = 0; // 0 = Rotate To, 1 = Look At
    public Quaternion rotation;

    public int endFrame;
    public bool isLocal;
    public Quaternion endRotation;

    public bool setRotation(Vector3 rotation) {
        if(this.rotation != Quaternion.Euler(rotation)) {
            this.rotation = Quaternion.Euler(rotation);
            return true;
        }
        return false;
    }
    public Vector3 getRotation() {
        return rotation.eulerAngles;
    }
    public bool setRotationQuaternion(Vector4 rotation) {
        Quaternion q = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        if(this.rotation != q) {
            this.rotation = q;
            return true;
        }
        return false;
    }
    public Vector4 getRotationQuaternion() {
        return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
    }
    /*public bool setType(int type) {
        if(this.type != type) {
            this.type = type;
            return true;
        }
        return false;
    }*/
    // copy properties from key
    public override AMKey CreateClone(GameObject go) {

		AMRotationKey a = go ? go.AddComponent<AMRotationKey>() : gameObject.AddComponent<AMRotationKey>();
        a.enabled = false;
        a.frame = frame;
        //a.type = type;
        a.rotation = rotation;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);

        return a;
    }

    #region action
    public override int getNumberOfFrames() {
        return endFrame - frame;
    }
    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }
    public override Tweener buildTweener(AMITarget target, Sequence sequence, UnityEngine.Object obj, int frameRate) {
        if(!obj) return null;
		if(easeType == EaseTypeNone) {
			return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugNoTween(rotation)));
		}
		else if(endFrame == -1) return null;
        else if(hasCustomEase()) {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugQuaternionSlerp(endRotation)).Ease(easeCurve));
        }
        else {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugQuaternionSlerp(endRotation)).Ease((EaseType)easeType, amplitude, period));
        }
    }

    public Quaternion getStartQuaternion() {
        return rotation;
    }
    public Quaternion getEndQuaternion() {
        return endRotation;
    }
    #endregion

}
