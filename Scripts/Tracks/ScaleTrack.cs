using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class ScaleTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Scale; } }

        public AxisFlags axis = AxisFlags.All;

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
            if(item != null && keys.Count <= 0) cachedInitialScale = item.localScale;
        }

        public override int version { get { return 1; } }

        Vector3 cachedInitialScale;

        public override string getTrackType() {
            return "Local Scale";
        }
        // add a new key
        public void addKey(ITarget target, int _frame, Vector3 _scale) {
            foreach(ScaleKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.scale = _scale;
                    // update cache
                    updateCache(target);
                    return;
                }
            }
            var a = new ScaleKey();
            a.frame = _frame;
            a.scale = _scale;
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
                var key = keys[i] as ScaleKey;

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
            var firstKey = keys[0] as ScaleKey;
            if(firstKey.endFrame == -1 || (frame <= firstKey.frame && !firstKey.canTween)) {
                ApplyScale(t, firstKey.scale);
                return;
            }

            // if lies on scale action
            for(int i = 0; i < keys.Count; i++) {
                var key = keys[i] as ScaleKey;
                var keyNext = i + 1 < keys.Count ? keys[i + 1] as ScaleKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    ApplyScale(t, key.scale);
                    return;
                }
                // else easing function

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - key.frame, 0f, numFrames);

                Vector3 qStart = key.scale;
                Vector3 qEnd = keyNext.scale;

                if(key.hasCustomEase()) {
                    ApplyScale(t, Vector3.Lerp(qStart, qEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)));
                }
                else {
                    var ease = Utility.GetEasingFunction(key.easeType);
                    ApplyScale(t, Vector3.Lerp(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period)));
                }

                return;
            }
        }
        // returns true if autoKey successful, sets output key
        public bool autoKey(ITarget itarget, Transform aObj, int frame, int frameRate) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t || t != aObj) { return false; }
            Vector3 s = t.localScale;
            if(keys.Count <= 0) {
                if(s != cachedInitialScale) {
                    // if updated position, addkey
                    addKey(itarget, frame, s);
                    return true;
                }

                return false;
            }
            Vector3 oldScale = getScaleAtFrame(frame, frameRate);
            if(s != oldScale) {
                // if updated position, addkey
                addKey(itarget, frame, s);
                return true;
            }

            return false;
        }
        void ApplyScale(Transform t, Vector3 toScale) {
            Vector3 s = t.localScale;

            if((axis & AxisFlags.X) != AxisFlags.None)
                s.x = toScale.x;
            if((axis & AxisFlags.Y) != AxisFlags.None)
                s.y = toScale.y;
            if((axis & AxisFlags.Z) != AxisFlags.None)
                s.z = toScale.z;

            t.localScale = s;
        }
        Vector3 getScaleAtFrame(int frame, int frameRate) {
            // if before or equal to first frame, or is the only frame
            var firstKey = keys[0] as ScaleKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                return firstKey.scale;
            }

            // if lies on scale action
            for(int i = 0; i < keys.Count; i++) {
                var key = keys[i] as ScaleKey;
                var keyNext = i + 1 < keys.Count ? keys[i + 1] as ScaleKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    return key.scale;
                }
                // else easing function

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                var sStart = key.scale;
                var sEnd = keyNext.scale;

                if(key.hasCustomEase()) {
                    return Vector3.Lerp(sStart, sEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve));
                }
                else {
                    var ease = Utility.GetEasingFunction(key.easeType);
                    return Vector3.Lerp(sStart, sEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period));
                }
            }

            Debug.LogError("Animator: Could not get scale at frame '" + frame + "'");
            return Vector3.zero;
        }
        public Vector3 getInitialScale() {
            return ((ScaleKey)keys[0]).scale;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            if(!_obj || keys.Count <= 0) return null;
            var init = new AnimateTimeline.JSONInit();
            init.type = "scale";
            init.go = _obj.gameObject.name;
            var s = new AnimateTimeline.JSONVector3();
            s.setValue(getInitialScale());
            init.scale = s;
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
            var ntrack = (ScaleTrack)track;
            ntrack._obj = _obj;
            ntrack.cachedInitialScale = cachedInitialScale;
        }
    }
}