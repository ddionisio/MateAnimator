using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
    [System.Serializable]
    public class UnityAnimationTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.UnityAnimation; } }

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

        public override bool CheckComponent(GameObject go) {
            return go.GetComponent<Animation>() != null;
        }

        public override void AddComponent(GameObject go) {
            go.AddComponent<Animation>();
        }

        // add a new key
        public void addKey(ITarget itarget, int _frame, AnimationClip _clip, WrapMode _wrapMode) {
            foreach(UnityAnimationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.amClip = _clip;
                    key.wrapMode = _wrapMode;
                    // update cache
                    updateCache(itarget);
                    return;
                }
            }
            var a = new UnityAnimationKey();
            a.frame = _frame;
            a.amClip = _clip;
            a.wrapMode = _wrapMode;
            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }
        // preview a frame in the scene view
        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            GameObject go = GetTarget(target) as GameObject;
            if(!go || keys.Count == 0) return;

            Animation anim = go.GetComponent<Animation>();
            if(!anim) return;

            if(frame < keys[0].frame) {
                var amKey = keys[0] as UnityAnimationKey;
                if(amKey.amClip)
                    Utility.SampleAnimation(anim, amKey.amClip.name, amKey.wrapMode, amKey.crossfade ? 0.0f : 1.0f, 0.0f);
                return;
            }

            for(int i = keys.Count - 1; i >= 0; i--) {
                if(keys[i].frame <= frame) {
                    var amKey = keys[i] as UnityAnimationKey;
                    if(amKey.amClip) {
                        float t = (frame - (float)amKey.frame) / (float)frameRate;

                        if(amKey.crossfade) {
                            if(i > 0) {
                                var amPrevKey = keys[i - 1] as UnityAnimationKey;
                                if(amPrevKey.amClip) {
                                    float prevT = (frame - (float)amPrevKey.frame) / (float)frameRate;
                                    Utility.SampleAnimationCrossFade(anim, amKey.crossfadeTime, amPrevKey.amClip.name, amPrevKey.wrapMode, prevT, amKey.amClip.name, amKey.wrapMode, t);
                                }
                            }
                            else
                                Utility.SampleAnimationFadeIn(anim, amKey.amClip.name, amKey.wrapMode, amKey.crossfadeTime, t);
                        }
                        else
                            Utility.SampleAnimation(anim, amKey.amClip.name, amKey.wrapMode, 1.0f, t);
                    }
                    break;
                }
            }
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            // no initial values to set
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            var go = GetTarget(target) as GameObject;
            List<GameObject> ls = new List<GameObject>();
            if(go) ls.Add(go);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            var go = GetTarget(target) as GameObject;
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
                    SetTarget(target, newReferences[i].transform, !string.IsNullOrEmpty(targetPath));
                    break;
                }
            }

            return lsFlagToKeep;
        }

        protected override void DoCopy(Track track) {
            (track as UnityAnimationTrack).obj = obj;
        }
    }
}