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

        public override int version { get { return 1; } }

        Vector3 cachedInitialRotation;

        public override string getTrackType() {
            return "Rotation Axis";
        }
        // add a new key
        public void addKey(ITarget target, int _frame, Vector3 _rotation) {
            foreach(RotationEulerKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.rotation = _rotation;
                    // update cache
                    updateCache(target);
                    return;
                }
            }
            var a = new RotationEulerKey();
            a.frame = _frame;
            a.rotation = _rotation;
            // set default ease type to linear
            a.easeType = Ease.Linear;

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

                key.version = version;

                if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
                else {
                    if(i > 0 && !keys[i - 1].canTween)
                        key.interp = Key.Interpolation.None;

                    key.endFrame = -1;
                }
            }
        }
        // preview a frame in the scene view
        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            Transform t = GetTarget(target) as Transform;

            if(!t) return;
            if(keys == null || keys.Count <= 0) return;

            // if before or equal to first frame, or is the only frame
            RotationEulerKey firstKey = keys[0] as RotationEulerKey;
            if(firstKey.endFrame == -1 || (frame <= firstKey.frame && !firstKey.canTween)) {
                ApplyRot(t, firstKey.rotation);
                return;
            }

            // if lies on rotation action
            for(int i = 0; i < keys.Count; i++) {
                RotationEulerKey key = keys[i] as RotationEulerKey;
                RotationEulerKey keyNext = i + 1 < keys.Count ? keys[i + 1] as RotationEulerKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    ApplyRot(t, key.rotation);
                    return;
                }
                // else easing function

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - key.frame, 0f, numFrames);

                Vector3 qStart = key.rotation;
                Vector3 qEnd = keyNext.rotation;

                if(key.hasCustomEase()) {
                    ApplyRot(t, Vector3.Lerp(qStart, qEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)));
                }
                else {
                    var ease = Utility.GetEasingFunction(key.easeType);
                    ApplyRot(t, Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period)));
                }

                return;
            }
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
            Vector3 oldRot = getRotationAtFrame(frame, frameRate);
            if(r != oldRot) {
                // if updated position, addkey
                addKey(itarget, frame, r);
                return true;
            }

            return false;
        }
        void ApplyRot(Transform t, Vector3 toRot) {
            Vector3 rot = t.localEulerAngles;

            if((axis & AxisFlags.X) != AxisFlags.None)
                rot.x = toRot.x;
            if((axis & AxisFlags.Y) != AxisFlags.None)
                rot.y = toRot.y;
            if((axis & AxisFlags.Z) != AxisFlags.None)
                rot.z = toRot.z;

            t.localEulerAngles = rot;
        }
        Vector3 getRotationAtFrame(int frame, int frameRate) {
            // if before or equal to first frame, or is the only frame
            RotationEulerKey firstKey = keys[0] as RotationEulerKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                return firstKey.rotation;
            }

            // if lies on rotation action
            for(int i = 0; i < keys.Count; i++) {
                RotationEulerKey key = keys[i] as RotationEulerKey;
                RotationEulerKey keyNext = i + 1 < keys.Count ? keys[i + 1] as RotationEulerKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    return key.rotation;
                }
                // else easing function

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                Vector3 qStart = key.rotation;
                Vector3 qEnd = keyNext.rotation;

                if(key.hasCustomEase()) {
                    return Vector3.Lerp(qStart, qEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve));
                }
                else {
                    var ease = Utility.GetEasingFunction(key.easeType);
                    return Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period));
                }
            }

            Debug.LogError("Animator: Could not get rotation at frame '" + frame + "'");
            return Vector3.zero;
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
            ntrack.cachedInitialRotation = cachedInitialRotation;
        }
    }
}