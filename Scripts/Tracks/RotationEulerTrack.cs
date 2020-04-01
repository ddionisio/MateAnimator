using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class RotationEulerTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.RotationEuler; } }

        public AxisFlags axis = AxisFlags.All;

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
            if(item != null && keys.Count <= 0) cachedInitialRotation = item.localEulerAngles;
        }

        public override int version { get { return 2; } }

        public override int interpCount { get { return 3; } }

        Vector3 cachedInitialRotation;

        public override string getTrackType() {
            return "Rotation Axis";
        }
        // add a new key
        public void addKey(ITarget target, int _frame, Vector3 _rotation) {
            RotationEulerKey prevKey = null;

            foreach(RotationEulerKey key in keys) {
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

            var a = new RotationEulerKey();
            a.frame = _frame;
            a.rotation = _rotation;

            // copy interpolation and ease type from previous
            if(prevKey != null) {
                a.interp = prevKey.interp;
                a.easeType = prevKey.easeType;
                a.easeCurve = prevKey.easeCurve;
            }
            else { //set default
                a.interp = Key.Interpolation.Curve;
                a.easeType = Ease.Linear;
            }

            // add a new key
            keys.Add(a);
            // update cache
            updateCache(target);
        }

        // update cache (optimized)
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            for(int i = 0; i < keys.Count; i++) {
                RotationEulerKey key = keys[i] as RotationEulerKey;

                key.GeneratePath(this, i);

                //invalidate some keys in between
                if(key.path != null) {
                    int endInd = i + key.keyCount - 1;
                    if(endInd < keys.Count - 1 || key.interp != keys[endInd].interp) //don't count the last element if there are more keys ahead
                        endInd--;

                    for(int j = i + 1; j <= endInd; j++) {
                        var _key = keys[j] as RotationEulerKey;

                        _key.interp = key.interp;
                        _key.easeType = key.easeType;
                        _key.Invalidate();
                    }

                    i = endInd;
                }
            }
        }

        // preview a frame in the scene view
        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            Transform t = GetTarget(target) as Transform;
            if(!t) return;

            t.localEulerAngles = getRotationAtFrame(t, frame, frameRate);
        }
        // returns true if autoKey successful, sets output key
        public bool autoKey(ITarget itarget, Transform aObj, int frame, int frameRate) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t || t != aObj) { return false; }
            Vector3 r = t.localEulerAngles;
            if(keys.Count <= 0) {
                if(r != cachedInitialRotation) {
                    // if updated position, addkey
                    addKey(itarget, frame, r);
                    return true;
                }

                return false;
            }

            var curKey = (RotationEulerKey)getKeyOnFrame(frame, false);
            if(curKey == null || curKey.rotation != t.localEulerAngles) {
                // if updated position, addkey
                addKey(itarget, frame, r);
                return true;
            }

            return false;
        }
        Vector3 GetRotation(Transform t, Vector3 toRot) {
            Vector3 rot = t.localEulerAngles;

            if((axis & AxisFlags.X) != AxisFlags.None)
                rot.x = toRot.x;
            if((axis & AxisFlags.Y) != AxisFlags.None)
                rot.y = toRot.y;
            if((axis & AxisFlags.Z) != AxisFlags.None)
                rot.z = toRot.z;

            return rot;
        }
        Vector3 getRotationAtFrame(Transform transform, float frame, int frameRate) {
            int keyCount = keys.Count;

            if(keyCount <= 0) return transform.localEulerAngles;

            int iFrame = Mathf.RoundToInt(frame);

            var firstKey = keys[0] as RotationEulerKey;

            //check if only key or behind first key
            if(keyCount == 1 || iFrame <= firstKey.frame)
                return GetRotation(transform, firstKey.rotation);

            // if lies on rotation action
            for(int i = 0; i < keyCount; i++) {
                var key = keys[i] as RotationEulerKey;

                if(key.endFrame == -1) //invalid
                    continue;

                //end of last path in track?
                if(iFrame >= key.endFrame) {
                    if(key.interp == Key.Interpolation.None) {
                        if(i + 1 == keyCount)
                            return GetRotation(transform, key.rotation);
                    }
                    else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                        if(i + 1 == keyCount - 1)
                            return GetRotation(transform, ((RotationEulerKey)keys[i + 1]).rotation);
                    }
                    else if(key.interp == Key.Interpolation.Curve) {
                        if(i + key.keyCount == keyCount) //end of last path in track?
                            return GetRotation(transform, ((RotationEulerKey)keys[i + key.keyCount - 1]).rotation);
                    }

                    continue;
                }

                if(key.interp == Key.Interpolation.None)
                    return key.rotation;
                else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                    var keyNext = keys[i + 1] as RotationEulerKey;

                    float numFrames = (float)key.getNumberOfFrames(frameRate);

                    float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                    var start = key.rotation;
                    var end = keyNext.rotation;

                    if(key.hasCustomEase()) {
                        return GetRotation(transform, Vector3.Lerp(start, end, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)));
                    }
                    else {
                        var ease = Utility.GetEasingFunction((Ease)key.easeType);
                        return GetRotation(transform, Vector3.Lerp(start, end, ease(framePositionInAction, numFrames, key.amplitude, key.period)));
                    }
                }
                else {
                    float _value = Mathf.Clamp01((frame - key.frame) / key.getNumberOfFrames(frameRate));

                    return GetRotation(transform, key.GetRotationFromPath(_value));
                }
            }

            return transform.localEulerAngles;
        }
        public Vector3 getInitialRotation() {
            return (keys[0] as RotationEulerKey).rotation;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            if(!_obj || keys.Count <= 0) return null;
            AnimateTimeline.JSONInit init = new AnimateTimeline.JSONInit();
            init.type = "rotation";
            init.go = _obj.gameObject.name;
            AnimateTimeline.JSONQuaternion q = new AnimateTimeline.JSONQuaternion();
            q.setValue(getInitialRotation());
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
            var ntrack = track as RotationEulerTrack;
            ntrack._obj = _obj;
            ntrack.axis = axis;
            ntrack.cachedInitialRotation = cachedInitialRotation;
        }
    }
}