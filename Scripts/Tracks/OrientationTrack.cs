using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class OrientationTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Orientation; } }

        public override int order { get { return 1; } }

        [SerializeField]
        Transform obj;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            this.obj = obj as Transform;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO.transform : obj;
        }

        public override string getTrackType() {
            return "Orientation";
        }
        // add a new key
        public void addKey(ITarget itarget, int _frame, Transform target) {
            foreach(OrientationKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.SetTarget(itarget, target);
                    // update cache
                    updateCache(itarget);
                    return;
                }
            }
            OrientationKey a = new OrientationKey();
            a.frame = _frame;
            if(target)
                a.SetTarget(itarget, target);
            // set default ease type to linear
            a.easeType = Ease.Linear;// AMTween.EaseType.linear;
                                          // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            // save rotation
            //Quaternion temp = obj.rotation;

            for(int i = 0; i < keys.Count; i++) {
                OrientationKey key = keys[i] as OrientationKey;

                key.version = version;

                if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
                else {
                    if(i > 0 && !keys[i - 1].canTween)
                        key.interp = Key.Interpolation.None;

                    key.endFrame = -1;
                }
            }
            // restore rotation
            //if(restoreRotation) obj.rotation = temp;
        }

        public Transform getInitialTarget(ITarget itarget) {
            return (keys[0] as OrientationKey).GetTarget(itarget);
        }

        public override void previewFrame(ITarget itarget, float frame, int frameRate, bool play, float playSpeed) {
            Transform t = GetTarget(itarget) as Transform;

            if(keys == null || keys.Count <= 0) return;

            // if before or equal to first frame, or is the only frame
            OrientationKey firstKey = keys[0] as OrientationKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                Transform keyt = firstKey.GetTarget(itarget);
                if(keyt)
                    t.LookAt(keyt);
                return;
            }

            for(int i = 0; i < keys.Count; i++) {
                OrientationKey key = keys[i] as OrientationKey;
                OrientationKey keyNext = i + 1 < keys.Count ? keys[i + 1] as OrientationKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;

                Transform keyt = key.GetTarget(itarget);

                // if no ease
                if(!key.canTween || keyNext == null) {
                    if(keyt)
                        t.LookAt(keyt);
                    return;
                }
                // else easing function

                Transform keyet = keyNext.GetTarget(itarget);

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                t.rotation = key.getQuaternionAtPercent(t, keyt, keyet, framePositionInAction / numFrames);

                return;
            }
        }

        public Transform getStartTargetForFrame(ITarget itarget, float frame) {
            foreach(OrientationKey key in keys) {
                if(/*((int)frame<action.startFrame)||*/((int)frame > key.endFrame)) continue;
                return key.GetTarget(itarget);
            }
            return null;
        }
        public Transform getEndTargetForFrame(ITarget itarget, float frame) {
            if(keys.Count > 1) return (keys[keys.Count - 1] as OrientationKey).GetTarget(itarget);
            return null;
        }
        public Transform getTargetForFrame(ITarget itarget, float frame) {
            if(isFrameBeyondLastKeyFrame(frame)) return getEndTargetForFrame(itarget, frame);
            else return getStartTargetForFrame(itarget, frame);
        }
        // draw gizmos
        public override void drawGizmos(ITarget itarget, float gizmo_size, bool inPlayMode, int frame) {
            if(!obj) return;

            // draw line to target
            bool isLineDrawn = false;
            if(!inPlayMode) {
                for(int i = 0; i < keys.Count; i++) {
                    OrientationKey key = keys[i] as OrientationKey;
                    if(key == null)
                        continue;

                    OrientationKey keyNext = i + 1 < keys.Count ? keys[i + 1] as OrientationKey : null;

                    Transform t = key.GetTarget(itarget);
                    if(t) {
                        //draw target
                        Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 1f);
                        Gizmos.DrawSphere(t.position, 0.2f * (AnimateTimeline.e_gizmoSize / 0.1f));

                        //draw line
                        if(!isLineDrawn) {
                            if(key.frame > frame) isLineDrawn = true;
                            if(frame >= key.frame && frame <= key.endFrame) {
                                if(keyNext == null || t == keyNext.GetTarget(itarget)) {
                                    Gizmos.color = new Color(245f / 255f, 107f / 255f, 30f / 255f, 0.2f);
                                    Gizmos.DrawLine(obj.transform.position, t.position);
                                }
                                isLineDrawn = true;
                            }
                        }
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

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            if(!obj || keys.Count <= 0) return null;
            AnimateTimeline.JSONInit init = new AnimateTimeline.JSONInit();
            init.type = "orientation";
            init.go = obj.gameObject.name;
            Transform _target = getInitialTarget(target);
            int start_frame = keys[0].frame;
            Track _translation_track = null;
            //if(start_frame > 0) _translation_track = parentTake.getTranslationTrackForTransform(_target);
            Vector3 _lookv3 = _target.transform.position;
            if(_translation_track != null) _lookv3 = (_translation_track as TranslationTrack).getPositionAtFrame((_translation_track as TranslationTrack).GetTarget(target) as Transform, start_frame, 0, true);
            AnimateTimeline.JSONVector3 v = new AnimateTimeline.JSONVector3();
            v.setValue(_lookv3);
            init.position = v;
            return init;
        }

        public override List<GameObject> getDependencies(ITarget itarget) {
            Transform tgt = GetTarget(itarget) as Transform;
            List<GameObject> ls = new List<GameObject>();
            if(tgt) ls.Add(tgt.gameObject);
            foreach(OrientationKey key in keys) {
                Transform t = key.GetTarget(itarget);
                if(t) ls.Add(t.gameObject);
            }
            return ls;
        }
        public override List<GameObject> updateDependencies(ITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
            Transform tgt = GetTarget(itarget) as Transform;
            bool didUpdateObj = false;
            for(int i = 0; i < oldReferences.Count; i++) {
                if(!didUpdateObj && tgt && oldReferences[i] == tgt.gameObject) {
                    SetTarget(itarget, newReferences[i].transform);
                    didUpdateObj = true;
                }
                foreach(OrientationKey key in keys) {
                    Transform t = key.GetTarget(itarget);
                    if(t && oldReferences[i] == t.gameObject) {
                        key.SetTarget(itarget, newReferences[i].transform);
                    }
                }
            }

            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            (track as OrientationTrack).obj = obj;
        }
    }
}