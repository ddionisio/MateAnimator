using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMOrientationKey : AMKey {

    public Transform target;

    public int endFrame;
    public Transform obj;
    public Transform endTarget;

    public bool isSetStartPosition = false;
    public bool isSetEndPosition = false;
    public Vector3 startPosition;
    public Vector3 endPosition;

    public bool setTarget(Transform target) {
        if(target != this.target) {
            this.target = target;
            return true;
        }
        return false;
    }

    public override AMKey CreateClone() {

        AMOrientationKey a = gameObject.AddComponent<AMOrientationKey>();
        a.enabled = false;
        a.frame = frame;
        a.target = target;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
        return a;
    }

    #region action
    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        if(!obj) return null;
        if(endFrame == -1) return null;
        if(isLookFollow()) {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("position", new AMPlugLookFollowTarget(target)));
        }
        else {
            if(hasCustomEase()) {
                if(isSetEndPosition)
                    return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("position", new AMPlugLookToFollowTarget(endTarget, endPosition)).Ease(easeCurve));
                else
                    return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("position", new AMPlugLookToFollowTarget(endTarget)).Ease(easeCurve));
            }
            else {
                if(isSetEndPosition)
                    return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("position", new AMPlugLookToFollowTarget(endTarget, endPosition)).Ease((EaseType)easeType));
                else
                    return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("position", new AMPlugLookToFollowTarget(endTarget)).Ease((EaseType)easeType));
            }
        }
    }

    public override int getNumberOfFrames() {
        return endFrame - frame;
    }

    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public bool isLookFollow() {
        if(target != endTarget) return false;
        return true;
    }

    public Quaternion getQuaternionAtPercent(float percentage, /*Vector3 startPosition, Vector3 endPosition,*/ Vector3? startVector = null, Vector3? endVector = null) {
        if(isLookFollow()) {
            obj.LookAt(target);
            return obj.rotation;
        }

        Vector3 _temp = obj.position;
        if(isSetStartPosition) obj.position = (Vector3)startPosition;
        obj.LookAt(startVector ?? target.position);
        Vector3 eStart = obj.eulerAngles;
        if(isSetEndPosition) obj.position = (Vector3)endPosition;
        obj.LookAt(endVector ?? endTarget.position);
        Vector3 eEnd = obj.eulerAngles;
        obj.position = _temp;
        eEnd = new Vector3(AMUtil.clerp(eStart.x, eEnd.x, 1), AMUtil.clerp(eStart.y, eEnd.y, 1), AMUtil.clerp(eStart.z, eEnd.z, 1));

        Vector3 eCurrent = new Vector3();

        if(hasCustomEase()) {
            eCurrent.x = AMUtil.EaseCustom(eStart.x, eEnd.x - eStart.x, percentage, easeCurve);
            eCurrent.y = AMUtil.EaseCustom(eStart.y, eEnd.y - eStart.y, percentage, easeCurve);
            eCurrent.z = AMUtil.EaseCustom(eStart.z, eEnd.z - eStart.z, percentage, easeCurve);
        }
        else {
            TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)easeType);
            eCurrent.x = ease(percentage, eStart.x, eEnd.x - eStart.x, 1.0f, 0.0f, 0.0f);
            eCurrent.y = ease(percentage, eStart.y, eEnd.y - eStart.y, 1.0f, 0.0f, 0.0f);
            eCurrent.z = ease(percentage, eStart.z, eEnd.z - eStart.z, 1.0f, 0.0f, 0.0f);
        }

        return Quaternion.Euler(eCurrent);
    }
    #endregion
}
