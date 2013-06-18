using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween.Core;
using Holoville.HOTween;

[AddComponentMenu("")]
public class AMTranslationTrack : AMTrack {
    [SerializeField]
    private Transform _obj;
    public Transform obj {
        set {
            if(value != null && cache.Count <= 0) cachedInitialPosition = _isLocal ? value.localPosition : value.position;
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
                    if(_obj != null && cache.Count <= 0) cachedInitialPosition = _obj.position;
                }
                else {
                    if(_obj != null && cache.Count <= 0) cachedInitialPosition = _obj.localPosition;
                }

                if(_obj != null && _obj.parent != null) {
                    Transform t = _obj.parent;

                    foreach(AMTranslationAction action in cache) {
                        for(int i = 0; i < action.path.Length; i++) {
                            if(action.isLocal && !value) //to world
                                action.path[i] = t.localToWorldMatrix.MultiplyPoint(action.path[i]);
                            else if(!action.isLocal && value) //to local
                                action.path[i] = t.InverseTransformPoint(action.path[i]);
                        }

                        action.isLocal = value;
                    }

                    foreach(AMTranslationKey key in keys) {
                        if(_isLocal && !value) //to world
                            key.position = t.localToWorldMatrix.MultiplyPoint(key.position);
                        else if(!_isLocal && value) //to local
                            key.position = t.InverseTransformPoint(key.position);
                    }
                }

                _isLocal = value;
            }
        }
    }

    public Vector3 position {
        get { return _isLocal ? _obj.localPosition : _obj.position; }
        set {
            if(_isLocal)
                _obj.localPosition = value;
            else
                _obj.position = value;
        }
    }

    public Vector3 cachedInitialPosition;

    public override string getTrackType() {
        return _isLocal ? "Local Translation" : "Translation";
    }

    public bool isObjectEqual(Transform t) {
        return _obj == t;
    }

    // add a new key
    public void addKey(int _frame, Vector3 _position, int _interp, int _easeType) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.position = _position;
                key.interp = _interp;
                key.easeType = _easeType;
                // update cache
                updateCache();
                return;
            }
        }
        AMTranslationKey a = gameObject.AddComponent<AMTranslationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.position = _position;
        a.interp = _interp;
        a.easeType = _easeType;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
    }
    // add a new key, default interpolation and easeType
    public void addKey(int _frame, Vector3 _position) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.position = _position;
                // update cache
                updateCache();
                return;
            }
        }
        AMTranslationKey a = gameObject.AddComponent<AMTranslationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.position = _position;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
    }

    // preview a frame in the scene view
    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        if(!_obj) return;
        if(cache.Count <= 0) return;
        // if before first frame
        if(frame <= (float)cache[0].startFrame) {
            position = (cache[0] as AMTranslationAction).path[0];
            return;
        }
        // if beyond last frame
        if(frame >= (float)(cache[cache.Count - 1] as AMTranslationAction).endFrame) {
            position = (cache[cache.Count - 1] as AMTranslationAction).path[(cache[cache.Count - 1] as AMTranslationAction).path.Length - 1];
            return;
        }
        // if lies on curve
        foreach(AMTranslationAction action in cache) {
            if(((int)frame < action.startFrame) || ((int)frame > action.endFrame)) continue;
            if(action.path.Length == 1) {
                position = action.path[0];
                return;
            }
            float _value;
            float framePositionInPath = frame - (float)action.startFrame;
            if(framePositionInPath < 0f) framePositionInPath = 0f;

            if(action.hasCustomEase()) {
                _value = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInPath / action.getNumberOfFrames(), action.easeCurve);
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)action.easeType);
                _value = ease(framePositionInPath, 0.0f, 1.0f, action.getNumberOfFrames(), 0.0f, 0.0f);
                if(float.IsNaN(_value)) { //this really shouldn't happen...
                    return;
                }
            }

            AMUtil.PutOnPath(_obj, action.path, Mathf.Clamp(_value, 0f, 1f), _isLocal);
            return;
        }

    }
    // returns true if autoKey successful
    public bool autoKey(Transform aobj, int frame) {
        if(!_obj) return false;
        if(aobj != _obj) return false;

        if(cache.Count <= 0) {
            if(position != cachedInitialPosition) {
                // if updated position, addkey
                addKey(frame, position);
                return true;
            }
            return false;
        }
        Vector3 oldPos = getPositionAtFrame((float)frame, false);
        if(position != oldPos) {
            // if updated position, addkey
            addKey(frame, position);
            return true;
        }
        return false;
    }
    public Vector3 getPositionAtFrame(float frame, bool forceWorld) {
        Vector3 ret = Vector3.zero;

        if(cache.Count <= 0) ret = position;
        // if before first frame
        else if(frame <= (float)cache[0].startFrame) {
            ret = (cache[0] as AMTranslationAction).path[0];
        }
        // if beyond last frame
        else if(frame >= (float)(cache[cache.Count - 1] as AMTranslationAction).endFrame) {
            ret = (cache[cache.Count - 1] as AMTranslationAction).path[(cache[cache.Count - 1] as AMTranslationAction).path.Length - 1];
        }
        else {
            bool retFound = false;
            // if lies on curve
            foreach(AMTranslationAction action in cache) {
                if(((int)frame < action.startFrame) || ((int)frame > action.endFrame)) continue;
                if(action.path.Length == 1) {
                    ret = action.path[0];
                    retFound = true;
                    break;
                }

                float framePositionInPath = frame - (float)action.startFrame;
                if(framePositionInPath < 0f) framePositionInPath = 0f;

                // ease
                if(action.hasCustomEase()) {
                    ret = AMUtil.PointOnPath(action.path, Mathf.Clamp(AMUtil.EaseCustom(0.0f, 1.0f, framePositionInPath / action.getNumberOfFrames(), action.easeCurve), 0.0f, 1.0f));
                    retFound = true;
                    break;
                }
                else {
                    TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)action.easeType);
                    ret = AMUtil.PointOnPath(action.path, Mathf.Clamp(ease(framePositionInPath, 0.0f, 1.0f, action.getNumberOfFrames(), 0.0f, 0.0f), 0.0f, 1.0f));
                    retFound = true;
                    break;
                }
            }

            if(!retFound)
                Debug.LogError("Animator: Could not get " + _obj.name + " position at frame '" + frame + "'");
        }

        if(forceWorld && _isLocal && _obj != null && _obj.parent != null)
            ret = _obj.parent.localToWorldMatrix.MultiplyPoint(ret);

        return ret;
    }
    // draw gizmos
    public override void drawGizmos(float gizmo_size) {
        foreach(AMTranslationAction action in cache) {
            if(action.path.Length > 1) {
                if(_isLocal && _obj != null && _obj.parent != null) {
                    AMGizmo.DrawPathRelative(_obj.parent, action.path, new Color(255f, 255f, 255f, .5f));
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(_obj.parent.localToWorldMatrix.MultiplyPoint(action.path[0]), gizmo_size);
                    Gizmos.DrawSphere(_obj.parent.localToWorldMatrix.MultiplyPoint(action.path[action.path.Length - 1]), gizmo_size);
                }
                else {
                    AMGizmo.DrawPath(action.path, new Color(255f, 255f, 255f, .5f));
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(action.path[0], gizmo_size);
                    Gizmos.DrawSphere(action.path[action.path.Length - 1], gizmo_size);
                }
            }
        }
    }

    private AMPath getPathFromIndex(int startIndex) {
        // sort the keys by frame		
        List<Vector3> path = new List<Vector3>();
        int endIndex, startFrame, endFrame;
        endIndex = startIndex;
        startFrame = keys[startIndex].frame;
        endFrame = keys[startIndex].frame;

        path.Add((keys[startIndex] as AMTranslationKey).position);


        // get path from startIndex until the next linear interpolation key (inclusive)
        for(int i = startIndex + 1; i < keys.Count; i++) {
            path.Add((keys[i] as AMTranslationKey).position);
            endFrame = keys[i].frame;
            endIndex = i;
            if((keys[i] as AMTranslationKey).interp == (int)AMTranslationKey.Interpolation.Linear) break;
        }
        return new AMPath(path.ToArray(), (keys[startIndex] as AMTranslationKey).interp, startFrame, endFrame, startIndex, endIndex);
    }
    // update cache (optimized)
    public override void updateCache() {
        // destroy cache
        destroyCache();
        // create new cache
        cache = new List<AMAction>();
        AMPath path;
        // sort keys
        sortKeys();
        // get all paths and add them to the action list
        for(int i = 0; i < keys.Count; i++) {
            path = getPathFromIndex(i);
            AMTranslationAction a = gameObject.AddComponent<AMTranslationAction>();
            a.enabled = false;
            a.isLocal = _isLocal;
            a.startFrame = path.startFrame;
            a.endFrame = path.endFrame;
            a.obj = _obj;
            a.path = path.path;
            a.easeType = (keys[i] as AMTranslationKey).easeType;
            a.customEase = new List<float>(keys[i].customEase);
            cache.Add(a);
            if(i < keys.Count - 1) i = path.endIndex - 1;
        }
        // update cache for orientation tracks with track obj as target
        foreach(AMTrack track in parentTake.trackValues) {
            if(track is AMOrientationTrack && ((track as AMOrientationTrack).obj == _obj || (track as AMOrientationTrack).hasTarget(_obj))) {
                track.updateCache();
            }
        }
        base.updateCache();
    }
    // get the starting translation key for the action where the frame lies
    public AMTranslationKey getActionStartKeyFor(int frame) {
        foreach(AMTranslationAction action in cache) {
            if((frame < action.startFrame) || (frame >= action.endFrame)) continue;
            return (AMTranslationKey)getKeyOnFrame(action.startFrame);
        }
        Debug.LogError("Animator: Action for frame " + frame + " does not exist in cache.");
        return new AMTranslationKey();
    }

    public Vector3 getInitialPosition() {
        return (keys[0] as AMTranslationKey).position;
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        if(!_obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "position";
        init.go = _obj.gameObject.name;
        AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
        v.setValue(getInitialPosition());
        init.position = v;
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

}
