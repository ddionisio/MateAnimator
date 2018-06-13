using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
    [System.Serializable]
    public class TriggerTrack : Track {

        public TriggerSignal signal; //optional signal to call on trigger

        public override SerializeType serializeType { get { return SerializeType.Trigger; } }

        public override bool canTween { get { return false; } }

        protected override void SetSerializeObject(UnityEngine.Object obj) { signal = obj as TriggerSignal; }
        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) { return signal; }

        /// <summary>
        /// This directly sets the target with no path, used for anything that's not a GameObject e.g. ScriptableObject
        /// </summary>
        public void SetSignal(TriggerSignal s) {
            _targetPath = "";
            SetSerializeObject(s);
        }

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
        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
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
            ((TriggerTrack)track).signal = signal;
        }
    }
}