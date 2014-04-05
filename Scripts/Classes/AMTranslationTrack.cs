using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween.Core;
using Holoville.HOTween;

//Note: no longer using global

[AddComponentMenu("")]
public class AMTranslationTrack : AMTrack {
    [SerializeField]
    private Transform _obj;
    public Transform obj {
        set {
            if(_obj != value) {
                if(value != null && keys.Count <= 0) cachedInitialPosition = _isLocal ? value.localPosition : value.position;
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
                    if(_obj != null && keys.Count <= 0) cachedInitialPosition = _obj.position;
                }
                else {
                    if(_obj != null && keys.Count <= 0) cachedInitialPosition = _obj.localPosition;
                }

                if(_obj != null && _obj.parent != null) {
                    Transform t = _obj.parent;

                    foreach(AMTranslationKey key in keys) {
                        for(int i = 0; i < key.path.Length; i++) {
                            if(key.isLocal && !value) //to world
                                key.path[i] = t.localToWorldMatrix.MultiplyPoint(key.path[i]);
                            else if(!key.isLocal && value) //to local
                                key.path[i] = t.InverseTransformPoint(key.path[i]);
                        }

                        key.isLocal = value;

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
    public AMKey addKey(int _frame, Vector3 _position, int _interp, int _easeType, OnKey addCallback) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                if(addCallback != null)
                    addCallback(this, null);

                key.position = _position;
                key.interp = _interp;
                key.easeType = _easeType;
                // update cache
                updateCache();
                return null;
            }
        }
        AMTranslationKey a = gameObject.AddComponent<AMTranslationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.position = _position;
        a.interp = _interp;
        a.easeType = _easeType;

        if(addCallback != null)
            addCallback(this, a);

        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
        return a;
    }
    // add a new key, default interpolation and easeType
    public AMKey addKey(int _frame, Vector3 _position, OnKey addCallback) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                if(addCallback != null)
                    addCallback(this, null);

                key.position = _position;
                // update cache
                updateCache();
                return null;
            }
        }
        AMTranslationKey a = gameObject.AddComponent<AMTranslationKey>();

        a.enabled = false;
        a.frame = _frame;
        a.position = _position;

        if(addCallback != null)
            addCallback(this, a);

        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
        return a;
    }

    // preview a frame in the scene view
    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        if(!_obj) return;
        if(keys == null || keys.Count <= 0) return;
        // if before first frame
        if(frame <= (float)(keys[0] as AMTranslationKey).startFrame) {
            AMTranslationKey key = keys[0] as AMTranslationKey;
			position = key.easeType == AMKey.EaseTypeNone || key.path.Length == 0 ? key.position : key.path[0];
            return;
        }
        // if beyond last frame
        if(frame >= (float)(keys[keys.Count - 1] as AMTranslationKey).endFrame) {
            AMTranslationKey key = keys[keys.Count - 1] as AMTranslationKey;
			position = key.easeType == AMKey.EaseTypeNone || key.path.Length == 0 ? key.position : key.path[key.path.Length - 1];
            return;
        }
        // if lies on curve
        foreach(AMTranslationKey key in keys) {
            if(((int)frame < key.startFrame) || ((int)frame > key.endFrame)) continue;
			if(key.easeType == AMKey.EaseTypeNone && (int)frame < key.endFrame) {
				position = key.position;
				return;
			}
			else if(key.path.Length == 0) {
				continue;
			}
            else if(key.path.Length == 1) {
                position = key.path[0];
                return;
            }
            float _value;
            float framePositionInPath = frame - (float)key.startFrame;
            if(framePositionInPath < 0f) framePositionInPath = 0f;

            if(key.hasCustomEase()) {
                _value = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInPath / key.getNumberOfFrames(), key.easeCurve);
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                _value = ease(framePositionInPath, 0.0f, 1.0f, key.getNumberOfFrames(), key.amplitude, key.period);
                if(float.IsNaN(_value)) { //this really shouldn't happen...
                    return;
                }
            }

            AMUtil.PutOnPath(_obj, key.path, Mathf.Clamp(_value, 0f, 1f), _isLocal);
            return;
        }

    }
    // returns true if autoKey successful
    public bool autoKey(Transform aobj, int frame, OnKey addCallback) {
        if(!_obj || aobj != _obj) { return false; }

        if(keys.Count <= 0) {
            if(position != cachedInitialPosition) {
                // if updated position, addkey
                addKey(frame, position, addCallback);
                return true;
            }
            return false;
        }
        Vector3 oldPos = getPositionAtFrame((float)frame, false);
        if(position != oldPos) {
            // if updated position, addkey
            addKey(frame, position, addCallback);
            return true;
        }

        return false;
    }
    public Vector3 getPositionAtFrame(float frame, bool forceWorld) {
        Vector3 ret = Vector3.zero;

        if(keys.Count <= 0) ret = position;
        // if before first frame
        else if(frame <= (float)(keys[0] as AMTranslationKey).startFrame) {
            AMTranslationKey key = keys[0] as AMTranslationKey;
            ret = key.easeType == AMKey.EaseTypeNone || key.path.Length == 0 ? key.position : key.path[0];
        }
        // if beyond last frame
        else if(frame >= (float)(keys[keys.Count - 1] as AMTranslationKey).endFrame) {
            AMTranslationKey key = keys[keys.Count - 1] as AMTranslationKey;
			ret = key.easeType == AMKey.EaseTypeNone || key.path.Length == 0 ? key.position : key.path[key.path.Length - 1];
        }
        else {
            bool retFound = false;
            // if lies on curve
            foreach(AMTranslationKey key in keys) {
                if(((int)frame < key.startFrame) || ((int)frame > key.endFrame)) continue;
				if(key.easeType == AMKey.EaseTypeNone && (int)frame < key.endFrame) {
					ret = key.position;
					retFound = true;
					break;
				}
				else if(key.path.Length == 0) {
					continue;
				}
                else if(key.path.Length == 1) {
                    ret = key.path[0];
                    retFound = true;
                    break;
                }

                float framePositionInPath = frame - (float)key.startFrame;
                if(framePositionInPath < 0f) framePositionInPath = 0f;

                // ease
                if(key.hasCustomEase()) {
                    ret = AMUtil.PointOnPath(key.path, Mathf.Clamp(AMUtil.EaseCustom(0.0f, 1.0f, framePositionInPath / key.getNumberOfFrames(), key.easeCurve), 0.0f, 1.0f));
                    retFound = true;
                    break;
                }
                else {
                    TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                    ret = AMUtil.PointOnPath(key.path, Mathf.Clamp(ease(framePositionInPath, 0.0f, 1.0f, key.getNumberOfFrames(), key.amplitude, key.period), 0.0f, 1.0f));
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
        foreach(AMTranslationKey key in keys) {
            if(key != null) {
				if(key.easeType == AMKey.EaseTypeNone) {
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(key.position, gizmo_size);
				}
				else if(key.path.Length > 1) {
	                if(_isLocal && _obj != null && _obj.parent != null) {
	                    AMGizmo.DrawPathRelative(_obj.parent, key.path, new Color(255f, 255f, 255f, .5f));
	                    Gizmos.color = Color.green;
	                    Gizmos.DrawSphere(_obj.parent.localToWorldMatrix.MultiplyPoint(key.path[0]), gizmo_size);
	                    Gizmos.DrawSphere(_obj.parent.localToWorldMatrix.MultiplyPoint(key.path[key.path.Length - 1]), gizmo_size);
	                }
	                else {
	                    AMGizmo.DrawPath(key.path, new Color(255f, 255f, 255f, .5f));
	                    Gizmos.color = Color.green;
	                    Gizmos.DrawSphere(key.path[0], gizmo_size);
	                    Gizmos.DrawSphere(key.path[key.path.Length - 1], gizmo_size);
	                }
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
			AMTranslationKey key = keys[i] as AMTranslationKey;
            path.Add(key.position);
            endFrame = keys[i].frame;
            endIndex = i;
			if(keys[startIndex].easeType == AMKey.EaseTypeNone 
			   || key.easeType == AMKey.EaseTypeNone 
			   || key.interp == (int)AMTranslationKey.Interpolation.Linear) break;
        }
        return new AMPath(path.ToArray(), (keys[startIndex] as AMTranslationKey).interp, startFrame, endFrame, startIndex, endIndex);
    }
    // update cache (optimized)
    public override void updateCache() {
		base.updateCache();

		//force local, using global is useless
		isLocal = true;

        AMPath path;

        // get all paths and add them to the action list
        for(int i = 0; i < keys.Count; i++) {
            AMTranslationKey key = keys[i] as AMTranslationKey;

			int easeType = key.easeType;

            key.version = version;
			            
            path = getPathFromIndex(i);

            key.isLocal = _isLocal;
            key.startFrame = path.startFrame;
            key.endFrame = path.endFrame;

			if(key.easeType == AMKey.EaseTypeNone) {
				key.path = new Vector3[0];

				if(path.endIndex == keys.Count - 1) {
					AMTranslationKey lastKey = keys[path.endIndex] as AMTranslationKey;
					lastKey.easeType = AMKey.EaseTypeNone;
					lastKey.isLocal = _isLocal;
					lastKey.startFrame = path.endFrame;
					lastKey.endFrame = path.endFrame;
					lastKey.path = new Vector3[0];
				}
			}
			else {
				key.path = path.path;
			}

            //invalidate some keys in between
            if(path.startIndex < keys.Count - 1) {
                for(i = path.startIndex + 1; i <= path.endIndex - 1; i++) {
                    key = keys[i] as AMTranslationKey;

                    key.version = version;
					key.easeType = easeType;
                    key.isLocal = _isLocal;
                    key.startFrame = key.frame;
                    key.endFrame = key.frame;
                    key.path = new Vector3[0];
                }

                i = path.endIndex - 1;
            }
        }
    }
    // get the starting translation key for the action where the frame lies
    public AMTranslationKey getKeyStartFor(int frame) {
        foreach(AMTranslationKey key in keys) {
            if((frame < key.startFrame) || (frame >= key.endFrame)) continue;
            return (AMTranslationKey)getKeyOnFrame(key.startFrame);
        }
        Debug.LogError("Animator: Action for frame " + frame + " does not exist in cache.");
        return null;
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

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMTranslationTrack ntrack = newTake.gameObject.AddComponent<AMTranslationTrack>();
        ntrack.enabled = false;
        ntrack._obj = _obj;
        ntrack._isLocal = _isLocal;
        ntrack.cachedInitialPosition = cachedInitialPosition;

        return ntrack;
    }
}
