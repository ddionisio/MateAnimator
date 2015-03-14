using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Holoville.HOTween;

[AddComponentMenu("")]
public abstract class AMTrack : MonoBehaviour {
    public delegate AMKey OnAddKey(GameObject go, System.Type type);
    public delegate void OnKey(AMTrack track, AMKey key);

    public int id;
    public new string name;
    public List<AMKey> keys = new List<AMKey>();
    public bool foldout = true;							// whether or not to foldout track in timeline GUI

	[SerializeField]
	protected string _targetPath; //for animations saved as meta

    private bool mStarted;

    public virtual int version { get { return 1; } } //must be at least 1

    public virtual int order { get { return 0; } }

    /// <summary>
    /// If true, then track has settings to display even if there is no key selected.
    /// </summary>
    public virtual bool hasTrackSettings { get { return false; } }

    public virtual bool canTween { get { return true; } }

    public virtual int interpCount { get { return 2; } } //at some point, all tracks can potentially use curve...

	public string targetPath { get { return _targetPath; } }

    // set name based on index
    public void setName(int index) {
        name = "Track" + (index + 1);
    }

    public bool started { get { return mStarted; } }

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

		if(target.isMeta) {
			Transform tgt = target.GetCache(_targetPath);
            if(tgt)
                ret = GetSerializeObject(tgt.gameObject);
            else {
                tgt = AMUtil.GetTarget(target.root, _targetPath);
                target.SetCache(_targetPath, tgt);
                if(tgt)
                    ret = GetSerializeObject(tgt.gameObject);
            }
		}
		else
			ret = GetSerializeObject(null);

		return ret;
	}

	public virtual string GetRequiredComponent() {
		return "";
	}

    /// <summary>
    /// Check to see if given GameObject has all the required components for this track
    /// </summary>
    public bool VerifyComponents(GameObject go) {
        if(!go) return false;

        if(go.GetComponent(GetRequiredComponent()) == null)
            return false;

        foreach(AMKey key in keys) {
            if(go.GetComponent(key.GetRequiredComponent()) == null)
                return false;
        }

        return true;
    }

	public string GetTargetPath(AMITarget target) {
		if(target.isMeta)
			return _targetPath;
		else
			return AMUtil.GetPath(target.root, GetSerializeObject(null));
	}

	public void SetTarget(AMITarget target, Transform item) {
		if(target.isMeta && item) {
			_targetPath = AMUtil.GetPath(target.root, item);
			target.SetCache(_targetPath, item);
			SetSerializeObject(GetSerializeObject(item.gameObject));
		}
		else {
			_targetPath = "";
            SetSerializeObject(item ? GetSerializeObject(item.gameObject) : null);
		}
	}

	public virtual bool isTargetEqual(AMITarget target, UnityEngine.Object obj) {
		return GetTarget(target) == obj;
	}

	public virtual void maintainTrack(AMITarget itarget) {
		Object obj = null;

		//fix the target info
		if(itarget.isMeta) {
			if(string.IsNullOrEmpty(_targetPath)) {
				obj = GetSerializeObject(null);
				if(obj) {
					_targetPath = AMUtil.GetPath(itarget.root, obj);
					itarget.SetCache(_targetPath, AMUtil.GetTransform(obj));
				}
			}
			SetSerializeObject(null);
		}
		else {
			obj = GetSerializeObject(null);
			if(obj == null) {
				if(!string.IsNullOrEmpty(_targetPath)) {
					Transform tgt = itarget.GetCache(_targetPath);
					if(tgt == null)
						tgt = AMUtil.GetTarget(itarget.root, _targetPath);
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
    public virtual void drawGizmos(AMITarget target, float gizmo_size, bool inPlayMode, int frame) { }

    // preview frame
    public virtual void previewFrame(AMITarget target, float frame, int frameRate, AMTrack extraTrack = null) { }

    // update cache
    public virtual void updateCache(AMITarget target) {
		sortKeys();
    }

    /// <summary>
    /// Only called during editor
    /// </summary>
    public virtual void undoRedoPerformed() {

    }

	public virtual void buildSequenceStart(AMSequence sequence) {
    }

    public virtual void PlayStart(AMITarget itarget, float frame, int frameRate, float animScale) {
        if(!mStarted) {
            mStarted = true;

            //preview from starting frame so that the first tweener will grab the appropriate start value
            if(canTween && keys.Count > 1 && keys[0].canTween)
                previewFrame(itarget, 0f, frameRate);
        }
        else {
            //apply first frame if frame > first frame
            if(canTween && keys.Count > 1 && keys[0].canTween && keys[0].frame > Mathf.RoundToInt(frame))
                previewFrame(itarget, 0f, frameRate);
        }
    }

    /// <summary>
    /// Call when we are switching take
    /// </summary>
    public virtual void PlaySwitch(AMITarget itarget) {

    }

	public virtual AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        Debug.LogWarning("Animator: No override for getJSONInit()");
        return new AnimatorTimeline.JSONInit();
    }

    // get key on frame
    public AMKey getKeyOnFrame(int _frame, bool showWarning = true) {
        foreach(AMKey key in keys) {
            if(key.frame == _frame) return key;
        }
        if(showWarning)
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

    public int getKeyIndex(AMKey key) {
        return keys.IndexOf(key);
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

    public int getLastFrame(int frameRate) {
        if(keys.Count > 0) {
            int lastNumFrames = keys[keys.Count - 1].getNumberOfFrames(frameRate);
            if(lastNumFrames < 0)
                return keys[keys.Count - 1].frame;
            return keys[keys.Count - 1].frame + lastNumFrames;
        }
        return 0;
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

    protected virtual void DoCopy(AMTrack track) {
    }

	public void CopyTo(AMTrack track) {
        track.id = id;
        track.name = name;
        track._targetPath = _targetPath;
        DoCopy(track);
    }
}
