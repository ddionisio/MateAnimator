using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[AddComponentMenu("")]
public class AMAnimationTrack : AMTrack {
    public override bool canTween { get { return false; } }

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

	public override System.Type GetRequiredComponent() {
		return typeof(Animation);
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
                return;
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
    public override void previewFrame(AMITarget target, float frame, int frameRate, bool play, float playSpeed) {
		GameObject go = GetTarget(target) as GameObject;
		if(!go || keys.Count == 0) return;

        Animation anim = go.GetComponent<Animation>();

        if(frame < keys[0].frame) {
            AMAnimationKey amKey = keys[0] as AMAnimationKey;
            if(amKey.amClip)
                AMUtil.SampleAnimation(anim, amKey.amClip.name, amKey.wrapMode, amKey.crossfade ? 0.0f : 1.0f, 0.0f);
            return;
        }

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame <= frame) {
                AMAnimationKey amKey = keys[i] as AMAnimationKey;
                if(amKey.amClip) {
                    float t = (frame - (float)amKey.frame) / (float)frameRate;

                    if(amKey.crossfade) {
                        if(i > 0) {
                            AMAnimationKey amPrevKey = keys[i - 1] as AMAnimationKey;
                            if(amPrevKey.amClip) {
                                float prevT = (frame - (float)amPrevKey.frame) / (float)frameRate;
                                AMUtil.SampleAnimationCrossFade(anim, amKey.crossfadeTime, amPrevKey.amClip.name, amPrevKey.wrapMode, prevT, amKey.amClip.name, amKey.wrapMode, t);
                            }
                        }
                        else
                            AMUtil.SampleAnimationFadeIn(anim, amKey.amClip.name, amKey.wrapMode, amKey.crossfadeTime, t);
                    }
                    else
                        AMUtil.SampleAnimation(anim, amKey.amClip.name, amKey.wrapMode, 1.0f, t);
                }
                break;
            }
        }
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
