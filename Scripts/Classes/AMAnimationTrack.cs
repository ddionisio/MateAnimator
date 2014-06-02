using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[AddComponentMenu("")]
public class AMAnimationTrack : AMTrack {
    // to do
    // sample currently selected clip
	[SerializeField]
    GameObject obj;

    public override string getTrackType() {
        return "Animation";
    }

	protected override void SetSerializeObject(UnityEngine.Object obj) {
		this.obj = obj as GameObject;
	}

	protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
		return targetGO ? targetGO : obj;
	}

	public override string GetRequiredComponent() {
		return "Animation";
	}

    // add a new key
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, AnimationClip _clip, WrapMode _wrapMode) {
        foreach(AMAnimationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.amClip = _clip;
                key.wrapMode = _wrapMode;
                // update cache
				updateCache(itarget);
            }
        }
        AMAnimationKey a = addCall(gameObject, typeof(AMAnimationKey)) as AMAnimationKey;
        a.frame = _frame;
        a.amClip = _clip;
        a.wrapMode = _wrapMode;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(itarget);
    }
    // preview a frame in the scene view
	public void previewFrame(AMITarget target, float frame, float frameRate) {
		GameObject go = GetTarget(target) as GameObject;
		if(!go) return;
        bool found = false;
        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame <= frame) {

                AnimationClip amClip = (keys[i] as AMAnimationKey).amClip;
                if(amClip) {
                    amClip.wrapMode = (keys[i] as AMAnimationKey).wrapMode;
					go.SampleAnimation(amClip, getTime(frameRate, frame - keys[i].frame));
                }
                found = true;
                break;
            }

        }
        // sample default animation if not found
		if(!found && go.animation.clip) go.SampleAnimation(go.animation.clip, 0f);
    }
    public float getTime(float frameRate, float numberOfFrames) {
        return (float)numberOfFrames / (float)frameRate;
    }

	public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        // no initial values to set
        return null;
    }

    public override List<GameObject> getDependencies(AMITarget target) {
		GameObject go = GetTarget(target) as GameObject;
        List<GameObject> ls = new List<GameObject>();
		if(go) ls.Add(go);
        return ls;
    }

	public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
		GameObject go = GetTarget(target) as GameObject;
        List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!go) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
			if(oldReferences[i] == go) {
                // missing animation
                if(!newReferences[i].GetComponent(typeof(Animation))) {
					Debug.LogWarning("Animator: Animation Track component 'Animation' not found on new reference for GameObject '" + go.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
				SetTarget(target, newReferences[i].transform);
                break;
            }
        }

        return lsFlagToKeep;
    }

    protected override void DoCopy(AMTrack track) {
        (track as AMAnimationTrack).obj = obj;
    }
}
