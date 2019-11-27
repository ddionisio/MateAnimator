using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace M8.Animator {
    [System.Serializable]
    public class GOSetActiveTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.GOSetActive; } }

        [SerializeField]
        GameObject obj;

        public bool startActive = true;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            this.obj = obj as GameObject;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO : obj;
        }

        public override string getTrackType() {
            return "GOSetActive";
        }
        // update cache
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            // add all clips to list
            for(int i = 0; i < keys.Count; i++) {
                GOSetActiveKey key = keys[i] as GOSetActiveKey;

                key.version = version;

                if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
                else key.endFrame = -1;
            }
        }
        // preview a frame in the scene view
        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            GameObject go = GetTarget(target) as GameObject;

            if(keys == null || keys.Count <= 0) {
                return;
            }
            if(!go) return;

            // if before the first frame
            if(frame < (float)keys[0].frame) {
                //go.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
                go.SetActive(startActive);
                return;
            }
            // if beyond or equal to last frame
            if(frame >= (float)(keys[keys.Count - 1] as GOSetActiveKey).frame) {
                go.SetActive((keys[keys.Count - 1] as GOSetActiveKey).setActive);
                return;
            }
            // if lies on property action
            foreach(GOSetActiveKey key in keys) {
                if((frame < (float)key.frame) || (key.endFrame != -1 && frame >= (float)key.endFrame)) continue;

                go.SetActive(key.setActive);
                return;
            }
        }

        public override void buildSequenceStart(SequenceControl seq) {
            //need to add activate game object on start to 'reset' properly during reverse
            if(keys.Count > 0 && keys[0].frame > 0) {
                GameObject go = GetTarget(seq.target) as GameObject;
                var tween = DG.Tweening.DOTween.To(new TweenPlugValueSet<bool>(), () => go.activeSelf, (x) => go.SetActive(x), startActive, keys[0].getWaitTime(seq.take.frameRate, 0.0f));
                seq.Insert(0f, tween);
            }
        }

        // add a new key
        public void addKey(ITarget target, int _frame) {
            foreach(GOSetActiveKey key in keys) {
                // if key exists on frame, update
                if(key.frame == _frame) {
                    key.setActive = true;
                    updateCache(target);
                    return;
                }
            }
            GOSetActiveKey a = new GOSetActiveKey();
            a.frame = _frame;
            a.setActive = true;
            // add a new key
            keys.Add(a);
            // update cache
            updateCache(target);
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            // no initial values to set
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            GameObject go = GetTarget(target) as GameObject;
            List<GameObject> ls = new List<GameObject>();
            if(go) ls.Add(go);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            GameObject go = GetTarget(target) as GameObject;
            bool didUpdateObj = false;
            if(go) {
                for(int i = 0; i < oldReferences.Count; i++) {
                    if(oldReferences[i] == go) {
                        SetTarget(target, newReferences[i].transform);
                        didUpdateObj = true;
                        break;
                    }

                }
            }
            if(didUpdateObj) updateCache(target);

            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            var ntrack = track as GOSetActiveTrack;
            ntrack.obj = obj;
            ntrack.startActive = startActive;
        }
    }
}