using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[AddComponentMenu("")]
public class AMAnimatorMateTrack : AMTrack {
    [SerializeField]
    AnimatorData obj;

    public override string getTrackType() {
        return "MateAnimator";
    }

    protected override void SetSerializeObject(UnityEngine.Object obj) {
        this.obj = obj as AnimatorData;
    }

    protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
        return targetGO ? targetGO.GetComponent<AnimatorData>() : obj;
    }

    public override string GetRequiredComponent() {
        return "AnimatorData";
    }

    // add a new key
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame) {
        foreach(AMAnimatorMateKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.take = "";
                key.loop = AMPlugMateAnimator.LoopType.None;
                // update cache
                updateCache(itarget);
                return;
            }
        }
        AMAnimatorMateKey a = addCall(gameObject, typeof(AMAnimatorMateKey)) as AMAnimatorMateKey;
        a.frame = _frame;
        a.take = "";
        a.loop = AMPlugMateAnimator.LoopType.None;
        a.duration = 0f;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache(itarget);
    }

    // preview a frame in the scene view
    public override void previewFrame(AMITarget target, float frame, int frameRate, AMTrack extraTrack = null) {
        AnimatorData anim = GetTarget(target) as AnimatorData;

        if(!anim || keys.Count == 0) return;

        if(frame < keys[0].frame) {
            AMAnimatorMateKey amKey = keys[0] as AMAnimatorMateKey;
            if(!string.IsNullOrEmpty(amKey.take)) {
                int takeInd = anim.GetTakeIndex(amKey.take);
                if(takeInd == -1)
                    return;

                AMTakeData take = anim._takes[takeInd];
                take.previewFrame(anim, 0f);
            }
            return;
        }

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame <= frame) {
                AMAnimatorMateKey amKey = keys[i] as AMAnimatorMateKey;
                amKey.updateDuration(this, target);

                if(!PreviewKey(anim, amKey, frame, frameRate)) {
                    //preview last frame
                    if(i > 0) {
                        frame = amKey.frame;
                        amKey = keys[i-1] as AMAnimatorMateKey;
                        amKey.updateDuration(this, target);
                        PreviewKey(anim, amKey, frame, frameRate);
                    }
                }
                break;
            }
        }
    }

    bool PreviewKey(AnimatorData anim, AMAnimatorMateKey amKey, float frame, int frameRate) {
        if(!string.IsNullOrEmpty(amKey.take)) {
            int takeInd = anim.GetTakeIndex(amKey.take);
            if(takeInd == -1)
                return false;

            AMTakeData take = anim._takes[takeInd];

            float t = (frame - (float)amKey.frame + 1f) / (float)frameRate;

            float aframe = t*take.frameRate;
            int iLastFrame;

            switch(amKey.loop) {
                case AMPlugMateAnimator.LoopType.Restart:
                    iLastFrame = take.getLastFrame();
                    aframe %= (float)iLastFrame;
                    break;
                case AMPlugMateAnimator.LoopType.Yoyo:
                    iLastFrame = take.getLastFrame();
                    float fLastFrame = iLastFrame;
                    int count = Mathf.FloorToInt(aframe)/iLastFrame;
                    if(count % 2 == 0)
                        aframe %= fLastFrame;
                    else {
                        aframe = fLastFrame - (aframe%fLastFrame);
                    }
                    break;
            }

            take.previewFrame(anim, aframe);
            return true;
        }
        return false;
    }

    public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        // no initial values to set
        return null;
    }

    public override List<GameObject> getDependencies(AMITarget target) {
        AnimatorData anim = GetTarget(target) as AnimatorData;
        List<GameObject> ls = new List<GameObject>();
        if(anim) ls.Add(anim.gameObject);
        return ls;
    }

    public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
        AnimatorData anim = GetTarget(target) as AnimatorData;
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        if(!anim) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == anim.gameObject) {
                // missing animation
                if(!newReferences[i].GetComponent(typeof(AnimatorData))) {
                    Debug.LogWarning("Animator: Animation Track component 'Animation' not found on new reference for GameObject '" + anim.name + "'. Duplicate not replaced.");
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
        (track as AMAnimatorMateTrack).obj = obj;
    }
}
