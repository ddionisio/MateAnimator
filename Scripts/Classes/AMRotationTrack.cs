using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween.Core;
using Holoville.HOTween;

[AddComponentMenu("")]
public class AMRotationTrack : AMTrack {

    [SerializeField]
    private Transform _obj;
    public Transform obj {
        set {
            if(value != null && cache.Count <= 0) cachedInitialRotation = _isLocal ? value.localRotation : value.rotation;
            _obj = value;

        }
    }

    public override UnityEngine.Object genericObj {
        get { return _obj; }
    }

    [SerializeField]
    private bool _isLocal;
    public bool isLocal {
        get { return _isLocal; }
        set {
            if(_isLocal != value) {
                if(value) {
                    if(_obj != null && cache.Count <= 0) cachedInitialRotation = _obj.rotation;
                }
                else {
                    if(_obj != null && cache.Count <= 0) cachedInitialRotation = _obj.localRotation;
                }

                if(_obj != null && _obj.parent != null) {
                    Transform t = _obj.parent;

                    foreach(AMRotationAction action in cache) {
                        if(action.isLocal && !value) {//to world
                            action.startRotation = t.rotation * action.startRotation;
                            action.endRotation = t.rotation * action.endRotation;
                        }
                        else if(!action.isLocal && value) {//to local
                            Quaternion invQ = Quaternion.Inverse(t.rotation);
                            action.startRotation = action.startRotation * invQ;
                            action.endRotation = action.endRotation * invQ;
                        }

                        action.isLocal = value;
                    }

                    foreach(AMRotationKey key in keys) {
                        if(_isLocal && !value) //to world
                            key.rotation = key.rotation * t.rotation;
                        else if(!_isLocal && value) //to local
                            key.rotation = key.rotation * Quaternion.Inverse(t.rotation);
                    }
                }

                _isLocal = value;
            }
        }
    }

    public Quaternion rotation {
        get { return _isLocal ? _obj.localRotation : _obj.rotation; }
        set {
            if(_isLocal)
                _obj.localRotation = value;
            else
                _obj.rotation = value;
        }
    }

    public Quaternion cachedInitialRotation;

