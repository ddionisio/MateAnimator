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
            if(_obj != value) {
                if(value != null && keys.Count <= 0) cachedInitialRotation = _isLocal ? value.localRotation : value.rotation;
                _obj = value;

                updateCache();
            }

        }
    }

    public override UnityEngine.Object target {
        get { return _obj; }
    }

    public override int version { get { return 2; } }

    [SerializeField]
    private bool _isLocal;
    public bool isLocal {
        get { return _isLocal; }
        set {
            if(_isLocal != value) {
                if(value) {
                    if(_obj != null && keys.Count <= 0) cachedInitialRotation = _obj.rotation;
                }
                else {
                    if(_obj != null && keys.Count <= 0) cachedInitialRotation = _obj.localRotation;
                }

                if(_obj != null && _obj.parent != null) {
                    Transform t = _obj.parent;

                    foreach(AMRotationKey key in keys) {
                        if(key.isLocal && !value) {//to world
                            key.rotation = t.rotation * key.rotation;
                            key.endRotation = t.rotation * key.endRotation;
                        }
                        else if(!key.isLocal && value) {//to local
                            Quaternion invQ = Quaternion.Inverse(t.rotation);
                            key.rotation = key.rotation * invQ;
                            key.endRotation = key.endRotation * invQ;
                        }

                        key.isLocal = value;

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
    public AMKey addKey(int _frame, Quaternion _rotation, OnKey addCallback) {
        foreach(AMRotationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                if(addCallback != null)
                    addCallback(this, null);

                key.rotation = _rotation;
                // update cache
                updateCache();
                return null;
            }
        }
        AMRotationKey a = gameObject.AddComponent<AMRotationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.rotation = _rotation;
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;

        if(addCallback != null)
            addCallback(this, a);

        // add a new key
        keys.Add(a);
        // update cache
        updateCache();

        return a;
    }

    // update cache (optimized)
    public override void updateCache() {
		base.updateCache();

        for(int i = 0; i < keys.Count; i++) {
            AMRotationKey key = keys[i] as AMRotationKey;

            isLocal = true;

            key.version = version;

            //a.type = (keys[i] as AMRotationKey).type;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else {
				if(i > 0 && keys[i-1].easeType == AMKey.EaseTypeNone)
					key.easeType = AMKey.EaseTypeNone;

				key.endFrame = -1;
			}
            key.isLocal = _isLocal;
            // quaternions
            if(key.endFrame != -1) key.endRotation = (keys[i + 1] as AMRotationKey).rotation;

        }
    }
    // preview a frame in the scene view
    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        if(!_obj) return;
        if(keys == null || keys.Count <= 0) return;
        // if before or equal to first frame, or is the only frame
        if((frame <= (float)keys[0].frame) || ((keys[0] as AMRotationKey).endFrame == -1)) {
            rotation = (keys[0] as AMRotationKey).getStartQuaternion();
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(keys[keys.Count - 2] as AMRotationKey).endFrame) {
            rotation = (keys[keys.Count - 2] as AMRotationKey).getEndQuaternion();
            return;
        }
        // if lies on rotation action
        foreach(AMRotationKey key in keys) {
            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            // if on startFrame or is no ease
            if(frame == (float)key.frame || (key.easeType == AMKey.EaseTypeNone && frame < (float)key.endFrame)) {
                rotation = key.getStartQuaternion();
                return;
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
                rotation = key.getEndQuaternion();
                return;
            }
            // else find Quaternion using easing function

            float framePositionInAction = frame - (float)key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            Quaternion qStart = key.getStartQuaternion();
            Quaternion qEnd = key.getEndQuaternion();

            if(key.hasCustomEase()) {
                rotation = Quaternion.Slerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(), key.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                rotation = Quaternion.Slerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(), key.amplitude, key.period));
            }

            return;
        }
    }
    // returns true if autoKey successful, sets output key
    public bool autoKey(Transform aObj, int frame, OnKey addCallback) {
        if(!_obj || _obj != aObj) { return false; }

        if(keys.Count <= 0) {
            if(rotation != cachedInitialRotation) {
                // if updated position, addkey
                addKey(frame, rotation, addCallback);
                return true;
            }

            return false;
        }
        Quaternion oldRot = getRotationAtFrame((float)frame);
        if(rotation != oldRot) {
            // if updated position, addkey
            addKey(frame, rotation, addCallback);
            return true;
        }

        return false;
    }
    public Quaternion getRotationAtFrame(float frame) {
        // if before or equal to first frame, or is the only frame
        if((frame <= (float)keys[0].frame) || ((keys[0] as AMRotationKey).endFrame == -1)) {
            //rotation = (cache[0] as AMRotationAction).getStartQuaternion();
            return (keys[0] as AMRotationKey).getStartQuaternion();
        }
        // if beyond or equal to last frame
        if(frame >= (float)(keys[keys.Count - 2] as AMRotationKey).endFrame) {
            //rotation = (cache[cache.Count-2] as AMRotationAction).getEndQuaternion();
            return (keys[keys.Count - 2] as AMRotationKey).getEndQuaternion();
        }
        // if lies on rotation action
        foreach(AMRotationKey key in keys) {
            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            // if on startFrame or no ease
			if(frame == (float)key.frame || (key.easeType == AMKey.EaseTypeNone && frame < (float)key.endFrame)) {
                return key.getStartQuaternion();
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
                return key.getEndQuaternion();
            }
            // else find Quaternion using easing function

            Quaternion qStart = key.getStartQuaternion();
            Quaternion qEnd = key.getEndQuaternion();

            float framePositionInAction = frame - (float)key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            if(key.hasCustomEase()) {
                return Quaternion.Slerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(), key.easeCurve));
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                return Quaternion.Slerp(qStart, qEnd, ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(), 0.0f, 0.0f));
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
	public bool isObjectEqual(Transform t) {
		return _obj == t;
	}
}
