﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class TranslationTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Translation; } }

        public override int order { get { return 1; } } //allow orientation to do its rotation before rotation tracks

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

        public override int version { get { return 3; } }

        public override int interpCount { get { return 3; } }

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

        // add a new key, default interpolation and easeType
        public void addKey(ITarget itarget, int _frame, Vector3 _position) {
            TranslationKey prevKey = null;

            foreach(TranslationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.position = _position;
                    // update cache
                    updateCache(itarget);
                    return;
                }
                else if(key.frame < _frame)
                    prevKey = key;
            }

            TranslationKey a = new TranslationKey();
            a.frame = _frame;
            a.position = _position;

            // copy interpolation and ease type from previous
            if(prevKey != null) {
                a.interp = prevKey.interp;
                a.easeType = prevKey.easeType;
                a.easeCurve = prevKey.easeCurve;
                a.isConstSpeed = prevKey.isConstSpeed;

                a.orientMode = prevKey.orientMode;
                a.orientLockAxis = prevKey.orientLockAxis;
            }

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

            if(keyCount <= 0) return;

            int iFrame = Mathf.RoundToInt(frame);

            TranslationKey firstKey = keys[0] as TranslationKey;

            //check if only key or behind first key
            if(keyCount == 1 || iFrame < firstKey.frame) {
                t.localPosition = convertPosition(t, firstKey.position, false);
                return;
            }
            else if(iFrame == firstKey.frame) {
                if(firstKey.interp == Key.Interpolation.Linear || firstKey.path == null) { //apply orientation
                    t.localPosition = convertPosition(t, firstKey.position, false);

                    if(firstKey.orientMode != OrientMode.None) {
                        var nextPt = ((TranslationKey)keys[1]).position;
                        if(t.parent)
                            nextPt = t.parent.TransformPoint(nextPt);

                        t.rotation = firstKey.GetOrientation(t, nextPt);
                    }
                }
                else if(firstKey.interp == Key.Interpolation.Curve) { //apply orientation
                    t.localPosition = convertPosition(t, firstKey.position, false);

                    if(firstKey.orientMode != OrientMode.None)
                        t.rotation = firstKey.GetOrientation(t, 0f);
                }
                else
                    t.localPosition = convertPosition(t, firstKey.position, false);

                return;
            }

            //check in-between
            for(int i = 0; i < keyCount; i++) {
                TranslationKey key = keys[i] as TranslationKey;

                if(key.endFrame == -1) //invalid
                    continue;

                //end of last path in track?
                if(iFrame >= key.endFrame) {
                    if(key.interp == Key.Interpolation.None) {
                        if(i + 1 == keyCount) {
                            t.localPosition = convertPosition(t, key.position, false);
                            return;
                        }
                    }
                    else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                        if(i + 1 == keyCount - 1) {
                            var pt = ((TranslationKey)keys[i + 1]).position;

                            t.localPosition = convertPosition(t, pt, false);

                            if(key.orientMode != OrientMode.None && iFrame == key.endFrame) {//only apply rotation if we are at end of frame
                                Vector3 ptW, prevPtW;
                                if(t.parent) {
                                    ptW = t.parent.TransformPoint(pt);
                                    prevPtW = t.parent.TransformPoint(key.position);
                                }
                                else {
                                    ptW = pt;
                                    prevPtW = key.position;
                                }
                                var dir = (ptW - prevPtW).normalized;

                                t.rotation = key.GetOrientation(t, ptW + dir);
                            }

                            return;
                        }
                    }
                    else if(key.interp == Key.Interpolation.Curve) {
                        if(i + key.keyCount == keyCount) {//end of last path in track?
                            t.localPosition = convertPosition(t, ((TranslationKey)keys[i + key.keyCount - 1]).position, false);

                            if(key.orientMode != OrientMode.None && iFrame == key.endFrame) //only apply rotation if we are at end of frame
                                t.rotation = key.GetOrientation(t, 1f);

                            return;
                        }
                    }

                    continue;
                }

                if(key.interp == Key.Interpolation.None)
                    t.localPosition = convertPosition(t, key.position, false);
                else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                    var keyNext = keys[i + 1] as TranslationKey;

                    float numFrames = (float)key.getNumberOfFrames(frameRate);

                    float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                    var start = key.position;
                    var end = keyNext.position;

                    if(key.hasCustomEase()) {
                        t.localPosition = convertPosition(t, Vector3.Lerp(start, end, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)), false);
                    }
                    else {
                        var ease = Utility.GetEasingFunction((Ease)key.easeType);
                        t.localPosition = convertPosition(t, Vector3.Lerp(start, end, ease(framePositionInAction, numFrames, key.amplitude, key.period)), false);
                    }

                    if(key.orientMode != OrientMode.None)
                        t.rotation = key.GetOrientation(t, t.parent ? t.parent.TransformPoint(end) : end);
                }
                else {
                    float _value = Mathf.Clamp01((frame - key.frame) / key.getNumberOfFrames(frameRate));

                    var pt = key.GetPoint(_value);
                    t.localPosition = convertPosition(t, pt, false);

                    if(key.orientMode != OrientMode.None)
                        t.rotation = key.GetOrientation(t, _value);
                }

                return;
            }

            //var lpos = getPositionAtFrame(t, frame, frameRate, false);

            //t.localPosition = lpos;
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

            var curKey = (TranslationKey)getKeyOnFrame(frame, false);
            if(curKey == null || curKey.position != GetPosition(t)) {
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
        public Vector3 getPositionAtFrame(Transform t, float frame, int frameRate, bool forceWorld) {
            int keyCount = keys.Count;

            if(keyCount <= 0) return GetPosition(t);

            int iFrame = Mathf.RoundToInt(frame);

            TranslationKey firstKey = keys[0] as TranslationKey;

            //check if only key or behind first key
            if(keyCount == 1 || iFrame <= firstKey.frame)
                return convertPosition(t, firstKey.position, forceWorld);

            //check in-between
            for(int i = 0; i < keyCount; i++) {
                TranslationKey key = keys[i] as TranslationKey;

                if(key.endFrame == -1) //invalid
                    continue;

                //end of last path in track?
                if(iFrame >= key.endFrame) {
                    if(key.interp == Key.Interpolation.None) {
                        if(i + 1 == keyCount)
                            return convertPosition(t, key.position, forceWorld);
                    }
                    else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                        if(i + 1 == keyCount - 1) {
                            var pt = ((TranslationKey)keys[i + 1]).position;
                            return convertPosition(t, pt, forceWorld);
                        }
                    }
                    else if(key.interp == Key.Interpolation.Curve) {
                        if(i + key.keyCount == keyCount) {//end of last path in track?
                            return convertPosition(t, ((TranslationKey)keys[i + key.keyCount - 1]).position, forceWorld);
                        }
                    }

                    continue;
                }

                if(key.interp == Key.Interpolation.None)
                    return convertPosition(t, key.position, forceWorld);
                else if(key.interp == Key.Interpolation.Linear || key.path == null) {
                    var keyNext = keys[i + 1] as TranslationKey;

                    float numFrames = (float)key.getNumberOfFrames(frameRate);

                    float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                    var start = key.position;
                    var end = keyNext.position;

                    if(key.hasCustomEase()) {
                        return convertPosition(t, Vector3.Lerp(start, end, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve)), forceWorld);
                    }
                    else {
                        var ease = Utility.GetEasingFunction((Ease)key.easeType);
                        return convertPosition(t, Vector3.Lerp(start, end, ease(framePositionInAction, numFrames, key.amplitude, key.period)), forceWorld);
                    }
                }
                else {
                    float _value = Mathf.Clamp01((frame - key.frame) / key.getNumberOfFrames(frameRate));

                    var pt = key.GetPoint(_value);

                    return convertPosition(t, pt, forceWorld);
                }
            }

            return GetPosition(t); //last key is impartial tween
        }
        // draw gizmos
        public override void drawGizmos(ITarget target, float gizmo_size, bool inPlayMode, int frame) {
            Transform t = GetTarget(target) as Transform;
            if(!t) return;

            for(int i = 0; i < keys.Count;) {
                var key = (TranslationKey)keys[i];
                var nextKey = i + 1 < keys.Count ? (TranslationKey)keys[i + 1] : null;

                if(key.endFrame != -1)
                    key.DrawGizmos(nextKey, t, gizmo_size);

                i += key.keyCount > 1 ? key.keyCount - 1 : 1;
            }
        }

        // update cache (optimized)
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            // get all paths and add them to the action list
            for(int i = 0; i < keys.Count; i++) {
                TranslationKey key = keys[i] as TranslationKey;

                var interp = key.interp;
                var easeType = key.easeType;
                var isConstSpeed = key.isConstSpeed;
                var orientMode = key.orientMode;
                var lockAxis = key.orientLockAxis;

                key.GeneratePath(this, i);

                //invalidate some keys in between
                if(key.path != null) {
                    int endInd = i + key.keyCount - 1;
                    if(endInd < keys.Count - 1 || key.interp != keys[endInd].interp) //don't count the last element if there are more keys ahead
                        endInd--;

                    for(int j = i + 1; j <= endInd; j++) {
                        var _key = keys[j] as TranslationKey;

                        _key.interp = interp;
                        _key.easeType = easeType;
                        _key.isConstSpeed = isConstSpeed;
                        _key.orientMode = orientMode;
                        _key.orientLockAxis = lockAxis;
                        _key.Invalidate();
                    }

                    i = endInd;
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