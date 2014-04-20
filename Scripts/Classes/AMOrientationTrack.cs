using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("")]
public class AMOrientationTrack : AMTrack {

    public override int order { get { return 1; } }

	[SerializeField]
    Transform obj;

	protected override void SetSerializeObject(UnityEngine.Object obj) {
		this.obj = obj as Transform;
	}
	
	protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
		return targetGO ? targetGO.transform : obj;
	}

    public override string getTrackType() {
        return "Orientation";
    }
    // add a new key
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, Transform target) {
        foreach(AMOrientationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
				key.SetTarget(itarget, target);
                // update cache
				updateCache(itarget);
                return;
            }
        }
        AMOrientationKey a = addCall(gameObject, typeof(AMOrientationKey)) as AMOrientationKey;
        a.frame = _frame;
		a.SetTarget(itarget, target);
        // set default ease type to linear
        a.easeType = (int)0;// AMTween.EaseType.linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(itarget);
    }
	public override void updateCache(AMITarget target) {
		base.updateCache(target);

		// save rotation
		//Quaternion temp = obj.rotation;

		for(int i = 0; i < keys.Count; i++) {
			AMOrientationKey key = keys[i] as AMOrientationKey;
			
			key.version = version;
			
			if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
			else {
				if(i > 0 && keys[i-1].easeType == AMKey.EaseTypeNone)
					key.easeType = AMKey.EaseTypeNone;

				key.endFrame = -1;
			}

			// targets
			if(key.endFrame != -1) key.SetTargetEnd(keys[i + 1] as AMOrientationKey);
		}
		// restore rotation
		//if(restoreRotation) obj.rotation = temp;
    }

    public Transform getInitialTarget(AMITarget itarget) {
		return (keys[0] as AMOrientationKey).GetTarget(itarget);
    }

	public override void previewFrame(AMITarget itarget, float frame, AMTrack extraTrack = null) {
		Transform t = GetTarget(itarget) as Transform;

        if(keys == null || keys.Count <= 0) {
            return;
        }
        for(int i = 0; i < keys.Count; i++) {
			AMOrientationKey key = keys[i] as AMOrientationKey;
			Transform keyt = key.GetTarget(itarget);
			Transform keyet = key.GetTargetEnd(itarget);

			if(key.easeType == AMKey.EaseTypeNone && frame == (float)key.endFrame && i < keys.Count - 1)
				continue;

            // before first frame
			if(frame <= key.frame) {
				if(!keyt) return;
				t.LookAt(keyt);
                return;
                // between first and last frame
            }
			else if(frame <= key.endFrame) {
				if(!keyt || !keyet) return;
                float framePositionInPath = frame - (float)keys[i].frame;
                if(framePositionInPath < 0f) framePositionInPath = 0f;
                float percentage = framePositionInPath / keys[i].getNumberOfFrames();
				t.rotation = key.getQuaternionAtPercent(itarget, t, percentage);
                return;
                // after last frame
            }
            else if(i == keys.Count - 2) {
				if(!keyet) return;
				t.LookAt(keyet);
                return;
            }
        }
    }

	public Transform getStartTargetForFrame(AMITarget itarget, float frame) {
        foreach(AMOrientationKey key in keys) {
            if(/*((int)frame<action.startFrame)||*/((int)frame > key.endFrame)) continue;
            return key.GetTarget(itarget);
        }
        return null;
    }
	public Transform getEndTargetForFrame(AMITarget itarget, float frame) {
		if(keys.Count > 1) return (keys[keys.Count - 2] as AMOrientationKey).GetTargetEnd(itarget);
        return null;
    }
	public Transform getTargetForFrame(AMITarget itarget, float frame) {
		if(isFrameBeyondLastKeyFrame(frame)) return getEndTargetForFrame(itarget, frame);
		else return getStartTargetForFrame(itarget, frame);
    }
    // draw gizmos
    public void drawGizmos(AMITarget itarget, float gizmo_size, bool inPlayMode, int frame) {
        if(!obj) return;
        // draw line to target
        if(!inPlayMode) {
            foreach(AMOrientationKey key in keys) {
                if(key == null)
                    continue;

                if(key.frame > frame) break;
                if(frame >= key.frame && frame <= key.endFrame) {
					Transform t = key.GetTarget(itarget);
					if(key.isLookFollow(itarget) && t) {
                        Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 0.2f);
						Gizmos.DrawLine(obj.transform.position, t.position);
                    }
                    break;
                }

            }
        }
        // draw arrow
        Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 1f);
        Vector3 pos = obj.transform.position;
        float size = (1.2f * (gizmo_size / 0.1f));
        if(size < 0.1f) size = 0.1f;
        Vector3 direction = obj.forward * size;
        float arrowHeadAngle = 20f;
        float arrowHeadLength = 0.3f * size;

        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public bool isFrameBeyondLastKeyFrame(float frame) {
        if(keys.Count <= 0) return false;
        else if((int)frame > keys[keys.Count - 1].frame) return true;
        else return false;
    }


	public bool hasTarget(AMITarget itarget, Transform obj) {
        foreach(AMOrientationKey key in keys) {
            if(key.GetTarget(itarget) == obj || key.GetTargetEnd(itarget) == obj) return true;
        }
        return false;
    }


	public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        if(!obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "orientation";
        init.go = obj.gameObject.name;
		Transform _target = getInitialTarget(target);
        int start_frame = keys[0].frame;
        AMTrack _translation_track = null;
        //if(start_frame > 0) _translation_track = parentTake.getTranslationTrackForTransform(_target);
        Vector3 _lookv3 = _target.transform.position;
		if(_translation_track) _lookv3 = (_translation_track as AMTranslationTrack).getPositionAtFrame((_translation_track as AMTranslationTrack).GetTarget(target) as Transform, start_frame, true);
        AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
        v.setValue(_lookv3);
        init.position = v;
        return init;
    }

	public override List<GameObject> getDependencies(AMITarget itarget) {
		Transform tgt = GetTarget(itarget) as Transform;
        List<GameObject> ls = new List<GameObject>();
		if(tgt) ls.Add(tgt.gameObject);
        foreach(AMOrientationKey key in keys) {
			Transform t = key.GetTarget(itarget);
            if(t) ls.Add(t.gameObject);
        }
        return ls;
    }
	public override List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
		Transform tgt = GetTarget(itarget) as Transform;
        bool didUpdateObj = false;
        for(int i = 0; i < oldReferences.Count; i++) {
			if(!didUpdateObj && tgt && oldReferences[i] == tgt.gameObject) {
				SetTarget(itarget, newReferences[i].transform);
                didUpdateObj = true;
            }
            foreach(AMOrientationKey key in keys) {
				Transform t = key.GetTarget(itarget);
                if(t && oldReferences[i] == t.gameObject) {
                    key.SetTarget(itarget, newReferences[i].transform);
                }
            }
        }

        return new List<GameObject>();
    }

    protected override void DoCopy(AMTrack track) {
        (track as AMOrientationTrack).obj = obj;
    }
}
