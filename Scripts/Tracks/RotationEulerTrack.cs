using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class RotationEulerTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.RotationEuler; } }

        public enum Axis {
            X = 1, //only do rotation on X
            Y = 2, //only do rotation on Y
            Z = 4, //only do rotation on Z
            All = 7 //do all rotation
        }

        public Axis axis = Axis.All;

        [SerializeField]
        private Transform _obj;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            _obj = obj as Transform;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO.transform : _obj;
        }

        public new void SetTarget(ITarget target, Transform item) {
            base.SetTarget(target, item);
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
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
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

                float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                Vector3 qStart = key.rotation;
                Vector3 qEnd = keyNext.rotation;

                if(key.hasCustomEase()) {
                    ApplyRot(t, Vector3.Lerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)));
                }
                else {
                    var ease = AMUtil.GetEasingFunction((Ease)key.easeType);
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
            switch(axis) {
                case Axis.X:
                    rot.x = toRot.x;
                    t.localEulerAngles = rot;
                    break;
                case Axis.Y:
                    rot.y = toRot.y;
                    t.localEulerAngles = rot;
                    break;
                case Axis.Z:
                    rot.z = toRot.z;
                    t.localEulerAngles = rot;
                    break;
                case Axis.All:
                    t.localEulerAngles = toRot;
                    break;
            }
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
                    return Vector3.Lerp(qStart, qEnd, AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve));
                }
                else {
                    var ease = AMUtil.GetEasingFunction(key.easeType);
                    return Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period));
                }
            }

            Debug.LogError("Animator: Could not get rotation at frame '" + frame + "'");
            return Vector3.zero;
        }
        public Vector3 getInitialRotation() {
            return (keys[0] as RotationEulerKey).rotation;
        }

        public override AnimatorTimeline.JSONInit getJSONInit(ITarget target) {
            if(!_obj || keys.Count <= 0) return null;
            AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
            init.type = "rotation";
            init.go = _obj.gameObject.name;
            AnimatorTimeline.JSONQuaternion q = new AnimatorTimeline.JSONQuaternion();
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
                    SetTarget(itarget, newReferences[i].transform);
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