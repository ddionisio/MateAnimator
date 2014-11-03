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

    /// <summary>
    /// Called via AMTakeData's sampleAudioAtFrame
    /// </summary>
    public void sampleAudioAtFrame(AMITarget itarget, int frame, float speed, int frameRate) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            for(int i = keys.Count - 1; i >= 0; i--) {
                if(keys[i].frame <= frame) {
                    AMAnimatorMateKey key = keys[i] as AMAnimatorMateKey;
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd != -1) {
                        AMTakeData take = anim._takes[takeInd];
                        int takeFrame = Mathf.FloorToInt(((float)frame/frameRate)*(float)take.frameRate);
                        take.sampleAudioAtFrame(anim, takeFrame, speed);
                    }
                    else if(i > 0) {
                        //null take, stop audio from previous take
                        takeInd = anim.GetTakeIndex(((AMAnimatorMateKey)keys[i-1]).take);
                        if(takeInd != -1) {
                            AMTakeData take = anim._takes[takeInd];
                            take.endAudioLoops(anim);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called via AMTakeData's sampleAudio
    /// </summary>
    public void sampleAudio(AMITarget itarget, float frame, float speed, int frameRate, bool playOneShots) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            for(int i = keys.Count - 1; i >= 0; i--) {
                if(keys[i].frame <= frame) {
                    AMAnimatorMateKey key = keys[i] as AMAnimatorMateKey;

                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd != -1) {
                        AMTakeData take = anim._takes[takeInd];
                        float takeFrame = (frame/frameRate)*(float)take.frameRate;
                        take.sampleAudio(anim, takeFrame, speed, playOneShots);
                    }
                    break;
                }
            }
        }
    }

    public void endAudioLoops(AMITarget itarget) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            foreach(AMAnimatorMateKey key in keys) {
                if(!string.IsNullOrEmpty(key.take)) {
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd == -1)
                        return;

                    AMTakeData take = anim._takes[takeInd];
                    take.endAudioLoops(anim);
                }
            }
        }
    }

    public void stopAudio(AMITarget itarget) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            foreach(AMAnimatorMateKey key in keys) {
                if(!string.IsNullOrEmpty(key.take)) {
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd == -1)
                        return;

                    AMTakeData take = anim._takes[takeInd];
                    take.stopAudio(anim);
                }
            }
        }
    }

    public void pauseAudio(AMITarget itarget) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            foreach(AMAnimatorMateKey key in keys) {
                if(!string.IsNullOrEmpty(key.take)) {
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd == -1)
                        return;

                    AMTakeData take = anim._takes[takeInd];
                    take.pauseAudio(anim);
                }
            }
        }
    }

    public void resumeAudio(AMITarget itarget) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            foreach(AMAnimatorMateKey key in keys) {
                if(!string.IsNullOrEmpty(key.take)) {
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd == -1)
                        return;

                    AMTakeData take = anim._takes[takeInd];
                    take.resumeAudio(anim);
                }
            }
        }
    }

    public void setAudioSpeed(AMITarget itarget, float speed) {
        AnimatorData anim = GetTarget(itarget) as AnimatorData;
        if(anim) {
            foreach(AMAnimatorMateKey key in keys) {
                if(!string.IsNullOrEmpty(key.take)) {
                    int takeInd = anim.GetTakeIndex(key.take);
                    if(takeInd == -1)
                        return;

                    AMTakeData take = anim._takes[takeInd];
                    take.setAudioSpeed(anim, speed);
                }
            }
        }
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

    public void runFrame(AMITarget target, float frame, int frameRate, float animScale, bool playAudio, bool noAudioLoop) {
        AnimatorData anim = GetTarget(target) as AnimatorData;

        if(!anim || keys.Count == 0) return;

        if(frame < keys[0].frame) {
            AMAnimatorMateKey amKey = keys[0] as AMAnimatorMateKey;
            if(!string.IsNullOrEmpty(amKey.take)) {
                int takeInd = anim.GetTakeIndex(amKey.take);
                if(takeInd == -1)
                    return;

                AMTakeData take = anim._takes[takeInd];
                take.runFrame(anim, 0f, animScale, playAudio, noAudioLoop);
            }
            return;
        }

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame <= frame) {
                AMAnimatorMateKey amKey = keys[i] as AMAnimatorMateKey;

                AMTakeData take = GrabTake(anim, amKey);
                if(take != null) {
                    float subFrame = GrabFrame(take, amKey, frame, frameRate, anim.TargetAnimScale());
                    take.runFrame(anim, subFrame, animScale, playAudio, noAudioLoop);
                }
                else if(i > 0) { //jump to last frame of previous key
                    frame = amKey.frame;
                    amKey = keys[i-1] as AMAnimatorMateKey;
                    take = GrabTake(anim, amKey);
                    if(take != null) {
                        float subFrame = GrabFrame(take, amKey, frame, frameRate, anim.TargetAnimScale());
                        take.runFrame(anim, subFrame, animScale, playAudio, noAudioLoop);
                    }
                }
                break;
            }
        }
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

                AMTakeData take = GrabTake(anim, amKey);
                if(take != null) {
                    float subFrame = GrabFrame(take, amKey, frame, frameRate, anim.TargetAnimScale());
                    take.previewFrame(anim, subFrame);
                }
                else if(i > 0) { //jump to last frame of previous key
                    frame = amKey.frame;
                    amKey = keys[i-1] as AMAnimatorMateKey;
                    amKey.updateDuration(this, target);
                    take = GrabTake(anim, amKey);
                    if(take != null) {
                        float subFrame = GrabFrame(take, amKey, frame, frameRate, anim.TargetAnimScale());
                        take.previewFrame(anim, subFrame);
                    }
                }
                break;
            }
        }
    }

    AMTakeData GrabTake(AnimatorData anim, AMAnimatorMateKey amKey) {
        if(!string.IsNullOrEmpty(amKey.take)) {
            int takeInd = anim.GetTakeIndex(amKey.take);
            if(takeInd != -1)
                return anim._takes[takeInd];
        }
        return null;
    }

    // get the frame of the take relative to frame (of parent take)
    float GrabFrame(AMTakeData take, AMAnimatorMateKey amKey, float frame, int frameRate, float animScale) {
        float t = ((frame - (float)amKey.frame + 1f) / (float)frameRate)*animScale;

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

        return aframe;
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
