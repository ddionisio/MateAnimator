using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[AddComponentMenu("")]
public class AMAnimationTrack : AMTrack {
    // to do
    // sample currently selected clip
    public GameObject obj;

    public override string getTrackType() {
        return "Animation";
    }

	public override UnityEngine.Object target {
		get {
			return obj;
		}
	}

    // add a new key
    public AMKey addKey(int _frame, AnimationClip _clip, WrapMode _wrapMode) {
        foreach(AMAnimationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.amClip = _clip;
                key.wrapMode = _wrapMode;
                // update cache
                updateCache();
                return null;
            }
        }
        AMAnimationKey a = gameObject.AddComponent<AMAnimationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.amClip = _clip;
        a.wrapMode = _wrapMode;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();

        return a;
    }
    // preview a frame in the scene view
    public void previewFrame(float frame, float frameRate) {
        if(!obj) return;
        bool found = false;
        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame <= frame) {

                AnimationClip amClip = (keys[i] as AMAnimationKey).amClip;
                if(!amClip) {
                    // do nothing
                }
                else {
                    amClip.wrapMode = (keys[i] as AMAnimationKey).wrapMode;
                    obj.SampleAnimation(amClip, getTime(frameRate, frame - keys[i].frame));
                }
                found = true;
                break;
            }

        }
        // sample default animation if not found
        if(!found && obj.animation.clip) obj.SampleAnimation(obj.animation.clip, 0f);
    }
    public float getTime(float frameRate, float numberOfFrames) {
        return (float)numberOfFrames / (float)frameRate;
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        // no initial values to set
        return null;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(obj) ls.Add(obj);
        return ls;
    }

    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        if(!obj) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == obj) {
                // missing animation
                if(!newReferences[i].GetComponent(typeof(Animation))) {
                    Debug.LogWarning("Animator: Animation Track component 'Animation' not found on new reference for GameObject '" + obj.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
                obj = newReferences[i];
                break;
            }
        }

        return lsFlagToKeep;
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMAnimationTrack ntrack = newTake.gameObject.AddComponent<AMAnimationTrack>();
        ntrack.enabled = false;
        ntrack.obj = obj;

        return ntrack;
    }
}
