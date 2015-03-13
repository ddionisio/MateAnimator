using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween.Core;
using Holoville.HOTween;

[AddComponentMenu("")]
public class AMRotationEulerTrack : AMTrack {
    [SerializeField]
    private Transform _obj;

    protected override void SetSerializeObject(UnityEngine.Object obj) {
        _obj = obj as Transform;
    }

    protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
        return targetGO ? targetGO.transform : _obj;
    }

    public new void SetTarget(AMITarget target, Transform item) {
        base.SetTarget(target, item);
        if(item != null && keys.Count <= 0) cachedInitialRotation = item.localEulerAngles;
    }

    public override int version { get { return 1; } }

    Vector3 cachedInitialRotation;

    public override string getTrackType() {
        return "Rotation Euler";
    }
    // add a new key
    public void addKey(AMITarget target, OnAddKey addCall, int _frame, Vector3 _rotation) {
        foreach(AMRotationEulerKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.rotation = _rotation;
                // update cache
                updateCache(target);
                return;
            }
        }
        AMRotationEulerKey a = addCall(gameObject, typeof(AMRotationEulerKey)) as AMRotationEulerKey;
        a.frame = _frame;
        a.rotation = _rotation;
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;

        // add a new key
        keys.Add(a);
        // update cache
        updateCache(target);
    }

    // update cache (optimized)
    public override void updateCache(AMITarget target) {
        base.updateCache(target);

        for(int i = 0; i < keys.Count; i++) {
            AMRotationEulerKey key = keys[i] as AMRotationEulerKey;

            key.version = version;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else {
                if(i > 0 && !keys[i-1].canTween)
                    key.interp = (int)AMKey.Interpolation.None;

                key.endFrame = -1;
            }
        }
    }
    // preview a frame in the scene view
    public override void previewFrame(AMITarget target, float frame, int frameRate, AMTrack extraTrack = null) {
        Transform t = GetTarget(target) as Transform;

        if(!t) return;
        if(keys == null || keys.Count <= 0) return;
        // if before or equal to first frame, or is the only frame
        if((frame <= (float)keys[0].frame) || ((keys[0] as AMRotationEulerKey).endFrame == -1)) {
            t.localEulerAngles = (keys[0] as AMRotationEulerKey).rotation;
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(keys[keys.Count - 2] as AMRotationEulerKey).endFrame) {
            t.localEulerAngles = (keys[keys.Count - 1] as AMRotationEulerKey).rotation;
            return;
        }
        // if lies on rotation action
        for(int i = 0; i < keys.Count; i++) {
            AMRotationEulerKey key = keys[i] as AMRotationEulerKey;
            AMRotationEulerKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMRotationEulerKey : null;

            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            // if on startFrame or is no ease
            if(frame == (float)key.frame || (!key.canTween && frame < (float)key.endFrame)) {
                t.localEulerAngles = key.rotation;
                return;
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
                t.localEulerAngles = keyNext.rotation;
                return;
            }
            // else find Quaternion using easing function

            float framePositionInAction = frame - (float)key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            Vector3 qStart = key.rotation;
            Vector3 qEnd = keyNext.rotation;

            if(key.hasCustomEase()) {
                t.localEulerAngles = Vector3.Lerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(frameRate), key.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                t.localEulerAngles = Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), key.amplitude, key.period));
            }

            return;
        }
    }
    // returns true if autoKey successful, sets output key
    public bool autoKey(AMITarget itarget, OnAddKey addCall, Transform aObj, int frame, int frameRate) {
        Transform t = GetTarget(itarget) as Transform;
        if(!t || t != aObj) { return false; }
        Vector3 r = t.localEulerAngles;
        if(keys.Count <= 0) {
            if(r != cachedInitialRotation) {
                // if updated position, addkey
                addKey(itarget, addCall, frame, r);
                return true;
            }

            return false;
        }
        Vector3 oldRot = getRotationAtFrame(frame, frameRate);
        if(r != oldRot) {
            // if updated position, addkey
            addKey(itarget, addCall, frame, r);
            return true;
        }

        return false;
    }
    public Vector3 getRotationAtFrame(int frame, int frameRate) {
        // if before or equal to first frame, or is the only frame
        if((frame <= keys[0].frame) || ((keys[0] as AMRotationEulerKey).endFrame == -1)) {
            //rotation = (cache[0] as AMRotationAction).getStartQuaternion();
            return (keys[0] as AMRotationEulerKey).rotation;
        }
        // if beyond or equal to last frame
        if(frame >= (keys[keys.Count - 2] as AMRotationEulerKey).endFrame) {
            //rotation = (cache[cache.Count-2] as AMRotationAction).getEndQuaternion();
            return (keys[keys.Count - 1] as AMRotationEulerKey).rotation;
        }
        // if lies on rotation action
        for(int i = 0; i < keys.Count; i++) {
            AMRotationEulerKey key = keys[i] as AMRotationEulerKey;
            AMRotationEulerKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMRotationEulerKey : null;

            if((frame < key.frame) || (frame > key.endFrame)) continue;
            // if on startFrame or no ease
            if(frame == key.frame || (!key.canTween && frame < key.endFrame)) {
                return key.rotation;
            }
            // if on endFrame
            if(frame == key.endFrame) {
                return keyNext.rotation;
            }
            // else find Quaternion using easing function

            Vector3 qStart = key.rotation;
            Vector3 qEnd = keyNext.rotation;

            int framePositionInAction = frame - key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0;

            if(key.hasCustomEase()) {
                return Vector3.Lerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, (float)framePositionInAction / (float)key.getNumberOfFrames(frameRate), key.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                return Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), 0.0f, 0.0f));
            }
        }
        Debug.LogError("Animator: Could not get rotation at frame '" + frame + "'");
        return Vector3.zero;
    }
    public Vector3 getInitialRotation() {
        return (keys[0] as AMRotationEulerKey).rotation;
    }

    public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        if(!_obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "rotation";
        init.go = _obj.gameObject.name;
        AnimatorTimeline.JSONQuaternion q = new AnimatorTimeline.JSONQuaternion();
        q.setValue(getInitialRotation());
        init.rotation = q;
        return init;
    }

    public override List<GameObject> getDependencies(AMITarget itarget) {
        Transform t = GetTarget(itarget) as Transform;
        List<GameObject> ls = new List<GameObject>();
        if(t) ls.Add(t.gameObject);
        return ls;
    }
    public override List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
        Transform t = GetTarget(itarget) as Transform;
        if(!t) return new List<GameObject>();
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == t.gameObject) {
                SetTarget(itarget, newReferences[i].transform);
                break;
            }
        }
        return new List<GameObject>();
    }

    protected override void DoCopy(AMTrack track) {
        AMRotationEulerTrack ntrack = track as AMRotationEulerTrack;
        ntrack._obj = _obj;
        ntrack.cachedInitialRotation = cachedInitialRotation;
    }
}
