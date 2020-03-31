using UnityEngine;
using System.Collections;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class AudioKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Audio; } }

        public AudioClip audioClip;
        public bool loop;
        public bool oneShot;

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as AudioKey;

            a.audioClip = audioClip;
            a.loop = loop;
            a.oneShot = oneShot;
        }

        #region action

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            //float sTime = getWaitTime(seq.take.frameRate, 0.0f);

            Sequence _seq = seq.sequence;
            AudioSource _src = target as AudioSource;
            float frameRate = seq.take.frameRate;
            float frameCount = Mathf.Ceil(audioClip.length * frameRate);

            if(oneShot) {
                seq.InsertCallback(this, () => {
                    //don't play when going backwards
                    if(_seq.isBackwards) return;

                    _src.pitch = _seq.timeScale;
                    _src.PlayOneShot(audioClip);
                });
            }
            else {
                var tweenV = DOTween.To(TweenPlugElapsed.Get(), () => 0f, (t) => {
                    //don't play when going backwards
                    if(_seq.isBackwards) return;

                    _src.time = t % audioClip.length;

                    _src.pitch = _seq.timeScale;

                    if((_src.isPlaying && _src.clip == audioClip)) return;
                    _src.loop = loop;
                    _src.clip = audioClip;
                    _src.Play();
                }, 0, audioClip.length);

                tweenV.plugOptions = new TweenPlugElapsedOptions(new TweenPlugElapsedCounter());

                seq.Insert(this, tweenV);
            }
        }

        public ulong getTimeInSamples(int frequency, float time) {
            return (ulong)((44100 / frequency) * frequency * time);
        }
        public override int getNumberOfFrames(int frameRate) {
            if(!audioClip || (loop && !oneShot)) return -1;
            return Mathf.CeilToInt(audioClip.length * (float)frameRate);
        }
        #endregion
    }
}