using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
	[AddComponentMenu("")]
	public class AMTriggerTrack : AMTrack {

	    public override bool canTween { get { return false; } }

	    protected override void SetSerializeObject(UnityEngine.Object obj) { }
	    protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) { return this; }

	    public override string getTrackType() {
	        return "Trigger";
	    }

	    // add a new key
	    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame) {
	        foreach(AMTriggerKey key in keys) {
	            // if key exists on frame, do nothing
	            if(key.frame == _frame) {
	                return;
	            }
	        }
	        AMTriggerKey a = addCall(gameObject, typeof(AMTriggerKey)) as AMTriggerKey;
	        a.frame = _frame;
	        // add a new key
	        keys.Add(a);
	        // update cache
	        updateCache(itarget);
	    }
	    public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
	        // no initial values to set
	        return null;
	    }

	    public override List<GameObject> getDependencies(AMITarget target) {
	        return new List<GameObject>(0);
	    }

	    public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
	        return new List<GameObject>();
	    }

	    protected override void DoCopy(AMTrack track) {
	    }
	}
}