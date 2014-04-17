using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("")]
public class AMAudioTrack : AMTrack {

	[SerializeField]
    AudioSource audioSource;

	protected override void SetSerializeObject(UnityEngine.Object obj) {
		audioSource = obj as AudioSource;
	}
	
	protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
		return targetGO ? targetGO.audio : audioSource;
	}

    public override string getTrackType() {
        return "Audio";
    }

	public override string GetRequiredComponent() {
		return "AudioSource";
	}

    // add a new key
    public AMKey addKey(AMITarget itarget, int _frame, AudioClip _clip, bool _loop) {
        foreach(AMAudioKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.audioClip = _clip;
                key.loop = _loop;
                // update cache
				updateCache(itarget);
                return null;
            }
        }
        AMAudioKey a = gameObject.AddComponent<AMAudioKey>();
        a.enabled = false;
        a.frame = _frame;
        a.audioClip = _clip;
        a.loop = _loop;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(itarget);
        return a;
    }

    // sample audio between frames
	public void sampleAudio(AMITarget target, float frame, float speed, int frameRate) {
		AudioSource src = GetTarget(target) as AudioSource;
		if(!src) return;
        float time;
        for(int i = keys.Count - 1; i >= 0; i--) {
            if(!(keys[i] as AMAudioKey).audioClip) return;
            if(keys[i].frame <= frame) {
                // get time
                time = ((frame - keys[i].frame) / frameRate);
                // if loop is set to false and is beyond length, then return
                if(!(keys[i] as AMAudioKey).loop && time > (keys[i] as AMAudioKey).audioClip.length) return;
                // find time based on length
                time = time % (keys[i] as AMAudioKey).audioClip.length;
				if(src.isPlaying) src.Stop();
				src.clip = null;
				src.clip = (keys[i] as AMAudioKey).audioClip;
				src.loop = (keys[i] as AMAudioKey).loop;
				src.time = time;
				src.pitch = speed;

				src.Play();

                return;
            }
        }
    }
    // sample audio at frame
	public void sampleAudioAtFrame(AMITarget target, int frame, float speed, int frameRate) {
		AudioSource src = GetTarget(target) as AudioSource;
		if(!src) return;

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame == frame) {
				if(src.isPlaying) src.Stop();
				src.clip = null;
				src.clip = (keys[i] as AMAudioKey).audioClip;
				src.time = 0f;
				src.loop = (keys[i] as AMAudioKey).loop;
				src.pitch = speed;
				src.Play();
                return;
            }
        }
    }
	public void stopAudio(AMITarget target) {
		AudioSource src = GetTarget(target) as AudioSource;
		if(!src) return;
		if(src.loop && src.isPlaying) src.Stop();
    }

    public ulong getTimeInSamples(int frequency, float time) {
        return (ulong)((44100 / frequency) * frequency * time);
    }

	public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        // no initial values to set
        return null;
    }

	public override List<GameObject> getDependencies(AMITarget target) {
		AudioSource src = GetTarget(target) as AudioSource;
        List<GameObject> ls = new List<GameObject>();
		if(src) ls.Add(src.gameObject);
        return ls;
    }

	public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
		AudioSource src = GetTarget(target) as AudioSource;
        List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!src) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
			if(oldReferences[i] == src.gameObject) {
                AudioSource _audioSource = (AudioSource)newReferences[i].GetComponent(typeof(AudioSource));
                // missing audiosource
                if(!_audioSource) {
					Debug.LogWarning("Animator: Audio Track component 'AudioSource' not found on new reference for GameObject '" + src.gameObject.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
				SetTarget(target, _audioSource);
                break;
            }
        }
        return lsFlagToKeep;
    }

    protected override AMTrack doDuplicate(GameObject holder) {
        AMAudioTrack ntrack = holder.AddComponent<AMAudioTrack>();
        ntrack.enabled = false;
        ntrack.audioSource = audioSource;

        return ntrack;
    }
}
