using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("")]
public class AMOrientationTrack : AMTrack {

    public Transform obj;

    public AMTrack cachedTranslationTrackStartTarget = null;
    public AMTrack cachedTranslationTrackEndTarget = null;

    public override string getTrackType() {
        return "Orientation";
    }
    public bool setObject(Transform obj) {
        if(this.obj != obj) {
            this.obj = obj;
            return true;
        }
        return false;
    }
    // add a new key
    public void addKey(int _frame, Transform target) {
        foreach(AMOrientationKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.target = target;
                // update cache
                updateCache();
                return;
            }
        }
        AMOrientationKey a = gameObject.AddComponent<AMOrientationKey>();
        a.enabled = false;
        a.frame = _frame;
        a.target = target;
        // set default ease type to linear
        a.easeType = (int)0;// AMTween.EaseType.linear;
        // add a new key
        keys.Add(a);
        // update cache
        updateCache();
    }
    public override void updateCache() {
        //_updateCache();
        updateOrientationCache(parentTake);
        base.updateCache();
    }

    public void updateOrientationCache(AMTake curTake, bool restoreRotation = false) {
        // save rotation
        Quaternion temp = obj.rotation;
        // sort keys
        sortKeys();
        AMTranslationTrack translationTrack = curTake.getTranslationTrackForTransform(obj);
        for(int i = 0; i < keys.Count; i++) {
            AMOrientationKey key = keys[i] as AMOrientationKey;

            key.version = version;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else key.endFrame = -1;
            key.obj = obj;
            // targets
            if(key.endFrame != -1) key.endTarget = (keys[i + 1] as AMOrientationKey).target;

            if(translationTrack != null && !key.isLookFollow()) {
                key.isSetStartPosition = true;
                key.startPosition = translationTrack.getPositionAtFrame(key.frame, true);
                key.isSetEndPosition = true;
                key.endPosition = translationTrack.getPositionAtFrame(key.endFrame, true);
            }
        }
        // restore rotation
        if(restoreRotation) obj.rotation = temp;
    }

    public Transform getInitialTarget() {
        return (keys[0] as AMOrientationKey).target;
    }

    public override void previewFrame(float frame, AMTrack extraTrack = null) {

        if(keys == null || keys.Count <= 0) {
            return;
        }
        for(int i = 0; i < keys.Count; i++) {
            // before first frame
            if(frame <= (keys[i] as AMOrientationKey).frame) {
                if(!(keys[i] as AMOrientationKey).target) return;
                Vector3 startPos;
                if(cachedTranslationTrackStartTarget == null) startPos = (keys[i] as AMOrientationKey).target.position;
                else startPos = (cachedTranslationTrackStartTarget as AMTranslationTrack).getPositionAtFrame((keys[i] as AMOrientationKey).frame, true);
                obj.LookAt(startPos);
                return;
                // between first and last frame
            }
            else if(frame <= (keys[i] as AMOrientationKey).endFrame) {
                if(!(keys[i] as AMOrientationKey).target || !(keys[i] as AMOrientationKey).endTarget) return;
                float framePositionInPath = frame - (float)keys[i].frame;
                if(framePositionInPath < 0f) framePositionInPath = 0f;
                float percentage = framePositionInPath / keys[i].getNumberOfFrames();
                if((keys[i] as AMOrientationKey).isLookFollow()) obj.rotation = (keys[i] as AMOrientationKey).getQuaternionAtPercent(percentage);
                else {
                    Vector3? startPos = (cachedTranslationTrackStartTarget == null ? null : (Vector3?)(cachedTranslationTrackStartTarget as AMTranslationTrack).getPositionAtFrame((keys[i] as AMOrientationKey).frame, true));
                    Vector3? endPos = (cachedTranslationTrackEndTarget == null ? null : (Vector3?)(cachedTranslationTrackEndTarget as AMTranslationTrack).getPositionAtFrame((keys[i] as AMOrientationKey).endFrame, true));
                    obj.rotation = (keys[i] as AMOrientationKey).getQuaternionAtPercent(percentage, startPos, endPos);
                }
                return;
                // after last frame
            }
            else if(i == keys.Count - 2) {
                if(!(keys[i] as AMOrientationKey).endTarget) return;
                Vector3 endPos;
                if(cachedTranslationTrackEndTarget == null) endPos = (keys[i] as AMOrientationKey).endTarget.position;
                else endPos = (cachedTranslationTrackEndTarget as AMTranslationTrack).getPositionAtFrame((keys[i] as AMOrientationKey).endFrame, true);
                obj.LookAt(endPos);
                return;
            }
        }
    }

    public Transform getStartTargetForFrame(float frame) {
        foreach(AMOrientationKey key in keys) {
            if(/*((int)frame<action.startFrame)||*/((int)frame > key.endFrame)) continue;
            return key.target;
        }
        return null;
    }
    public Transform getEndTargetForFrame(float frame) {
        if(keys.Count > 1) return (keys[keys.Count - 2] as AMOrientationKey).endTarget;
        return null;
    }
    public Transform getTargetForFrame(float frame) {
        if(isFrameBeyondLastKeyFrame(frame)) return getEndTargetForFrame(frame);
        else return getStartTargetForFrame(frame);
    }
    // draw gizmos
    public void drawGizmos(float gizmo_size, bool inPlayMode, int frame) {
        if(!obj) return;
        // draw line to target
        if(!inPlayMode) {
            foreach(AMOrientationKey key in keys) {
                if(key == null)
                    continue;

                if(key.frame > frame) break;
                if(frame >= key.frame && frame <= key.endFrame) {
                    if(key.isLookFollow() && key.target) {
                        Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 0.2f);
                        Gizmos.DrawLine(obj.transform.position, key.target.transform.position);
                    }
                    break;
                }

            }
        }
        // draw arrow
        Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 1f);
        Vector3 pos = obj.transform.position;
        float size = (1.2f * (gizmo_size / 0.1f));
        if(size < 0.1f) size = 0.1f;
        Vector3 direction = obj.forward * size;
        float arrowHeadAngle = 20f;
        float arrowHeadLength = 0.3f * size;

        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public bool isFrameBeyondLastKeyFrame(float frame) {
        if(keys.Count <= 0) return false;
        else if((int)frame > keys[keys.Count - 1].frame) return true;
        else return false;
    }


    public bool hasTarget(Transform obj) {
        foreach(AMOrientationKey key in keys) {
            if(key.target == obj || key.endTarget == obj) return true;
        }
        return false;
    }


    public override AnimatorTimeline.JSONInit getJSONInit() {
        if(!obj || keys.Count <= 0) return null;
        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        init.type = "orientation";
        init.go = obj.gameObject.name;
        Transform _target = getInitialTarget();
        int start_frame = keys[0].frame;
        AMTrack _translation_track = null;
        if(start_frame > 0) _translation_track = parentTake.getTranslationTrackForTransform(_target);
        Vector3 _lookv3 = _target.transform.position;
        if(_translation_track) _lookv3 = (_translation_track as AMTranslationTrack).getPositionAtFrame(start_frame, true);
        AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
        v.setValue(_lookv3);
        init.position = v;
        return init;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(obj) ls.Add(obj.gameObject);
        foreach(AMOrientationKey key in keys) {
            if(key.target) ls.Add(key.target.gameObject);
        }
        return ls;
    }
    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        bool didUpdateObj = false;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(!didUpdateObj && obj && oldReferences[i] == obj.gameObject) {
                obj = newReferences[i].transform;
                didUpdateObj = true;
            }
            foreach(AMOrientationKey key in keys) {
                if(key.target && oldReferences[i] == key.target.gameObject) {
                    key.target = newReferences[i].transform;
                }
            }
        }

        return new List<GameObject>();
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMOrientationTrack ntrack = newTake.gameObject.AddComponent<AMOrientationTrack>();
        ntrack.enabled = false;

        ntrack.obj = obj;

        return ntrack;
    }
}
