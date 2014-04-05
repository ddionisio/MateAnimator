using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("")]
public class AMAudioTrack : AMTrack {

    public AudioSource audioSource;

    public override string getTrackType() {
        return "Audio";
    }

    // add a new key
    public AMKey addKey(int _frame, AudioClip _clip, bool _loop) {
        foreach(AMAudioKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.audioClip = _clip;
                key.loop = _loop;
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }

    public override void previewFrame(float frame, AMTrack extraTrack = null) {
        // do nothing 
    }
    // sample audio between frames
    public void sampleAudio(float frame, float speed, int frameRate) {
        if(!audioSource) return;
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
                if(audioSource.isPlaying) audioSource.Stop();
                audioSource.clip = null;
                audioSource.clip = (keys[i] as AMAudioKey).audioClip;
                audioSource.loop = (keys[i] as AMAudioKey).loop;
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

        for(int i = keys.Count - 1; i >= 0; i--) {
            if(keys[i].frame == frame) {
                if(audioSource.isPlaying) audioSource.Stop();
                audioSource.clip = null;
                audioSource.clip = (keys[i] as AMAudioKey).audioClip;
                audioSource.time = 0f;
                audioSource.loop = (keys[i] as AMAudioKey).loop;
                audioSource.pitch = speed;
                audioSource.Play();
                return;
            }
        }
    }
    public void stopAudio() {
        if(!audioSource) return;
        if(audioSource.loop && audioSource.isPlaying) audioSource.Stop();
    }

    public ulong getTimeInSamples(int frequency, float time) {
        return (ulong)((44100 / frequency) * frequency * time);
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        // no initial values to set
        return null;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(audioSource) ls.Add(audioSource.gameObject);
        return ls;
    }

    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        if(!audioSource) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == audioSource.gameObject) {
                AudioSource _audioSource = (AudioSource)newReferences[i].GetComponent(typeof(AudioSource));
                // missing audiosource
                if(!_audioSource) {
                    Debug.LogWarning("Animator: Audio Track component 'AudioSource' not found on new reference for GameObject '" + audioSource.gameObject.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
                audioSource = _audioSource;
                break;
            }
        }
        return lsFlagToKeep;
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMAudioTrack ntrack = newTake.gameObject.AddComponent<AMAudioTrack>();
        ntrack.enabled = false;
        ntrack.audioSource = audioSource;

        return ntrack;
    }
}
