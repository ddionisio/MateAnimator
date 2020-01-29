using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class TranslationTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Translation; } }

        [SerializeField]
        private Transform _obj;

        public bool pixelSnap;

        public float pixelPerUnit;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            _obj = obj as Transform;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO.transform : _obj;
        }

        public override void SetTarget(ITarget target, Transform item, bool usePath) {
            base.SetTarget(target, item, usePath);
            if(item != null && keys.Count <= 0) cachedInitialPosition = item.localPosition;
        }

        public override int version { get { return 2; } }

        public override bool hasTrackSettings { get { return true; } }

        public override int interpCount { get { return 3; } }

        void SetPosition(Transform t, Vector3 p) {
            if(t) {
                if(pixelSnap) p.Set(Mathf.Round(p.x * pixelPerUnit) / pixelPerUnit, Mathf.Round(p.y * pixelPerUnit) / pixelPerUnit, Mathf.Round(p.z * pixelPerUnit) / pixelPerUnit);
                t.localPosition = p;
            }
        }

        Vector3 GetPosition(Transform t) {
            if(t) {
                return t.localPosition;
            }
            return Vector3.zero;
        }

        private Vector3 cachedInitialPosition;

        public override string getTrackType() {
            return "Position";
        }

        // add a new key
        public void addKey(ITarget itarget, int _frame, Vector3 _position, Key.Interpolation _interp, Ease _easeType) {
            foreach(TranslationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.position = _position;
                    key.interp = _interp;
                    key.easeType = _easeType;
                    // update cache
                    updateCache(itarget);
                    return;
                }
            }
            TranslationKey a = new TranslationKey();
            a.frame = _frame;
            a.position = _position;
            a.interp = _interp;
            a.easeType = _easeType;

            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }
        // add a new key, default interpolation and easeType
        public void addKey(ITarget itarget, int _frame, Vector3 _position) {
            foreach(TranslationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.position = _position;
                    // update cache
                    updateCache(itarget);
                    return;
                }
            }
            TranslationKey a = new TranslationKey();
            a.frame = _frame;
            a.position = _position;

            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }

        // preview a frame in the scene view
        public override void previewFrame(ITarget itarget, float frame, int frameRate, bool play, float playSpeed) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t) return;

            int keyCount = keys.Count;

            if(keys == null || keyCount <= 0) return;

            int iFrame = Mathf.RoundToInt(frame);

            TranslationKey firstKey = keys[0] as TranslationKey;

            //check if behind first key
            if(iFrame <= firstKey.frame && (!firstKey.canTween || firstKey.path.Length == 1)) {
                SetPosition(t, firstKey.position);
                return;
            }

            TranslationKey lastKey = keyCount == 1 ? firstKey : keys[keyCount - 1] as TranslationKey;

            //check if past last key
            if(iFrame >= lastKey.endFrame) {
                SetPosition(t, lastKey.position);
                return;
            }

            //check in-between
            for(int i = 0; i < keyCount; i++) {
                TranslationKey key = keys[i] as TranslationKey;

                if(key.path == null)
                    continue;
                                
                if(iFrame >= key.endFrame && i < keyCount - 1) continue;

                if((!key.canTween || key.path.Length <= 1)) {
                    SetPosition(t, key.position);
                    return;
                }

                float fNumFrames = (float)key.getNumberOfFrames(frameRate);

                float _value;

                float framePositionInPath = Mathf.Clamp(frame - (float)key.frame, 0f, fNumFrames);

                if(key.hasCustomEase())
                    _value = Utility.EaseCustom(0.0f, 1.0f, framePositionInPath / fNumFrames, key.easeCurve);
                else {
                    var ease = Utility.GetEasingFunction((Ease)key.easeType);
                    _value = ease(framePositionInPath, fNumFrames, key.amplitude, key.period);
                    if(float.IsNaN(_value)) //this really shouldn't happen...
                        return;
                }

                SetPosition(t, key.GetPoint(Mathf.Clamp01(_value)));

                return;
            }
        }

        // returns true if autoKey successful
        public bool autoKey(ITarget itarget, Transform aobj, int frame, int frameRate) {
            Transform t = GetTarget(itarget) as Transform;
            if(!t || aobj != t) { return false; }

            if(keys.Count <= 0) {
                if(GetPosition(t) != cachedInitialPosition) {
                    // if updated position, addkey
                    addKey(itarget, frame, GetPosition(t));
                    return true;
                }
                return false;
            }

            Vector3 oldPos = getPositionAtFrame(t, frame, frameRate, false);
            if(GetPosition(t) != oldPos) {
                // if updated position, addkey
                addKey(itarget, frame, GetPosition(t));
                return true;
            }

            return false;
        }
        private Vector3 convertPosition(Transform t, Vector3 ret, bool forceWorld) {
            if(pixelSnap) ret.Set(Mathf.Round(ret.x * pixelPerUnit) / pixelPerUnit, Mathf.Round(ret.y * pixelPerUnit) / pixelPerUnit, Mathf.Round(ret.z * pixelPerUnit) / pixelPerUnit);

            if(forceWorld && t != null && t.parent != null)
                ret = t.parent.localToWorldMatrix.MultiplyPoint(ret);

            return ret;
        }
        public Vector3 getPositionAtFrame(Transform t, int frame, int frameRate, bool forceWorld) {
            int keyCount = keys.Count;

            if(keyCount <= 0) return GetPosition(t);

            TranslationKey firstKey = keys[0] as TranslationKey;

            //check if behind first key
            if(frame <= firstKey.frame && (!firstKey.canTween || firstKey.path.Length == 1)) {
                return convertPosition(t, firstKey.position, forceWorld);
            }

            TranslationKey lastKey = keyCount == 1 ? firstKey : keys[keyCount - 1] as TranslationKey;

            //check if past last key
            if(frame >= lastKey.endFrame && !lastKey.canTween) {
                return convertPosition(t, lastKey.position, forceWorld);
            }

            //check in-between
            for(int i = 0; i < keyCount; i++) {
                TranslationKey key = keys[i] as TranslationKey;
                TranslationKey keyNext = i < keyCount - 1 ? keys[i + 1] as TranslationKey : null;

                if(frame >= key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.path.Length > 1)) continue;

                if(!key.canTween || key.path.Length == 1) {
                    return convertPosition(t, key.position, forceWorld);
                }
                else if(key.path.Length == 0)
                    continue;

                float fNumFrames = (float)key.getNumberOfFrames(frameRate);

                float _value;

                float framePositionInPath = Mathf.Clamp(frame - (float)key.frame, 0f, fNumFrames);

                if(key.hasCustomEase())
                    _value = Utility.EaseCustom(0.0f, 1.0f, framePositionInPath / fNumFrames, key.easeCurve);
                else {
                    var ease = Utility.GetEasingFunction((Ease)key.easeType);
                    _value = ease(framePositionInPath, fNumFrames, key.amplitude, key.period);
                    if(float.IsNaN(_value)) //this really shouldn't happen...
                        break;
                }

                return convertPosition(t, key.GetPoint(Mathf.Clamp01(_value)), forceWorld);
            }

            Debug.LogError("Animator: Could not get " + t.name + " position at frame '" + frame + "'");
            return GetPosition(t);
        }
        // draw gizmos
        public override void drawGizmos(ITarget target, float gizmo_size, bool inPlayMode, int frame) {
            Transform t = GetTarget(target) as Transform;
            if(!t) return;

            foreach(TranslationKey key in keys) {
                if(key != null) {
                    if(!key.canTween) {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(t.parent ? t.parent.localToWorldMatrix.MultiplyPoint3x4(key.position) : key.position, gizmo_size);
                    }
                    else if(key.path.Length > 1)
                        key.pathPreview.GizmoDraw(t.parent, gizmo_size);
                }
            }
        }

        public override void undoRedoPerformed() {
            //path preview must be rebuilt
            foreach(TranslationKey key in keys)
                key.pathPreview = null;
        }

        // update cache (optimized)
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            // get all paths and add them to the action list
            for(int i = 0; i < keys.Count; i++) {
                TranslationKey key = keys[i] as TranslationKey;

                var interp = key.interp;
                var easeType = key.easeType;

                key.version = version;

                PathData path;
                switch(key.interp) {
                    case Key.Interpolation.Curve:
                        path = PathData.GenerateCurve(keys, i);
                        break;
                    case Key.Interpolation.Linear:
                        path = PathData.GenerateLinear(keys, i);
                        break;
                    default:
                        int singleEndFrame = i < keys.Count - 1 ? keys[i + 1].frame : keys[i].frame;
                        path = PathData.GenerateSingle(keys[i], i, singleEndFrame);
                        break;
                }

                key.endFrame = path.endFrame;
                key.pathPreview = null;

                if(!key.canTween) {
                    if(path.endIndex == keys.Count - 1) {
                        TranslationKey lastKey = keys[path.endIndex] as TranslationKey;
                        lastKey.interp = Key.Interpolation.None;
                        lastKey.endFrame = lastKey.frame;
                        lastKey.path = new Vector3[0];
                    }
                }
                else {
                    key.path = path.path;
                }

                //invalidate some keys in between
                if(path.startIndex < keys.Count - 1) {
                    int _endInd = path.endIndex;
                    if(_endInd < keys.Count - 1)
                        _endInd--;

                    if(i < _endInd) {
                        for(i = path.startIndex + 1; i <= _endInd; i++) {
                            key = keys[i] as TranslationKey;

                            key.version = version;
                            key.interp = interp;
                            key.easeType = easeType;
                            key.endFrame = key.frame;
                            key.path = new Vector3[0];
                        }

                        i = _endInd;
                    }
                }
            }
        }
        // get the starting translation key for the action where the frame lies
        public TranslationKey getKeyStartFor(int frame) {
            foreach(TranslationKey key in keys) {
                if((frame < key.frame) || (frame >= key.endFrame)) continue;
                return (TranslationKey)getKeyOnFrame(key.frame);
            }
            Debug.LogError("Animator: Action for frame " + frame + " does not exist in cache.");
            return null;
        }

        public Vector3 getInitialPosition() {
            return (keys[0] as TranslationKey).position;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            if(!_obj || keys.Count <= 0) return null;
            AnimateTimeline.JSONInit init = new AnimateTimeline.JSONInit();
            init.type = "position";
            init.go = _obj.gameObject.name;
            AnimateTimeline.JSONVector3 v = new AnimateTimeline.JSONVector3();
            v.setValue(getInitialPosition());
            init.position = v;
            return init;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            Transform t = GetTarget(target) as Transform;
            List<GameObject> ls = new List<GameObject>();
            if(t) ls.Add(t.gameObject);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            Transform t = GetTarget(target) as Transform;
            if(!t) return new List<GameObject>();
            for(int i = 0; i < oldReferences.Count; i++) {
                if(oldReferences[i] == t.gameObject) {
                    SetTarget(target, newReferences[i].transform, !string.IsNullOrEmpty(targetPath));
                    break;
                }
            }
            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            TranslationTrack ntrack = track as TranslationTrack;
            ntrack._obj = _obj;
            ntrack.cachedInitialPosition = cachedInitialPosition;
            ntrack.pixelSnap = pixelSnap;
            ntrack.pixelPerUnit = pixelPerUnit;
        }
    }
}