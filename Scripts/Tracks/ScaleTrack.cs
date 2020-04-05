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

        public override void SetTarget(ITarget target, Transform item, bool usePath) {
            base.SetTarget(target, item, usePath);
            if(item != null && keys.Count <= 0) cachedInitialScale = item.localScale;
        }

        public override int version { get { return 3; } }

        public override int interpCount { get { return 3; } }

        Vector3 cachedInitialScale;

        public override string getTrackType() {
            return "Local Scale";
        }
        // add a new key
        public void addKey(ITarget target, int _frame, Vector3 _scale) {
            ScaleKey prevKey = null;

            foreach(ScaleKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.scale = _scale;
                    // update cache
                    updateCache(target);
                    return;
                }
                else if(key.frame < _frame)
                    prevKey = key;
            }

            var a = new ScaleKey();
            a.frame = _frame;
            a.scale = _scale;

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
                ScaleKey key = keys[i] as ScaleKey;

                key.GeneratePath(this, i);

                //invalidate some keys in between
                if(key.path != null) {
                    int endInd = i + key.keyCount - 1;
                    if(endInd < keys.Count - 1 || key.interp != keys[endInd].interp) //don't count the last element if there are more keys ahead
                        endInd--;

                    for(int j = i + 1; j <= endInd; j++) {
                        var _key = keys[j] as ScaleKey;

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

            t.localScale = getScaleAtFrame(t, frame, frameRate);
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

            var curKey = (ScaleKey)getKeyOnFrame(frame, false);
            if(curKey == null || curKey.scale != t.localScale) {
                // if updated position, addkey
                addKey(itarget, frame, s);
                return true;
            }

            return false;
        }
        Vector3 GetScale(Transform t, Vector3 toScale) {
            Vector3 s = t.localScale;

            if((axis & AxisFlags.X) != AxisFlags.None)
                s.x = toScale.x;
            if((axis & AxisFlags.Y) != AxisFlags.None)
                s.y = toScale.y;
            if((axis & AxisFlags.Z) != AxisFlags.None)
                s.z = toScale.z;

            return s;
        }
        Vector3 getScaleAtFrame(Transform transform, float frame, int frameRate) {
            int keyCount = keys.Count;

            if(keyCount <= 0) return transform.localScale;

            int iFrame = Mathf.RoundToInt(frame);

            var firstKey = keys[0] as ScaleKey;

            //check if only key or behind first key
            if(keyCount == 1 || iFrame <= firstKey.frame)
                return GetScale(transform, firstKey.scale);

            // if lies on rotation action
            for(int i = 0; i < keyCount; i++) {
                var key = keys[i] as ScaleKey;

                if(key.endFrame == -1) //invalid
                    continue;

                //end of last path in track?
                if(iFrame >= key.endFrame) {
                    if(key.interp == Key.Interpolation.None) {
                        if(i + 1 == keyCount)
                            return GetScale(transform, key.scale);
                    }
                    else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                        if(i + 1 == keyCount - 1)
                            return GetScale(transform, ((ScaleKey)keys[i + 1]).scale);
                    }
                    else if(key.interp == Key.Interpolation.Curve) {
                        if(i + key.keyCount == keyCount) //end of last path in track?
                            return GetScale(transform, ((ScaleKey)keys[i + key.keyCount - 1]).scale);
                    }

                    continue;
                }

                if(key.interp == Key.Interpolation.None)
                    return key.scale;
                else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                    var keyNext = keys[i + 1] as ScaleKey;

                    float numFrames = (float)key.getNumberOfFrames(frameRate);

                    float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                    var start = key.scale;
                    var end = keyNext.scale;

                    if(key.hasCustomEase()) {
                        return GetScale(transform, Vector3.Lerp(start, end, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)));
                    }
                    else {
                        var ease = Utility.GetEasingFunction((Ease)key.easeType);
                        return GetScale(transform, Vector3.Lerp(start, end, ease(framePositionInAction, numFrames, key.amplitude, key.period)));
                    }
                }
                else {
                    float _value = Mathf.Clamp01((frame - key.frame) / key.getNumberOfFrames(frameRate));

                    return GetScale(transform, key.GetScaleFromPath(_value));
                }
            }

            return transform.localScale;
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
                    SetTarget(itarget, newReferences[i].transform, !string.IsNullOrEmpty(targetPath));
                    break;
                }
            }
            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            var ntrack = (ScaleTrack)track;
            ntrack._obj = _obj;
            ntrack.axis = axis;
            ntrack.cachedInitialScale = cachedInitialScale;
        }
    }
}