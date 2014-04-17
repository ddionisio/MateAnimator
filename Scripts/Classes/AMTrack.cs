using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Holoville.HOTween;

[AddComponentMenu("")]
public abstract class AMTrack : MonoBehaviour {
    public delegate void OnKey(AMTrack track, AMKey key);

    public int id;
    public new string name;
    public List<AMKey> keys = new List<AMKey>();
    public bool foldout = true;							// whether or not to foldout track in timeline GUI

	[SerializeField]
	protected string _targetPath; //for animations saved as meta

    public virtual int version { get { return 1; } } //must be at least 1

    public virtual int order { get { return 0; } }

	public string targetPath { get { return _targetPath; } }

    // set name based on index
    public void setName(int index) {
        name = "Track" + (index + 1);
    }

	/// <summary>
	/// Stores the obj to serialized field based on track type, obj is null if _targetPath is used
	/// </summary>
	/// <param name="target">Target.</param>
	protected abstract void SetSerializeObject(UnityEngine.Object obj);

	/// <summary>
	/// Gets the serialize object. If targetGO is not null, return the appropriate object from targetGO (e.g. if it's a specific component).
	/// Otherwise, if targetGO is null, grab from serialized field
	/// </summary>
	/// <returns>The serialize object.</returns>
	/// <param name="go">Go.</param>
	protected abstract UnityEngine.Object GetSerializeObject(GameObject targetGO);

	/// <summary>
	/// Gets the target relative to animator's hierarchy if we are referencing via AnimatorMeta
	/// </summary>
	public UnityEngine.Object GetTarget(AMITarget target) {
		UnityEngine.Object ret = null;

		if(target.TargetIsMeta()) {
			Transform tgt = target.TargetGetCache(_targetPath);
			if(tgt == null) {
				tgt = AMUtil.GetTarget(target.TargetGetRoot(), _targetPath);
			}

			if(tgt) {
				ret = GetSerializeObject(tgt.gameObject);
				target.TargetSetCache(_targetPath, tgt);
			}
			else {
				target.TargetMissing(_targetPath, true);
			}
		}
		else {
			ret = GetSerializeObject(null);
		}

		return ret;
	}

	public virtual string GetRequiredComponent() {
		return "";
	}

	public string GetTargetPath(AMITarget target) {
		if(target.TargetIsMeta())
			return _targetPath;
		else
			return AMUtil.GetPath(target.TargetGetRoot(), GetSerializeObject(null));
	}

	public void SetTarget(AMITarget target, UnityEngine.Object item) {
		if(target.TargetIsMeta()) {
			target.TargetMissing(_targetPath, false);
			_targetPath = AMUtil.GetPath(target.TargetGetRoot(), item);
			target.TargetSetCache(_targetPath, AMUtil.GetTransform(item));
			SetSerializeObject(null);
		}
		else {
			_targetPath = "";
			SetSerializeObject(item);
		}
	}

	public virtual bool isTargetEqual(AMITarget target, UnityEngine.Object obj) {
		return GetTarget(target) == obj;
	}

	public virtual void maintainTrack(AMITarget itarget) {
		Object obj = null;

		//fix the target info
		if(itarget.TargetIsMeta()) {
			if(string.IsNullOrEmpty(_targetPath)) {
				obj = GetSerializeObject(null);
				if(obj) {
					_targetPath = AMUtil.GetPath(itarget.TargetGetRoot(), obj);
					itarget.TargetSetCache(_targetPath, AMUtil.GetTransform(obj));
				}
			}
			SetSerializeObject(null);
		}
		else {
			obj = GetSerializeObject(null);
			if(obj == null) {
				if(!string.IsNullOrEmpty(_targetPath)) {
					Transform tgt = itarget.TargetGetCache(_targetPath);
					if(tgt == null)
						tgt = AMUtil.GetTarget(itarget.TargetGetRoot(), _targetPath);
					if(tgt)
						obj = GetSerializeObject(tgt.gameObject);
					SetSerializeObject(obj);
				}
			}
			_targetPath = "";
		}

		//maintain keys
		foreach(AMKey key in keys)
			key.maintainKey(itarget, obj);
	}

