using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
    [System.Serializable]
    public class TriggerTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Trigger; } }

        public override bool canTween { get { return false; } }

        protected override void SetSerializeObject(UnityEngine.Object obj) { }
        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) { return null; }

        public override string getTrackType() {
            return "Trigger";
        }

        // add a new key
        public void addKey(ITarget itarget, int _frame) {
            foreach(TriggerKey key in keys) {
                // if key exists on frame, do nothing
                if(key.frame == _frame) {
                    return;
                }
            }
            TriggerKey a = new TriggerKey();
            a.frame = _frame;
            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }
        public override AnimatorTimeline.JSONInit getJSONInit(ITarget target) {
            // no initial values to set
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            return new List<GameObject>(0);
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
        }
    }
}