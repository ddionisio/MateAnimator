using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
	[AddComponentMenu("")]
	public class AMOrientationTrack : AMTrack {

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
	    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, Transform target) {
	        foreach(AMOrientationKey key in keys) {
	            // if key exists on frame, update key
	            if(key.frame == _frame) {
					key.SetTarget(itarget, target);
	                // update cache
					updateCache(itarget);
	                return;
	            }
	        }
	        AMOrientationKey a = addCall(gameObject, typeof(AMOrientationKey)) as AMOrientationKey;
	        a.frame = _frame;
			a.SetTarget(itarget, target);
	        // set default ease type to linear
	        a.easeType = (int)Ease.Linear;// AMTween.EaseType.linear;
	        // add a new key
	        keys.Add(a);
	        // update cache
			updateCache(itarget);
	    }
		public override void updateCache(AMITarget target) {
			base.updateCache(target);

			// save rotation
			//Quaternion temp = obj.rotation;

			for(int i = 0; i < keys.Count; i++) {
				AMOrientationKey key = keys[i] as AMOrientationKey;
				
				key.version = version;
				
				if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
				else {
					if(i > 0 && !keys[i-1].canTween)
						key.interp = (int)AMKey.Interpolation.None;

					key.endFrame = -1;
				}
			}
			// restore rotation
			//if(restoreRotation) obj.rotation = temp;
	    }

	    public Transform getInitialTarget(AMITarget itarget) {
			return (keys[0] as AMOrientationKey).GetTarget(itarget);
	    }

	    public override void previewFrame(AMITarget itarget, float frame, int frameRate, bool play, float playSpeed) {
			Transform t = GetTarget(itarget) as Transform;

	        if(keys == null || keys.Count <= 0) return;

            // if before or equal to first frame, or is the only frame
            AMOrientationKey firstKey = keys[0] as AMOrientationKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                Transform keyt = firstKey.GetTarget(itarget);
                if(keyt)
                    t.LookAt(keyt);
                return;
            }

	        for(int i = 0; i < keys.Count; i++) {
				AMOrientationKey key = keys[i] as AMOrientationKey;
                AMOrientationKey keyNext = i + 1 < keys.Count ? keys[i+1] as AMOrientationKey : null;

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

		public Transform getStartTargetForFrame(AMITarget itarget, float frame) {
	        foreach(AMOrientationKey key in keys) {
	            if(/*((int)frame<action.startFrame)||*/((int)frame > key.endFrame)) continue;
	            return key.GetTarget(itarget);
	        }
	        return null;
	    }
		public Transform getEndTargetForFrame(AMITarget itarget, float frame) {
			if(keys.Count > 1) return (keys[keys.Count - 1] as AMOrientationKey).GetTarget(itarget);
	        return null;
	    }
		public Transform getTargetForFrame(AMITarget itarget, float frame) {
			if(isFrameBeyondLastKeyFrame(frame)) return getEndTargetForFrame(itarget, frame);
			else return getStartTargetForFrame(itarget, frame);
	    }
	    // draw gizmos
	    public override void drawGizmos(AMITarget itarget, float gizmo_size, bool inPlayMode, int frame) {
	        if(!obj) return;

	        // draw line to target
	        bool isLineDrawn = false;
	        if(!inPlayMode) {
	            for(int i = 0; i < keys.Count; i++) {
	                AMOrientationKey key = keys[i] as AMOrientationKey;
	                if(key == null)
	                    continue;

	                AMOrientationKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMOrientationKey : null;

	                Transform t = key.GetTarget(itarget);
	                if(t) {
	                    //draw target
	                    Gizmos.color = new Color(245f/255f, 107f/255f, 30f/255f, 1f);
	                    Gizmos.DrawSphere(t.position, 0.2f * (AnimatorTimeline.e_gizmoSize/0.1f));

	                    //draw line
	                    if(!isLineDrawn) {
	                        if(key.frame > frame) isLineDrawn = true;
	                        if(frame >= key.frame && frame <= key.endFrame) {
	                            if(!keyNext || t == keyNext.GetTarget(itarget)) {
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

		public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
	        if(!obj || keys.Count <= 0) return null;
	        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
	        init.type = "orientation";
	        init.go = obj.gameObject.name;
			Transform _target = getInitialTarget(target);
	        int start_frame = keys[0].frame;
	        AMTrack _translation_track = null;
	        //if(start_frame > 0) _translation_track = parentTake.getTranslationTrackForTransform(_target);
	        Vector3 _lookv3 = _target.transform.position;
			if(_translation_track) _lookv3 = (_translation_track as AMTranslationTrack).getPositionAtFrame((_translation_track as AMTranslationTrack).GetTarget(target) as Transform, start_frame, 0, true);
	        AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
	        v.setValue(_lookv3);
	        init.position = v;
	        return init;
	    }

		public override List<GameObject> getDependencies(AMITarget itarget) {
			Transform tgt = GetTarget(itarget) as Transform;
	        List<GameObject> ls = new List<GameObject>();
			if(tgt) ls.Add(tgt.gameObject);
	        foreach(AMOrientationKey key in keys) {
				Transform t = key.GetTarget(itarget);
	            if(t) ls.Add(t.gameObject);
	        }
	        return ls;
	    }
		public override List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
			Transform tgt = GetTarget(itarget) as Transform;
	        bool didUpdateObj = false;
	        for(int i = 0; i < oldReferences.Count; i++) {
				if(!didUpdateObj && tgt && oldReferences[i] == tgt.gameObject) {
					SetTarget(itarget, newReferences[i].transform);
	                didUpdateObj = true;
	            }
	            foreach(AMOrientationKey key in keys) {
					Transform t = key.GetTarget(itarget);
	                if(t && oldReferences[i] == t.gameObject) {
	                    key.SetTarget(itarget, newReferences[i].transform);
	                }
	            }
	        }

	        return new List<GameObject>();
	    }

	    protected override void DoCopy(AMTrack track) {
	        (track as AMOrientationTrack).obj = obj;
	    }
	}
}