    public override string getTrackType() {
        return _isLocal ? "Local Rotation" : "Rotation";
    }
    // add a new key
    public void addKey(int _frame, Quaternion _rotation) {
        foreach(AMRotationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.rotation = _rotation;
                // update cache
                updateCache();
                return;
            }
        }
        AMRotationKey a = gameObject.AddComponent<AMRotationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.rotation = _rotation;
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
    }

    // update cache (optimized)
    public override void updateCache() {

        // sort keys
        sortKeys();
        destroyCache();
        cache = new List<AMAction>();
        for(int i = 0; i < keys.Count; i++) {

            // or create new action and add it to cache list
            AMRotationAction a = gameObject.AddComponent<AMRotationAction>();
            a.enabled = false;
            //a.type = (keys[i] as AMRotationKey).type;
            a.startFrame = keys[i].frame;
            if(keys.Count > (i + 1)) a.endFrame = keys[i + 1].frame;
            else a.endFrame = -1;
            a.obj = _obj;
            a.isLocal = _isLocal;
            // quaternions
            a.startRotation = (keys[i] as AMRotationKey).rotation;
            if(a.endFrame != -1) a.endRotation = (keys[i + 1] as AMRotationKey).rotation;

            a.easeType = (keys[i] as AMRotationKey).easeType;
            a.customEase = new List<float>(keys[i].customEase);
            // add to cache
            cache.Add(a);
        }
        base.updateCache();

    }
    // preview a frame in the scene view
    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        if(!_obj) return;
        if(cache == null || cache.Count <= 0) return;
        if(cache[0] == null) updateCache();
        // if before or equal to first frame, or is the only frame
        if((frame <= (float)cache[0].startFrame) || ((cache[0] as AMRotationAction).endFrame == -1)) {
            rotation = (cache[0] as AMRotationAction).getStartQuaternion();
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(cache[cache.Count - 2] as AMRotationAction).endFrame) {
            rotation = (cache[cache.Count - 2] as AMRotationAction).getEndQuaternion();
            return;
        }
        // if lies on rotation action
        foreach(AMRotationAction action in cache) {
            if((frame < (float)action.startFrame) || (frame > (float)action.endFrame)) continue;
            // if on startFrame
            if(frame == (float)action.startFrame) {
                rotation = action.getStartQuaternion();
                return;
            }
            // if on endFrame
            if(frame == (float)action.endFrame) {
                rotation = action.getEndQuaternion();
                return;
            }
            // else find Quaternion using easing function

            float framePositionInAction = frame - (float)action.startFrame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            Quaternion qStart = action.getStartQuaternion();
            Quaternion qEnd = action.getEndQuaternion();

            if(action.hasCustomEase()) {
                rotation = Quaternion.Slerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / action.getNumberOfFrames(), action.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)action.easeType);
                rotation = Quaternion.Slerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, action.getNumberOfFrames(), 0.0f, 0.0f));
            }

            return;
        }
    }
    // returns true if autoKey successful
    public bool autoKey(Transform aObj, int frame) {
        if(!_obj) return false;
        if(_obj != aObj) return false;

        if(cache.Count <= 0) {
            if(rotation != cachedInitialRotation) {
                // if updated position, addkey
                addKey(frame, rotation);
                return true;
            }
            return false;
        }
        Quaternion oldRot = getRotationAtFrame((float)frame);
        if(rotation != oldRot) {
            // if updated position, addkey
            addKey(frame, rotation);
            return true;
        }
        return false;
    }
    public Quaternion getRotationAtFrame(float frame) {
        // if before or equal to first frame, or is the only frame
        if((frame <= (float)cache[0].startFrame) || ((cache[0] as AMRotationAction).endFrame == -1)) {
            //rotation = (cache[0] as AMRotationAction).getStartQuaternion();
            return (cache[0] as AMRotationAction).getStartQuaternion();
        }
        // if beyond or equal to last frame
        if(frame >= (float)(cache[cache.Count - 2] as AMRotationAction).endFrame) {
            //rotation = (cache[cache.Count-2] as AMRotationAction).getEndQuaternion();
            return (cache[cache.Count - 2] as AMRotationAction).getEndQuaternion();
        }
        // if lies on rotation action
        foreach(AMRotationAction action in cache) {
            if((frame < (float)action.startFrame) || (frame > (float)action.endFrame)) continue;
            // if on startFrame
            if(frame == (float)action.startFrame) {
                return action.getStartQuaternion();
            }
            // if on endFrame
            if(frame == (float)action.endFrame) {
                return action.getEndQuaternion();
            }
            // else find Quaternion using easing function

            Quaternion qStart = action.getStartQuaternion();
            Quaternion qEnd = action.getEndQuaternion();

            float framePositionInAction = frame - (float)action.startFrame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            if(action.hasCustomEase()) {
                return Quaternion.Slerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / action.getNumberOfFrames(), action.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)action.easeType);
                return Quaternion.Slerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, action.getNumberOfFrames(), 0.0f, 0.0f));
            }
        }
        Debug.LogError("Animator: Could not get " + _obj.name + " rotation at frame '" + frame + "'");
        return Quaternion.identity;
    }
    public Vector4 getInitialRotation() {
        return (keys[0] as AMRotationKey).getRotationQuaternion();
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        if(!_obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "rotation";
        init.go = _obj.gameObject.name;
        AnimatorTimeline.JSONQuaternion q = new AnimatorTimeline.JSONQuaternion();
        q.setValue(getInitialRotation());
        init.rotation = q;
        return init;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(_obj) ls.Add(_obj.gameObject);
        return ls;
    }
    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        if(!_obj) return new List<GameObject>();
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == _obj.gameObject) {
                obj = newReferences[i].transform;
                break;
            }
        }
        return new List<GameObject>();
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMRotationTrack ntrack = newTake.gameObject.AddComponent<AMRotationTrack>();
        ntrack.enabled = false;

        ntrack._obj = _obj;
        ntrack._isLocal = _isLocal;
        ntrack.cachedInitialRotation = cachedInitialRotation;

        return ntrack;
    }
}
