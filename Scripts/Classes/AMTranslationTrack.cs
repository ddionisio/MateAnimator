using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMTranslationTrack : AMTrack {
	[SerializeField]
	private Transform _obj;
	public Transform obj {
		get {
			return _obj;	
		}
		set {
			if(value != null && cache.Count <= 0) cachedInitialPosition = value.position;
			_obj = value;
			
		}
	}
	public Vector3 cachedInitialPosition;
	
	public override string getTrackType() {
		return "Translation";	
	}
	// add a new key
	public void addKey(int _frame, Vector3 _position, int _interp, int _easeType) {
		foreach(AMTranslationKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.position = _position;
				key.interp = _interp;
				key.easeType = _easeType;
				// update cache
				updateCache();
				return;
			}
		}
		AMTranslationKey a = ScriptableObject.CreateInstance<AMTranslationKey>();
		a.frame = _frame;
		a.position = _position;
		a.interp = _interp;
		a.easeType = _easeType;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add a new key, default interpolation and easeType
	public void addKey(int _frame, Vector3 _position) {
		foreach(AMTranslationKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.position = _position;
				// update cache
				updateCache();
				return;
			}
		}
		AMTranslationKey a = ScriptableObject.CreateInstance<AMTranslationKey>();
		a.frame = _frame;
		a.position = _position;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	
	// preview a frame in the scene view
	public override void previewFrame(float frame, AMTrack extraTrack = null) {
		if(!obj) return;
		if(cache.Count <= 0) return;
		// if before first frame
		if(frame <= (float) cache[0].startFrame) {
			obj.position = (cache[0] as AMTranslationAction).path[0];
			return;
		}
		// if beyond last frame
		if(frame >= (float) (cache[cache.Count-1] as AMTranslationAction).endFrame) {
			obj.position = 	(cache[cache.Count-1] as AMTranslationAction).path[(cache[cache.Count-1] as AMTranslationAction).path.Length-1];
			return;
		}
		// if lies on curve
		foreach(AMTranslationAction action in cache) {
			if(((int)frame<action.startFrame)||((int)frame>action.endFrame)) continue;
			if(action.path.Length == 1) {
				obj.position = action.path[0];
				return;
			}
			float _value;
			float framePositionInPath = frame-(float)action.startFrame;
			if (framePositionInPath<0f) framePositionInPath = 0f;
			
			AMTween.EasingFunction ease;
			AnimationCurve curve = null;
			
			if(action.hasCustomEase()) {
				ease = AMTween.customEase;
				curve = action.easeCurve;
			} else {
				ease =  AMTween.GetEasingFunction((AMTween.EaseType)action.easeType);
			}
			
			_value = ease(0f,1f,framePositionInPath/action.getNumberOfFrames(),curve);
			
			AMTween.PutOnPath(obj,action.path,Mathf.Clamp (_value,0f,1f));
			return;
		}
		
	}
	// returns true if autoKey successful
	public bool autoKey(Transform _obj, int frame) {
		if(!obj) return false;
		if(_obj != obj) return false;
		
		if(cache.Count <= 0) {
			if(_obj.position != cachedInitialPosition) {
				// if updated position, addkey
				addKey (frame,_obj.position);
				return true;
			}
			return false;
		}
		Vector3 oldPos = getPositionAtFrame((float)frame);
		if(_obj.position != oldPos) {
			// if updated position, addkey
			addKey (frame,_obj.position);
			return true;
		}
		return false;
	}
	public Vector3 getPositionAtFrame(float frame) {
		if(cache.Count <= 0) return obj.position;
		// if before first frame
		if(frame <= (float) cache[0].startFrame) {
			return (cache[0] as AMTranslationAction).path[0];
		}
		// if beyond last frame
		if(frame >= (float) (cache[cache.Count-1] as AMTranslationAction).endFrame) {
			return (cache[cache.Count-1] as AMTranslationAction).path[(cache[cache.Count-1] as AMTranslationAction).path.Length-1];
		}
		// if lies on curve
		foreach(AMTranslationAction action in cache) {
			if(((int)frame<action.startFrame)||((int)frame>action.endFrame)) continue;
			if(action.path.Length == 1) {
				return action.path[0];
			}
			// ease
			AMTween.EasingFunction ease;
			AnimationCurve curve = null;
			
			if(action.hasCustomEase()) {
				ease = AMTween.customEase;
				curve = action.easeCurve;
			} else {
				ease = AMTween.GetEasingFunction((AMTween.EaseType)action.easeType);
			}
			float framePositionInPath = frame-(float)action.startFrame;
			if (framePositionInPath<0f) framePositionInPath = 0f;
			return AMTween.PointOnPath(action.path,Mathf.Clamp (ease(0f,1f,framePositionInPath/action.getNumberOfFrames(),curve),0f,1f));
		}	
		Debug.LogError("Animator: Could not get "+obj.name+" position at frame '"+frame+"'");
		return new Vector3(0f,0f,0f);
	}
	// draw gizmos
	public override void drawGizmos(float gizmo_size) {
		foreach (AMTranslationAction action in cache) {
			if(action.path.Length>1) {
				AMTween.DrawPath(action.path, new Color(255f,255f,255f,.5f)); 
				Gizmos.color = Color.green;
       			Gizmos.DrawSphere(action.path[0], gizmo_size);
				Gizmos.DrawSphere(action.path[action.path.Length-1], gizmo_size);
			}
		}
	}

	private AMPath getPathFromIndex(int startIndex) {
		// sort the keys by frame		
		List<Vector3> path = new List<Vector3>();	
		int endIndex, startFrame, endFrame;
		endIndex = startIndex;
		startFrame = keys[startIndex].frame;
		endFrame = keys[startIndex].frame;
		
		path.Add ((keys[startIndex] as AMTranslationKey).position);	
		
		
		// get path from startIndex until the next linear interpolation key (inclusive)
		for(int i=startIndex+1;i<keys.Count;i++) {
			path.Add ((keys[i] as AMTranslationKey).position);	
			endFrame = keys[i].frame;
			endIndex = i;
			if((keys[i] as AMTranslationKey).interp == (int)AMTranslationKey.Interpolation.Linear) break;
		}
		return new AMPath(path.ToArray(),(keys[startIndex] as AMTranslationKey).interp,startFrame,endFrame,startIndex,endIndex);
	}
	// update cache (optimized)
	public override void updateCache() {
		// destroy cache
		destroyCache();
		// create new cache
		cache = new List<AMAction>();
		AMPath path;
		// sort keys
		sortKeys();
		// get all paths and add them to the action list
		for(int i=0;i<keys.Count;i++) {
			path = getPathFromIndex(i);
				AMTranslationAction a = ScriptableObject.CreateInstance<AMTranslationAction> ();
				a.startFrame = path.startFrame;
				a.endFrame = path.endFrame;
				a.obj = obj;
				a.path = path.path;
				a.easeType = (keys[i] as AMTranslationKey).easeType;
				a.customEase = new List<float>(keys[i].customEase);
				cache.Add (a);
			if(i<keys.Count-1) i=path.endIndex-1;
		}
		// update cache for orientation tracks with track obj as target
		foreach(AMTrack track in parentTake.trackValues) {
			if(track is AMOrientationTrack && ((track as AMOrientationTrack).obj == obj || (track as AMOrientationTrack).hasTarget(obj))) {
				track.updateCache();	
			}
		}
		base.updateCache();
	}
	// get the starting translation key for the action where the frame lies
	public AMTranslationKey getActionStartKeyFor(int frame) {
		foreach(AMTranslationAction action in cache) {
			if((frame<action.startFrame)||(frame>=action.endFrame)) continue;
			return (AMTranslationKey) getKeyOnFrame(action.startFrame);
		}
		Debug.LogError("Animator: Action for frame "+frame+" does not exist in cache.");
		return new AMTranslationKey();
	}
	
	public Vector3 getInitialPosition() {
		return (keys[0] as AMTranslationKey).position;
	}
	
	public override AnimatorTimeline.JSONInit getJSONInit ()
	{
		if(!obj || keys.Count <= 0) return null;
		AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
		init.type = "position";
		init.go = obj.gameObject.name;
		AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
		v.setValue(getInitialPosition());
		init.position = v;
		return init;
	}
	
	public override List<GameObject> getDependencies() {
		List<GameObject> ls = new List<GameObject>();
		if(obj) ls.Add(obj.gameObject);
		return ls;
	}
	
	public override List<GameObject> updateDependencies (List<GameObject> newReferences, List<GameObject> oldReferences)
	{
		if(!obj) return new List<GameObject>();
		for(int i=0;i<oldReferences.Count;i++) {
			if(oldReferences[i] == obj.gameObject) {
				obj = newReferences[i].transform;
				break;
			}
		}
		return new List<GameObject>();
	}

}
