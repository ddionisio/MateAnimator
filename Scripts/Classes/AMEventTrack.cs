using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("")]
public class AMEventTrack : AMTrack {

    public GameObject obj;

    public override string getTrackType() {
        return "Event";
    }
    // update cache
    public override void updateCache() {
        // sort keys
        sortKeys();
        // add all clips to list
        for(int i = 0; i < keys.Count; i++) {
            AMEventKey key = keys[i] as AMEventKey;
            key.version = version;
        }
        base.updateCache();
    }
    public void setObject(GameObject obj) {
        this.obj = obj;
    }
    public bool isObjectUnique(GameObject obj) {
        if(this.obj != obj) return true;
        return false;
    }
    // add a new key
    public AMKey addKey(int _frame) {
        foreach(AMEventKey key in keys) {
            // if key exists on frame, do nothing
            if(key.frame == _frame) {
                return null;
            }
        }
        AMEventKey a = gameObject.AddComponent<AMEventKey>();
        a.frame = _frame;
        a.component = null;
        a.methodName = null;
        a.parameters = null;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
        return a;
    }
    public bool hasSameEventsAs(AMEventTrack _track) {
        if(_track.obj == obj)
            return true;
        return false;
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        // no initial values to set
        return null;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(obj) ls.Add(obj);
        foreach(AMEventKey key in keys) {
            ls = ls.Union(key.getDependencies()).ToList();
        }
        return ls;
    }

    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        bool didUpdateObj = false;
        bool didUpdateParameter = false;
        if(obj) {
            for(int i = 0; i < oldReferences.Count; i++) {
                if(oldReferences[i] == obj) {
                    // check if new GameObject has all the required components
                    foreach(AMEventKey key in keys) {
                        string componentName = key.component.GetType().Name;
                        if(key.component && newReferences[i].GetComponent(componentName) == null) {
                            // missing component
                            Debug.LogWarning("Animator: Event Track component '" + componentName + "' not found on new reference for GameObject '" + obj.name + "'. Duplicate not replaced.");
                            List<GameObject> lsFlagToKeep = new List<GameObject>();
                            lsFlagToKeep.Add(oldReferences[i]);
                            return lsFlagToKeep;
                        }
                    }
                    obj = newReferences[i];
                    didUpdateObj = true;
                    break;
                }

            }
        }
        foreach(AMEventKey key in keys) {
            if(key.updateDependencies(newReferences, oldReferences, didUpdateObj, obj) && !didUpdateParameter) didUpdateParameter = true;
        }

        if(didUpdateObj || didUpdateParameter) updateCache();

        return new List<GameObject>();
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMEventTrack ntrack = newTake.gameObject.AddComponent<AMEventTrack>();
        ntrack.enabled = false;
        ntrack.obj = obj;

        return ntrack;
    }
}
