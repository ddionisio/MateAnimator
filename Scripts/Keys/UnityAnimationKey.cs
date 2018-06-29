using UnityEngine;
using System.Collections;

using DG.Tweening;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;

namespace M8.Animator {
    [System.Serializable]
    public class UnityAnimationKey : Key {
        public override SerializeType serializeType { get { return SerializeType.UnityAnimation; } }

        public WrapMode wrapMode;   // animation wrap mode
        public AnimationClip amClip;
        public bool crossfade = true;
        public float crossfadeTime = 0.3f;

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as UnityAnimationKey;

            a.wrapMode = wrapMode;
            a.amClip = amClip;
            a.crossfade = crossfade;
            a.crossfadeTime = crossfadeTime;

        }

        // get number of frames, -1 is infinite
        public override int getNumberOfFrames(int frameRate) {
            if(!amClip) return -1;
            if(wrapMode != WrapMode.Once) return -1;
            return Mathf.CeilToInt(amClip.length * (float)frameRate);
        }

        #region action
        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            int frameRate = seq.take.frameRate;
            float waitTime = getWaitTime(frameRate, 0.0f);
            var anim = (target as GameObject).GetComponent<Animation>();

            float duration = wrapMode == WrapMode.Once ? amClip.length : ((seq.take.getLastFrame() - frame) + 1) / (float)frameRate;

            if(crossfade) {
                if(index > 0) {
                    var prevKey = track.keys[index - 1] as UnityAnimationKey;

                    var prevAnimState = anim[prevKey.amClip.name];
                    var prevWrap = prevKey.wrapMode;
                    var prevStartTime = prevKey.getWaitTime(frameRate, 0.0f);
                    var animState = anim[amClip.name];

                    var tween = DOTween.To(new FloatPlugin(), () => 0f, (x) => {
                        if(x < crossfadeTime) {
                            float weight = x / crossfadeTime;

                            prevAnimState.enabled = true;
                            prevAnimState.wrapMode = prevWrap;
                            prevAnimState.weight = 1.0f - weight;
                            prevAnimState.time = (waitTime + x) - prevStartTime;

                            animState.enabled = true;
                            animState.wrapMode = wrapMode;
                            animState.weight = weight;
                            animState.time = x;

                            anim.Sample();

                            prevAnimState.enabled = false;
                            animState.enabled = false;
                        }
                        else {
                            animState.enabled = true;
                            animState.wrapMode = wrapMode;
                            animState.weight = 1.0f;
                            animState.time = x;

                            anim.Sample();

                            animState.enabled = false;
                        }
                    }, duration, duration);

                    seq.Insert(this, tween);
                }
                else {
                    var animState = anim[amClip.name];

                    var tween = DOTween.To(new FloatPlugin(), () => 0f, (x) => {
                        animState.enabled = true;
                        animState.wrapMode = wrapMode;
                        animState.time = x;

                        if(x < crossfadeTime)
                            animState.weight = x / crossfadeTime;
                        else
                            animState.weight = 1.0f;

                        anim.Sample();
                        animState.enabled = false;
                    }, duration, duration);

                    seq.Insert(this, tween);
                }
            }
            else {
                var animState = anim[amClip.name];

                var tween = DOTween.To(new FloatPlugin(), () => 0f, (x) => {
                    animState.enabled = true;
                    animState.wrapMode = wrapMode;
                    animState.time = x;
                    animState.weight = 1.0f;
                    anim.Sample();
                    animState.enabled = false;
                }, duration, duration);

                seq.Insert(this, tween);
            }
        }
        #endregion
    }
}