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

    public override UnityEngine.Object target {
        get { return obj; }
    }

    public override string getTrackType() {
        return "GOSetActive";
    }
    // update cache
    public override void updateCache() {
		base.updateCache();

        // add all clips to list
        for(int i = 0; i < keys.Count; i++) {
            AMGOSetActiveKey key = keys[i] as AMGOSetActiveKey;

            key.version = version;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else key.endFrame = -1;
        }
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
        if(keys == null || keys.Count <= 0) {
            return;
        }
        if(!obj) return;

        // if before the first frame
        if(frame < (float)keys[0].frame) {
            //obj.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
            obj.SetActive(startActive);
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(keys[keys.Count - 1] as AMGOSetActiveKey).frame) {
            obj.SetActive((keys[keys.Count - 1] as AMGOSetActiveKey).setActive);
            return;
        }
        // if lies on property action
        foreach(AMGOSetActiveKey key in keys) {
			if((frame < (float)key.frame) || (key.endFrame != -1 && frame >= (float)key.endFrame)) continue;

            obj.SetActive(key.setActive);
            return;
        }
    }

    public override void buildSequenceStart(Sequence s, int frameRate) {
        //need to add activate game object on start to 'reset' properly during reverse
        if(keys.Count > 0 && keys[0].frame > 0) {
            s.Insert(0.0f, HOTween.To(obj, ((float)keys[0].frame) / ((float)frameRate),
                new TweenParms().Prop("active", new AMPlugGOActive(startActive)).Ease(EaseType.Linear)));
        }
    }

    // add a new key
    public AMKey addKey(int _frame) {
        foreach(AMGOSetActiveKey key in keys) {
            // if key exists on frame, update
            if(key.frame == _frame) {
                key.setActive = true;
                updateCache();
                return null;
            }
        }
        AMGOSetActiveKey a = gameObject.AddComponent<AMGOSetActiveKey>();
        a.frame = _frame;
        a.setActive = true;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
        return a;
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

	protected override AMTrack doDuplicate(GameObject holder) {
        AMGOSetActiveTrack ntrack = holder.AddComponent<AMGOSetActiveTrack>();
        ntrack.enabled = false;
        ntrack.obj = obj;
        ntrack.startActive = startActive;

        return ntrack;
    }
}