    // set name from string
    public void setName(string name) {
        this.name = name;
    }

    // does track have key on frame
    public bool hasKeyOnFrame(int _frame) {
        foreach(AMKey key in keys) {
            if(key && key.frame == _frame) return true;
        }
        return false;
    }

    // draw track gizmos
	public virtual void drawGizmos(AMITarget target, float gizmo_size) { }

    // preview frame
	public virtual void previewFrame(AMITarget target, float frame, AMTrack extraTrack = null) { }

    // update cache
    public virtual void updateCache(AMITarget target) {
		sortKeys();
    }

	public virtual void buildSequenceStart(AMITarget target, Sequence s, int frameRate) {
    }

	public virtual AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        Debug.LogWarning("Animator: No override for getJSONInit()");
        return new AnimatorTimeline.JSONInit();
    }

    // get key on frame
    public AMKey getKeyOnFrame(int _frame) {
        foreach(AMKey key in keys) {
            if(key.frame == _frame) return key;
        }
        Debug.LogError("Animator: No key found on frame " + _frame);
        return null;
    }

    // track type as string
    public virtual string getTrackType() {
        return "Unknown";
    }

    public void sortKeys() {
        // sort
        keys.Sort((c, d) => c.frame.CompareTo(d.frame));
    }

    public void deleteKeyOnFrame(int frame) {
        for(int i = 0; i < keys.Count; i++) {
            if(keys[i].frame == frame) {
                keys[i].destroy();
                keys.RemoveAt(i);
            }
        }
    }

	public AMKey[] removeKeyOnFrame(int frame) {
		List<AMKey> rkeys = new List<AMKey>(keys.Count);
		for(int i = 0; i < keys.Count; i++) {
			if(keys[i].frame == frame) {
				rkeys.Add(keys[i]);
				keys.RemoveAt(i);
			}
		}
		return rkeys.ToArray();
	}
	
	public void deleteDuplicateKeys() {
		sortKeys();
        int lastKey = -1;
        for(int i = 0; i < keys.Count; i++) {
            if(keys[i].frame == lastKey) {
                keys[i].destroy();
                keys.RemoveAt(i);
                i--;
            }
            else {
                lastKey = keys[i].frame;
            }
        }
    }

	public AMKey[] removeDuplicateKeys() {
		List<AMKey> dkeys = new List<AMKey>();

		sortKeys();
		int lastKey = -1;
		for(int i = 0; i < keys.Count; i++) {
			if(keys[i].frame == lastKey) {
				dkeys.Add(keys[i]);
				keys.RemoveAt(i);
				i--;
			}
			else {
				lastKey = keys[i].frame;
			}
		}

		return dkeys.ToArray();
	}

    public void deleteAllKeys() {
        foreach(AMKey key in keys) {
            key.destroy();
        }
        keys = new List<AMKey>();
    }

    public void deleteKeysAfter(int frame) {

        for(int i = 0; i < keys.Count; i++) {
            if(keys[i].frame > frame) {
                keys[i].destroy();
                keys.RemoveAt(i);
                i--;
            }
        }
    }

	public AMKey[] removeKeysAfter(int frame) {
		List<AMKey> dkeys = new List<AMKey>();
		for(int i = 0; i < keys.Count; i++) {
			if(keys[i].frame > frame) {
				dkeys.Add(keys[i]);
				keys.RemoveAt(i);
				i--;
			}
		}
		return dkeys.ToArray();
	}

    public void destroy() {
        // destroy keys
        if(keys != null) {
            foreach(AMKey key in keys) {
                if(key)
                    key.destroy();
            }

            keys.Clear();
            keys = null;
        }

        // destroy track
        Object.DestroyImmediate(this);
    }

	public virtual List<GameObject> getDependencies(AMITarget target) {
        return new List<GameObject>();
    }

	public virtual List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
        return new List<GameObject>();
    }

	public void offsetKeysFromBy(AMITarget target, int frame, int amount) {
        if(keys.Count <= 0) return;
        for(int i = 0; i < keys.Count; i++) {
            if(frame <= 0 || keys[i].frame >= frame) keys[i].frame += amount;
        }
		updateCache(target);
    }

    // returns the offset
	public int shiftOutOfBoundsKeys(AMITarget target) {
        if(keys.Count <= 0) return 0;
        sortKeys();
        if(keys[0].frame >= 1) return 0;
        int offset = 0;
        offset = Mathf.Abs(keys[0].frame) + 1; // calculate shift: -1 = 1+1 etc
        foreach(AMKey key in keys) {
            key.frame += offset;
        }
		updateCache(target);
        return offset;
    }

    // get action for frame from cache
    public AMKey getKeyContainingFrame(int frame) {
        for(int i = keys.Count - 1; i >= 0; i--) {
            if(frame >= keys[i].frame) return keys[i];
        }
        if(keys.Count > 0) return keys[0];	// return first if not greater than any action
        Debug.LogError("Animator: No key found for frame " + frame);
        return null;
    }

    // get action for frame from cache
    public AMKey getKeyForFrame(int startFrame) {
        foreach(AMKey key in keys) {
            if(key.frame == startFrame) return key;
        }
        Debug.LogError("Animator: No key found for frame " + startFrame);
        return null;
    }

    // get index of action for frame
    public int getKeyIndexForFrame(int startFrame) {
        for(int i = 0; i < keys.Count; i++) {
            if(keys[i].frame == startFrame) return i;
        }
        return -1;
    }

    // if whole take is true, when end frame is reached the last keyframe will be returned. This is used for the playback controls
    public int getKeyFrameAfterFrame(int frame, bool wholeTake = true) {
        foreach(AMKey key in keys) {
            if(key.frame > frame) return key.frame;
        }
        if(!wholeTake) return -1;
        if(keys.Count > 0) return keys[0].frame;
        Debug.LogError("Animator: No key found after frame " + frame);
        return -1;
    }

    // if whole take is true, when start frame is reached the last keyframe will be returned. This is used for the playback controls
    public int getKeyFrameBeforeFrame(int frame, bool wholeTake = true) {

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame < frame) return keys[i].frame;
        }
        if(!wholeTake) return -1;
        if(keys.Count > 0) return keys[keys.Count - 1].frame;
        Debug.LogError("Animator: No key found before frame " + frame);
        return -1;
    }

    public AMKey[] getKeyFramesInBetween(int startFrame, int endFrame) {
        List<AMKey> lsKeys = new List<AMKey>();
        if(startFrame <= 0 || endFrame <= 0 || startFrame >= endFrame || !hasKeyOnFrame(startFrame) || !hasKeyOnFrame(endFrame)) return lsKeys.ToArray();
        sortKeys();
        foreach(AMKey key in keys) {
            if(key.frame >= endFrame) break;
            if(key.frame > startFrame) lsKeys.Add(key);
        }
        return lsKeys.ToArray();
    }

    public float[] getKeyFrameRatiosInBetween(int startFrame, int endFrame) {
        List<float> lsKeyRatios = new List<float>();
        if(startFrame <= 0 || endFrame <= 0 || startFrame >= endFrame || !hasKeyOnFrame(startFrame) || !hasKeyOnFrame(endFrame)) return lsKeyRatios.ToArray();
        sortKeys();
        foreach(AMKey key in keys) {
            if(key.frame >= endFrame) break;
            if(key.frame > startFrame) lsKeyRatios.Add((float)(key.frame - startFrame) / (float)(endFrame - startFrame));
        }
        return lsKeyRatios.ToArray();
    }

	protected virtual AMTrack doDuplicate(GameObject holder) {
        return null;
    }

	public AMTrack duplicate(GameObject holder) {
		AMTrack ntrack = doDuplicate(holder);
		if(ntrack != null) {
            ntrack.id = id;
            ntrack.name = name;
			ntrack._targetPath = _targetPath;
        }

        return ntrack;
    }
}
