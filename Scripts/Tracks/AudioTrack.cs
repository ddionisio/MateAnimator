using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
    [System.Serializable]
    public class AudioTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Audio; } }

        public override bool canTween { get { return false; } }

        [SerializeField]
        AudioSource audioSource;

        bool paused;
        bool pausedLoop;
        int lastSampleKeyIndex = -1;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            audioSource = obj as AudioSource;
            if(audioSource)
                audioSource.playOnAwake = false;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO.GetComponent<AudioSource>() : audioSource;
        }

        public override string getTrackType() {
            return "Audio";
        }

        public override System.Type GetRequiredComponent() {
            return typeof(AudioSource);
        }

        public override void updateCache(ITarget target) {
            base.updateCache(target);

            if(audioSource)
                audioSource.playOnAwake = false;
        }

        public override void PlayStart(ITarget itarget, float frame, int frameRate, float animScale) {
            if(frame > 0) {
                AudioSource src = GetTarget(itarget) as AudioSource;
                if(!src) return;
                float time;
                for(int i = keys.Count - 1; i >= 0; i--) {
                    AudioKey key = keys[i] as AudioKey;
                    if(!key.audioClip) break;
                    if(key.frame <= frame) {
                        src.pitch = itarget.animScale * animScale;

                        if(!key.oneShot) {
                            // get time
                            time = ((frame - key.frame) / frameRate);
                            // if loop is set to false and is beyond length, then return
                            if(!key.loop && time > key.audioClip.length) break;

                            if(src.isPlaying && src.clip != key.audioClip) src.Stop();

                            // find time based on length
                            time = time % key.audioClip.length;

                            src.clip = key.audioClip;
                            src.loop = key.loop;
                            src.time = time;

                            src.Play();
                        }
                        lastSampleKeyIndex = i;
                        break;
                    }
                }
            }
        }

        // add a new key
        public void addKey(ITarget itarget, int _frame, AudioClip _clip, bool _loop) {
            foreach(AudioKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    key.audioClip = _clip;
                    key.loop = _loop;
                    // update cache
                    updateCache(itarget);
                }
            }
            AudioKey a = new AudioKey();
            a.frame = _frame;
            a.audioClip = _clip;
            a.loop = _loop;
            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }

        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            AudioSource src = GetTarget(target) as AudioSource;
            if(!src) return;
            if(play) {
                int iFrame = Mathf.RoundToInt(frame);
                for(int i = keys.Count - 1; i >= 0; i--) {
                    if(keys[i].frame <= iFrame && lastSampleKeyIndex != i) {
                        AudioKey key = keys[i] as AudioKey;

                        src.pitch = target.animScale * playSpeed;

                        if(key.oneShot) {
                            src.PlayOneShot(key.audioClip);
                        }
                        else {
                            if(src.isPlaying && src.clip != key.audioClip) src.Stop();

                            src.clip = key.audioClip;
                            src.loop = key.loop;
                            src.time = keys[i].frame == iFrame ? 0f : ((frame - key.frame) / frameRate) % key.audioClip.length;

                            src.Play();
                        }
                        lastSampleKeyIndex = i;
                        break;
                    }
                }
            }
            else {
                src.loop = false;
                lastSampleKeyIndex = -1;
            }
        }

        public override void PlayComplete(ITarget itarget) {
            AudioSource src = GetTarget(itarget) as AudioSource;
            if(src) {
                if(src.isPlaying) //stop if paused
                    src.Stop();
                else //let it finish playing, with no loop
                    src.loop = false;
            }

            paused = false;
            pausedLoop = false;
            lastSampleKeyIndex = -1;
        }

        public override void Stop(ITarget itarget) {
            AudioSource src = GetTarget(itarget) as AudioSource;
            if(src) src.Stop();
            paused = false;
            pausedLoop = false;
            lastSampleKeyIndex = -1;
        }

        public override void Pause(ITarget itarget) {
            AudioSource src = GetTarget(itarget) as AudioSource;
            if(src && src.isPlaying) {
                pausedLoop = src.loop && src.clip && src.clip.length - src.time < 1f; //only end loop if it's short enough to do so
                if(pausedLoop)
                    src.loop = false;
                else
                    src.Pause();
                paused = true;
            }
        }

        public override void Resume(ITarget itarget) {
            AudioSource src = GetTarget(itarget) as AudioSource;
            if(src && paused) {
                if(pausedLoop)
                    src.loop = true;
                src.Play();
                paused = false;
                pausedLoop = false;
            }
        }

        public override void SetAnimScale(ITarget itarget, float scale) {
            AudioSource src = GetTarget(itarget) as AudioSource;
            if(src) src.pitch = scale;
        }

        public ulong getTimeInSamples(int frequency, float time) {
            return (ulong)((44100 / frequency) * frequency * time);
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            // no initial values to set
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            AudioSource src = GetTarget(target) as AudioSource;
            List<GameObject> ls = new List<GameObject>();
            if(src) ls.Add(src.gameObject);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
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
                    SetTarget(target, newReferences[i].transform);
                    break;
                }
            }
            return lsFlagToKeep;
        }

        protected override void DoCopy(Track track) {
            (track as AudioTrack).audioSource = audioSource;
        }
    }
}