using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
	[AddComponentMenu("")]
	public class AMRotationTrack : AMTrack {
	    [SerializeField]
	    private Transform _obj;
	    
		protected override void SetSerializeObject(UnityEngine.Object obj) {
			_obj = obj as Transform;
		}
		
		protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
			return targetGO ? targetGO.transform : _obj;
		}

		public new void SetTarget(AMITarget target, Transform item) {
			base.SetTarget(target, item);
	        if(item != null && keys.Count <= 0) cachedInitialRotation = item.localRotation;
		}

	    public override int version { get { return 2; } }
	    
	    Quaternion cachedInitialRotation;

	    public override string getTrackType() {
	        return "Rotation Quaternion";
	    }
	    // add a new key
	    public void addKey(AMITarget target, OnAddKey addCall, int _frame, Quaternion _rotation) {
	        foreach(AMRotationKey key in keys) {
	            // if key exists on frame, update key
	            if(key.frame == _frame) {
	                key.rotation = _rotation;
	                // update cache
	                updateCache(target);
	                return;
	            }
	        }
	        AMRotationKey a = addCall(gameObject, typeof(AMRotationKey)) as AMRotationKey;
	        a.frame = _frame;
	        a.rotation = _rotation;
	        // set default ease type to linear
	        a.easeType = (int)Ease.Linear;

	        // add a new key
	        keys.Add(a);
	        // update cache
			updateCache(target);
	    }

	    // update cache (optimized)
	    public override void updateCache(AMITarget target) {
			base.updateCache(target);

	        for(int i = 0; i < keys.Count; i++) {
	            AMRotationKey key = keys[i] as AMRotationKey;
				            
	            key.version = version;

	            //a.type = (keys[i] as AMRotationKey).type;

	            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
	            else {
	                if(i > 0 && !keys[i-1].canTween)
	                    key.interp = (int)AMKey.Interpolation.None;

					key.endFrame = -1;
				}
	        }
	    }
	    // preview a frame in the scene view
	    public override void previewFrame(AMITarget target, float frame, int frameRate, bool play, float playSpeed) {
			Transform t = GetTarget(target) as Transform;

            int keyCount = keys.Count;

	        if(!t) return;
	        if(keys == null || keyCount <= 0) return;

	        // if before or equal to first frame, or is the only frame
            AMRotationKey firstKey = keys[0] as AMRotationKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                t.localRotation = firstKey.rotation;
	            return;
	        }

	        // if lies on rotation action
	        for(int i = 0; i < keys.Count; i++) {
	            AMRotationKey key = keys[i] as AMRotationKey;
	            AMRotationKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMRotationKey : null;

	            if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
	            // if no ease
	            if(!key.canTween || keyNext == null) {
					t.localRotation =  key.rotation;
	                return;
	            }
	            // else find Quaternion using easing function

                float numFrames = (float)key.getNumberOfFrames(frameRate);

                float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

	            Quaternion qStart = key.rotation;
	            Quaternion qEnd = keyNext.rotation;

	            if(key.hasCustomEase()) {
                    t.localRotation = Quaternion.LerpUnclamped(qStart, qEnd, Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / numFrames, key.easeCurve));
	            }
	            else {
	                var ease = Utility.GetEasingFunction((Ease)key.easeType);
                    t.localRotation = Quaternion.LerpUnclamped(qStart, qEnd, ease(framePositionInAction, numFrames, key.amplitude, key.period));
	            }

	            return;
	        }
	    }
	    // returns true if autoKey successful, sets output key
	    public bool autoKey(AMITarget itarget, OnAddKey addCall, Transform aObj, int frame, int frameRate) {
			Transform t = GetTarget(itarget) as Transform;
	        if(!t || t != aObj) { return false; }
	        Quaternion r = t.localRotation;
	        if(keys.Count <= 0) {
	            if(r != cachedInitialRotation) {
	                // if updated position, addkey
	                addKey(itarget, addCall, frame, r);
	                return true;
	            }

	            return false;
	        }
	        Quaternion oldRot = getRotationAtFrame(frame, frameRate);
	        if(r != oldRot) {
	            // if updated position, addkey
	            addKey(itarget, addCall, frame, r);
	            return true;
	        }

	        return false;
	    }
	    Quaternion getRotationAtFrame(int frame, int frameRate) {
            // if before or equal to first frame, or is the only frame
            AMRotationKey firstKey = keys[0] as AMRotationKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                return firstKey.rotation;
            }

            // if lies on rotation action
            for(int i = 0; i < keys.Count; i++) {
                AMRotationKey key = keys[i] as AMRotationKey;
                AMRotationKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMRotationKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    return key.rotation;
                }
                // else find Quaternion using easing function

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
            }

	        Debug.LogError("Animator: Could not get rotation at frame '" + frame + "'");
	        return Quaternion.identity;
	    }
	    public Quaternion getInitialRotation() {
	        return (keys[0] as AMRotationKey).rotation;
	    }

		public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
	        if(!_obj || keys.Count <= 0) return null;
	        AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
	        init.type = "rotation";
	        init.go = _obj.gameObject.name;
	        AnimatorTimeline.JSONQuaternion q = new AnimatorTimeline.JSONQuaternion();
	        Quaternion quat = getInitialRotation();
	        q.setValue(new Vector4(quat.x, quat.y, quat.z, quat.w));
	        init.rotation = q;
	        return init;
	    }

	    public override List<GameObject> getDependencies(AMITarget itarget) {
			Transform t = GetTarget(itarget) as Transform;
	        List<GameObject> ls = new List<GameObject>();
	        if(t) ls.Add(t.gameObject);
	        return ls;
	    }
		public override List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
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

	    protected override void DoCopy(AMTrack track) {
	        AMRotationTrack ntrack = track as AMRotationTrack;
	        ntrack._obj = _obj;
	        ntrack.cachedInitialRotation = cachedInitialRotation;
	    }
	}
}
