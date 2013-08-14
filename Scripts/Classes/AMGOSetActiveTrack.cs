using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Holoville.HOTween;

[AddComponentMenu("")]
public class AMGOSetActiveTrack : AMTrack {
    [SerializeField]
    protected GameObject obj;

    public bool startActive = true;

    public override UnityEngine.Object genericObj {
        get { return obj; }
    }

    public override string getTrackType() {
        return "GOSetActive";
    }
    // update cache
    public override void updateCache() {
        // destroy cache
        destroyCache();
        // create new cache
        cache = new List<AMAction>();
        // sort keys
        sortKeys();
        // add all clips to list
        for(int i = 0; i < keys.Count; i++) {
            AMGOSetActiveAction a = gameObject.AddComponent<AMGOSetActiveAction>();

            a.startFrame = keys[i].frame;
            if(keys.Count > (i + 1)) a.endFrame = keys[i + 1].frame;
            else a.endFrame = -1;

            a.enabled = false;
            a.endVal = (keys[i] as AMGOSetActiveKey).setActive;
            a.go = obj;
            cache.Add(a);
        }
        base.updateCache();
    }
    public void setObject(GameObject obj) {
        if(this.obj != obj && obj != null)
            this.startActive = obj.activeSelf;

        this.obj = obj;
    }
    public bool isObjectUnique(GameObject obj) {
        if(this.obj != obj) return true;
        return false;
    }
    // preview a frame in the scene view
    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        if(cache == null || cache.Count <= 0) {
            return;
        }
        if(!obj) return;

        // if before the first frame
        if(frame < (float)cache[0].startFrame) {
            //obj.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
            obj.SetActive(startActive);
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(cache[cache.Count - 1] as AMGOSetActiveAction).startFrame) {
            obj.SetActive((cache[cache.Count - 1] as AMGOSetActiveAction).endVal);
            return;
        }
        // if lies on property action
        foreach(AMGOSetActiveAction action in cache) {
            if((frame < (float)action.startFrame) || (frame > (float)action.endFrame)) continue;

            obj.SetActive(action.endVal);
            return;
        }
    }

    public override void buildSequenceStart(Sequence s, int frameRate) {
        //need to add activate game object on start to 'reset' properly during reverse
        if(cache.Count > 0 && cache[0].startFrame > 0) {
            s.Insert(0.0f, HOTween.To(obj, ((float)cache[0].startFrame)/((float)frameRate),
                new TweenParms().Prop("active", new AMPlugGOActive(startActive)).Ease(EaseType.Linear)));
        }
    }

    // add a new key
    public void addKey(int _frame) {
        foreach(AMGOSetActiveKey key in keys) {
            // if key exists on frame, update
            if(key.frame == _frame) {
                key.setActive = true;
                updateCache();
                return;
            }
        }
        AMGOSetActiveKey a = gameObject.AddComponent<AMGOSetActiveKey>();
        a.frame = _frame;
        a.setActive = true;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
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
        bool didUpdateObj = false;
        if(obj) {
            for(int i = 0; i < oldReferences.Count; i++) {
                if(oldReferences[i] == obj) {
                    obj = newReferences[i];
                    didUpdateObj = true;
                    break;
                }

            }
        }
        if(didUpdateObj) updateCache();

        return new List<GameObject>();
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMGOSetActiveTrack ntrack = newTake.gameObject.AddComponent<AMGOSetActiveTrack>();
        ntrack.enabled = false;

        ntrack.obj = obj;
        ntrack.startActive = startActive;

        return ntrack;
    }
}
