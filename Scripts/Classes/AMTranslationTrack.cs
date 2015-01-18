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

    public bool pixelSnap;

    public float pixelPerUnit;
        
	protected override void SetSerializeObject(UnityEngine.Object obj) {
		_obj = obj as Transform;
	}
	
	protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
		return targetGO ? targetGO.transform : _obj;
	}
	
	public new void SetTarget(AMITarget target, Transform item) {
		base.SetTarget(target, item);
        _isLocal = true;
        if(item != null && keys.Count <= 0) cachedInitialPosition = _isLocal ? item.localPosition : item.position;
	}

    public override int version { get { return 2; } }

    public override bool hasTrackSettings { get { return true; } }

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
                        if(key.path != null) {
                            for(int i = 0;i < key.path.Length;i++) {
                                if(key.isLocal && !value) //to world
                                    key.path[i] = t.localToWorldMatrix.MultiplyPoint(key.path[i]);
                                else if(!key.isLocal && value) //to local
                                    key.path[i] = t.InverseTransformPoint(key.path[i]);
                            }
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

	void SetPosition(Transform t, Vector3 p) {
		if(t) {
            if(pixelSnap) p.Set(Mathf.Round(p.x*pixelPerUnit)/pixelPerUnit, Mathf.Round(p.y*pixelPerUnit)/pixelPerUnit, Mathf.Round(p.z*pixelPerUnit)/pixelPerUnit);
			if(_isLocal) t.localPosition = p;
			else t.position = p;
		}
	}

	Vector3 GetPosition(Transform t) {
		if(t) {
			return _isLocal ? t.localPosition : t.position;
		}
		return Vector3.zero;
	}

    private Vector3 cachedInitialPosition;

    public override string getTrackType() {
        return "Local Position";
    }

    // add a new key
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, Vector3 _position, int _interp, int _easeType) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.position = _position;
                key.interp = _interp;
                key.easeType = _easeType;
                // update cache
				updateCache(itarget);
                return;
            }
        }
        AMTranslationKey a = addCall(gameObject, typeof(AMTranslationKey)) as AMTranslationKey;
        a.frame = _frame;
        a.position = _position;
        a.interp = _interp;
        a.easeType = _easeType;

        // add a new key
        keys.Add(a);
        // update cache
		updateCache(itarget);
    }
    // add a new key, default interpolation and easeType
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, Vector3 _position) {
        foreach(AMTranslationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.position = _position;
                // update cache
				updateCache(itarget);
                return;
            }
        }
        AMTranslationKey a = addCall(gameObject, typeof(AMTranslationKey)) as AMTranslationKey;
        a.frame = _frame;
        a.position = _position;

        // add a new key
        keys.Add(a);
        // update cache
		updateCache(itarget);
    }

    // preview a frame in the scene view
    public override void previewFrame(AMITarget itarget, float frame, int frameRate, AMTrack extraTrack = null) {
		Transform t = GetTarget(itarget) as Transform;
        if(!t) return;
        if(keys == null || keys.Count <= 0) return;
        // if before first frame
        if(frame <= (float)(keys[0] as AMTranslationKey).startFrame) {
            AMTranslationKey key = keys[0] as AMTranslationKey;
            SetPosition(t, !key.canTween || key.path.Length == 0 ? key.position : key.path[0]);
            return;
        }
        // if beyond last frame
        if(frame >= (float)(keys[keys.Count - 1] as AMTranslationKey).endFrame) {
            AMTranslationKey key = keys[keys.Count - 1] as AMTranslationKey;
            SetPosition(t, !key.canTween || key.path.Length == 0 ? key.position : key.path[key.path.Length - 1]);
            return;
        }

        // if lies on curve
        foreach(AMTranslationKey key in keys) {
            if(((int)frame < key.startFrame) || ((int)frame > key.endFrame)) continue;
            if(!key.canTween && (int)frame < key.endFrame) {
				SetPosition(t, key.position);
				return;
			}
			else if(key.path.Length == 0) {
				continue;
			}
            else if(key.path.Length == 1) {
				SetPosition(t, key.path[0]);
                return;
            }
            float _value;
            float framePositionInPath = frame - (float)key.startFrame;
            if(framePositionInPath < 0f) framePositionInPath = 0f;

            if(key.hasCustomEase()) {
                _value = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInPath / key.getNumberOfFrames(frameRate), key.easeCurve);
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                _value = ease(framePositionInPath, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), key.amplitude, key.period);
                if(float.IsNaN(_value)) { //this really shouldn't happen...
                    return;
                }
            }

            SetPosition(t, key.GetPoint(Mathf.Clamp(_value, 0f, 1f)));

            return;
        }

    }

    // returns true if autoKey successful
    public bool autoKey(AMITarget itarget, OnAddKey addCall, Transform aobj, int frame, int frameRate) {
		Transform t = GetTarget(itarget) as Transform;
        if(!t || aobj != t) { return false; }

        if(keys.Count <= 0) {
            if(GetPosition(t) != cachedInitialPosition) {
                // if updated position, addkey
				addKey(itarget, addCall, frame, GetPosition(t));
                return true;
            }
            return false;
        }

        Vector3 oldPos = getPositionAtFrame(t, frame, frameRate, false);
		if(GetPosition(t) != oldPos) {
            // if updated position, addkey
			addKey(itarget, addCall, frame, GetPosition(t));
            return true;
        }

        return false;
    }
	public Vector3 getPositionAtFrame(Transform t, int frame, int frameRate, bool forceWorld) {
        Vector3 ret = Vector3.zero;

        if(keys.Count <= 0) ret = GetPosition(t);
        // if before first frame
        else if(frame <= (keys[0] as AMTranslationKey).startFrame) {
            AMTranslationKey key = keys[0] as AMTranslationKey;
            ret = !key.canTween || key.path.Length == 0 ? key.position : key.path[0];
        }
        // if beyond last frame
        else if(frame >= (keys[keys.Count - 1] as AMTranslationKey).endFrame) {
            AMTranslationKey key = keys[keys.Count - 1] as AMTranslationKey;
            ret = !key.canTween || key.path.Length == 0 ? key.position : key.path[key.path.Length - 1];
        }
        else {
            bool retFound = false;
            // if lies on curve
            foreach(AMTranslationKey key in keys) {
                if(frame < key.startFrame || frame > key.endFrame) continue;
                if(!key.canTween && frame < key.endFrame) {
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

                int framePositionInPath = frame - key.startFrame;
                if(framePositionInPath < 0) framePositionInPath = 0;

                // ease
                if(key.hasCustomEase()) {
                    ret = key.GetPoint(Mathf.Clamp(AMUtil.EaseCustom(0.0f, 1.0f, (float)framePositionInPath / (float)key.getNumberOfFrames(frameRate), key.easeCurve), 0.0f, 1.0f));
                    retFound = true;
                    break;
                }
                else {
                    TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                    ret = key.GetPoint(Mathf.Clamp(ease(framePositionInPath, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), key.amplitude, key.period), 0.0f, 1.0f));
                    retFound = true;
                    break;
                }
            }

            if(!retFound)
                Debug.LogError("Animator: Could not get " + t.name + " position at frame '" + frame + "'");
        }

        if(pixelSnap) ret.Set(Mathf.Round(ret.x*pixelPerUnit)/pixelPerUnit, Mathf.Round(ret.y*pixelPerUnit)/pixelPerUnit, Mathf.Round(ret.z*pixelPerUnit)/pixelPerUnit);

        if(forceWorld && _isLocal && t != null && t.parent != null)
            ret = t.parent.localToWorldMatrix.MultiplyPoint(ret);

        return ret;
    }
    // draw gizmos
    public override void drawGizmos(AMITarget target, float gizmo_size) {
		Transform t = GetTarget(target) as Transform;

        foreach(AMTranslationKey key in keys) {
            if(key != null) {
                if(!key.canTween) {
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(key.position, gizmo_size);
				}
				else if(key.path.Length > 1)
                    key.pathPreview.GizmoDraw(_isLocal ? t.parent : null, gizmo_size);
            }
        }
    }

    public override void undoRedoPerformed() {
        //path preview must be rebuilt
        foreach(AMTranslationKey key in keys)
            key.pathPreview = null;
    }

    // update cache (optimized)
    public override void updateCache(AMITarget target) {
		base.updateCache(target);

        //Debug.Log("update");

		//force local, using global is useless
		isLocal = true;

        // get all paths and add them to the action list
        for(int i = 0; i < keys.Count; i++) {
            AMTranslationKey key = keys[i] as AMTranslationKey;

			int easeType = key.easeType;

            key.version = version;

            AMPath path = new AMPath(keys, i);

            key.isLocal = _isLocal;
            key.startFrame = path.startFrame;
            key.endFrame = path.endFrame;
            key.pathPreview = null;

            if(!key.canTween) {
				key.path = new Vector3[0];

				if(path.endIndex == keys.Count - 1) {
					AMTranslationKey lastKey = keys[path.endIndex] as AMTranslationKey;
                    lastKey.interp = (int)AMTranslationKey.Interpolation.None;
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

	public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        if(!_obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "position";
        init.go = _obj.gameObject.name;
        AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
        v.setValue(getInitialPosition());
        init.position = v;
        return init;
    }

    public override List<GameObject> getDependencies(AMITarget target) {
		Transform t = GetTarget(target) as Transform;
        List<GameObject> ls = new List<GameObject>();
        if(t) ls.Add(t.gameObject);
        return ls;
    }

	public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
		Transform t = GetTarget(target) as Transform;
        if(!t) return new List<GameObject>();
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == t.gameObject) {
				SetTarget(target, newReferences[i].transform);
                break;
            }
        }
        return new List<GameObject>();
    }

    protected override void DoCopy(AMTrack track) {
        AMTranslationTrack ntrack = track as AMTranslationTrack;
        ntrack._obj = _obj;
        ntrack._isLocal = _isLocal;
        ntrack.cachedInitialPosition = cachedInitialPosition;
        ntrack.pixelSnap = pixelSnap;
        ntrack.pixelPerUnit = pixelPerUnit;
    }
}
