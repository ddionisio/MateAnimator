using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AMAudioTrack : AMTrack {
	
	public AudioSource audioSource;
	
	public override string getTrackType() {
		return "Audio";	
	}
	
	public bool setAudioSource(AudioSource audioSource) {
		if(this.audioSource != audioSource) {
			this.audioSource = audioSource;
			return true;
		}
		return false;
	}
	public override void updateCache() {
		// destroy cache
		destroyCache();
		// create new cache
		cache = new List<AMAction>();
		// sort keys
		sortKeys();
		// add all clips to list
		for(int i=0;i<keys.Count;i++) {
			AMAudioAction a = ScriptableObject.CreateInstance<AMAudioAction> ();
			a.startFrame = keys[i].frame;
			a.audioSource = audioSource;
			a.audioClip = (keys[i] as AMAudioKey).audioClip;
			a.loop = (keys[i] as AMAudioKey).loop;
			cache.Add (a);
		}
		base.updateCache();
	}
	// add a new key
	public void addKey(int _frame, AudioClip _clip, bool _loop) {
		foreach(AMAudioKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.audioClip = _clip;
				key.loop = _loop;
				// update cache
				updateCache();
				return;
			}
		}
		AMAudioKey a = ScriptableObject.CreateInstance<AMAudioKey>();
		a.frame = _frame;
		a.audioClip = _clip;
		a.loop = _loop;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	
	public override void previewFrame(float frame, AMTrack extraTrack = null) { 
		// do nothing 
	}
	// sample audio between frames
	public void sampleAudio(float frame, float speed, int frameRate) {
		if(!audioSource) return;
		float time;
		for(int i=cache.Count-1;i>=0;i--) {
			if(!(cache[i] as AMAudioAction).audioClip) return;
			if(cache[i].startFrame <= frame) {
				// get time
				time = ((frame-cache[i].startFrame)/frameRate);
				// if loop is set to false and is beyond length, then return
				if(!(cache[i] as AMAudioAction).loop && time > (cache[i] as AMAudioAction).audioClip.length) return;
				// find time based on length
				time = time % (cache[i] as AMAudioAction).audioClip.length;
				if(audioSource.isPlaying) audioSource.Stop();
				audioSource.clip = null;
				audioSource.clip = (cache[i] as AMAudioAction).audioClip;
				audioSource.loop = (cache[i] as AMAudioAction).loop;
				audioSource.time = time;
				audioSource.pitch = speed;
				
				audioSource.Play();
				
				return;
			}
		}
	}
	// sample audio at frame
	public void sampleAudioAtFrame(int frame, float speed, int frameRate) {
		if(!audioSource) return;
		
		for(int i=cache.Count-1;i>=0;i--) {
			if(cache[i].startFrame == frame) {
				if(audioSource.isPlaying) audioSource.Stop();
				audioSource.clip = null;
				audioSource.clip = (cache[i] as AMAudioAction).audioClip;
				audioSource.time = 0f;
				audioSource.loop = (cache[i] as AMAudioAction).loop;
				audioSource.pitch = speed;
				audioSource.Play();
				return;
			}
		}	
	}	
	public void stopAudio() {
		if(!audioSource) return;
		if(audioSource.isPlaying) audioSource.Stop();
	}
	
	public ulong getTimeInSamples(int frequency, float time) {
		return (ulong)((44100/frequency)*frequency*time);	
	}
	
	public override AnimatorTimeline.JSONInit getJSONInit ()
	{
		// no initial values to set
		return null;
	}
	
	public override List<GameObject> getDependencies() {
		List<GameObject> ls = new List<GameObject>();
		if(audioSource) ls.Add(audioSource.gameObject);
		return ls;
	}
	
	public override List<GameObject> updateDependencies (List<GameObject> newReferences, List<GameObject> oldReferences)
	{
		List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!audioSource) return lsFlagToKeep;
		for(int i=0;i<oldReferences.Count;i++) {
			if(oldReferences[i] == audioSource.gameObject) {
				AudioSource _audioSource = (AudioSource) newReferences[i].GetComponent(typeof(AudioSource));
				// missing audiosource
				if(!_audioSource) {
					Debug.LogWarning("Animator: Audio Track component 'AudioSource' not found on new reference for GameObject '"+audioSource.gameObject.name+"'. Duplicate not replaced.");
					lsFlagToKeep.Add(oldReferences[i]);
					return lsFlagToKeep;
				}
				audioSource = _audioSource;
				break;
			}
		}
		return lsFlagToKeep;
	}
}
