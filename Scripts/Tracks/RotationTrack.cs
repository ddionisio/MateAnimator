using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class RotationTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Rotation; } }

        [SerializeField]
        private Transform _obj;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            _obj = obj as Transform;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO.transform : _obj;
        }

        public override void SetTarget(ITarget target, Transform item, bool usePath) {
            base.SetTarget(target, item, usePath);
            if(item != null && keys.Count <= 0) cachedInitialRotation = item.localRotation;
        }

        public override int version { get { return 3; } }

        public override int interpCount { get { return 3; } }

        Quaternion cachedInitialRotation;

        public override string getTrackType() {
            return "Rotation Quaternion";
        }
        // add a new key
        public void addKey(ITarget target, int _frame, Quaternion _rotation) {
            RotationKey prevKey = null;

            foreach(RotationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.rotation = _rotation;
                    // update cache
                    updateCache(target);
                    return;
                }
                else if(key.frame < _frame)
                    prevKey = key;
            }

            RotationKey a = new RotationKey();
            a.frame = _frame;
            a.rotation = _rotation;

            // copy interpolation and ease type from previous
            if(prevKey != null) {
                a.interp = prevKey.interp;
                a.easeType = prevKey.easeType;
                a.easeCurve = prevKey.easeCurve;
            }
            else {
                // set default
                a.interp = Key.Interpolation.Curve;
                a.easeType = Ease.Linear;
            }

            // add a new key
            keys.Add(a);
            // update cache
            updateCache(target);
        }

        public override void undoRedoPerformed() {
            //path preview must be rebuilt
            foreach(RotationKey key in keys)
                key.ClearCache();
        }

        // update cache (optimized)
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            for(int i = 0; i < keys.Count; i++) {
                RotationKey key = keys[i] as RotationKey;

                key.GeneratePath(this, i);
                key.ClearCache();

                //invalidate some keys in between
                if(key.path.Length > 1) {
                    int endInd = i + key.path.Length - 1;
                    if(endInd < keys.Count - 1 || key.interp != keys[endInd].interp) //don't count the last element if there are more keys ahead
                        endInd--;

                    for(int j = i + 1; j <= endInd; j++) {
                        var _key = keys[j] as RotationKey;

                        _key.interp = key.interp;
                        _key.easeType = key.easeType;
                        _key.endFrame = -1;
                        _key.path = new Vector3[0];
                    }

                    i = endInd;
                }
            }
        }
        // preview a frame in the scene view
        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            Transform t = GetTarget(target) as Transform;
            if(!t) return;

            t.localRotation = getRotationAtFrame(t, frame, frameRate);
        }
        // returns true if autoKey successful, sets output key
        public bool autoKey(ITarget itarget, Transform aObj, int frame, int frameRate) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t || t != aObj) { return false; }
            Quaternion r = t.localRotation;
            if(keys.Count <= 0) {
                if(r != cachedInitialRotation) {
                    // if updated position, addkey
                    addKey(itarget, frame, r);
                    return true;
                }

                return false;
            }

            var curKey = (RotationKey)getKeyOnFrame(frame, false);
            if(curKey == null || curKey.rotation != t.localRotation) {
                // if updated position, addkey
                addKey(itarget, frame, r);
                return true;
            }

            return false;
        }
        Quaternion getRotationAtFrame(Transform transform, float frame, int frameRate) {
            int keyCount = keys.Count;

            if(keyCount <= 0) return transform.localRotation;

            int iFrame = Mathf.RoundToInt(frame);

            var firstKey = keys[0] as RotationKey;

            //check if only key or behind first key
            if(keyCount == 1 || iFrame <= firstKey.frame)
                return firstKey.rotation;

            // if lies on rotation action
            for(int i = 0; i < keyCount; i++) {
                RotationKey key = keys[i] as RotationKey;

                if(key.endFrame == -1) //invalid
                    continue;

                //end of last path in track?
                if(iFrame >= key.endFrame) {
                    switch(key.interp) {
                        case Key.Interpolation.Linear:
                            if(i + 1 == keyCount - 1)
                                return ((RotationKey)keys[i + 1]).rotation;
                            break;
                        case Key.Interpolation.Curve:
                            if(key.path.Length > 0 && i + key.path.Length == keyCount) //end of last path in track?
                                return ((RotationKey)keys[key.path.Length - 1]).rotation;
                            break;
                        case Key.Interpolation.None:
                            if(i + 1 == keyCount)
                                return key.rotation;
                            break;
                    }

                    continue;
                }

                switch(key.interp) {
                    case Key.Interpolation.None:
                        return key.rotation;

                    case Key.Interpolation.Linear:
                        RotationKey keyNext = keys[i + 1] as RotationKey;

                        float numFrames = (float)key.getNumberOfFrames(frameRate);

                        float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                        Quaternion qStart = key.rotation;
                        Quaternion qEnd = keyNext.rotation;

                        if(key.hasCustomEase()) {
                            return Quaternion.LerpUnclamped(qStart, qEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve));
                        }
                        else {
                            var ease = Utility.GetEasingFunction((Ease)key.easeType);
                            return Quaternion.LerpUnclamped(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period));
                        }

                    case Key.Interpolation.Curve:
                        if(key.path.Length <= 1) //invalid key
                            return transform.localRotation;

                        float _value = Mathf.Clamp01((frame - key.frame) / key.getNumberOfFrames(frameRate));

                        return key.GetRotationFromPath(transform, frameRate, Mathf.Clamp01(_value));
                }
            }

            return transform.localRotation;
        }
        public Quaternion getInitialRotation() {
            return (keys[0] as RotationKey).rotation;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            if(!_obj || keys.Count <= 0) return null;
            AnimateTimeline.JSONInit init = new AnimateTimeline.JSONInit();
            init.type = "rotation";
            init.go = _obj.gameObject.name;
            AnimateTimeline.JSONQuaternion q = new AnimateTimeline.JSONQuaternion();
            Quaternion quat = getInitialRotation();
            q.setValue(new Vector4(quat.x, quat.y, quat.z, quat.w));
            init.rotation = q;
            return init;
        }

        public override List<GameObject> getDependencies(ITarget itarget) {
            Transform t = GetTarget(itarget) as Transform;
            List<GameObject> ls = new List<GameObject>();
            if(t) ls.Add(t.gameObject);
            return ls;
        }
        public override List<GameObject> updateDependencies(ITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t) return new List<GameObject>();
            for(int i = 0; i < oldReferences.Count; i++) {
                if(oldReferences[i] == t.gameObject) {
                    SetTarget(itarget, newReferences[i].transform, !string.IsNullOrEmpty(targetPath));
                    break;
                }
            }
            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            var ntrack = track as RotationTrack;
            ntrack._obj = _obj;
            ntrack.cachedInitialRotation = cachedInitialRotation;
        }
    }
}