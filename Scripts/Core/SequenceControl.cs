using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

namespace M8.Animator {
    public class SequenceControl {
        public int id { get; private set; }
        public ITarget target { get; private set; }
        public Take take { get; private set; }
        public Sequence sequence { get; private set; }

        private bool mIsAutoKill;

        public event System.Action completeCallback;
        public event System.Action stepCompleteCallback;

        public SequenceControl(ITarget itarget, int id, Take take) {
            target = itarget;
            this.id = id;
            this.take = take;
        }

        public void Insert(Key key, Tweener tween) {
            sequence.Insert(key.getWaitTime(take.frameRate, 0.0f), tween);
        }

        public void Insert(float atPosition, Tweener tween) {
            sequence.Insert(atPosition, tween);
        }

        public void Build(bool autoKill, UpdateType updateType, bool updateTimeIndependent) {
            if(sequence != null)
                sequence.Kill();

            //create sequence
            sequence = DOTween.Sequence();
            sequence.SetUpdate(updateType, updateTimeIndependent);
            sequence.SetAutoKill(mIsAutoKill = autoKill);

            if(take.numLoop < 0 && take.loopBackToFrame > 0) {
                if(take.loopMode == LoopType.Yoyo)
                    sequence.SetLoops(2, take.loopMode);
                else
                    sequence.SetLoops(1, take.loopMode); //allow sequence to end so we can loop back to specific frame
            }
            else
                sequence.SetLoops(take.numLoop, take.loopMode);

            sequence.OnComplete(OnSequenceComplete);
            sequence.OnStepComplete(OnStepComplete);

            take.maintainCaches(target);

            float minWaitTime = float.MaxValue;

            foreach(Track track in take.trackValues) {
                Object tgt = null;
                if((tgt = track.GetTarget(target)) != null) {
                    track.buildSequenceStart(this);

                    int keyMax = track.keys.Count;
                    if(keyMax > 0) {
                        for(int keyInd = 0; keyInd < keyMax; keyInd++) {
                            Key key = track.keys[keyInd];
                            key.build(this, track, keyInd, tgt);
                        }

                        float waitTime = track.keys[0].getWaitTime(take.frameRate, 0.0f);
                        if(waitTime < minWaitTime)
                            minWaitTime = waitTime;
                    }
                }
            }

            //prepend delay at the beginning
            if(minWaitTime > 0.0f)
                sequence.PrependInterval(minWaitTime);

            //append delay at the end
            if((take.numLoop >= 0 || take.loopBackToFrame <= 0) && take.endFramePadding > 0)
                sequence.AppendInterval(take.endFramePadding / (float)take.frameRate);
        }

        public void Reset() {
            if(sequence != null) {
                sequence.Pause();
                sequence.Goto(0);
            }
        }

        /// <summary>
        /// Only call this during OnDestroy
        /// </summary>
        public void Destroy() {
            if(sequence != null) {
                sequence.Kill();
                sequence = null;
            }

            target = null;
            take = null;
        }

        public void Trigger(Key key, TriggerData data) {
            target.SequenceTrigger(this, key, data);
        }

        void OnSequenceComplete() {
            if(completeCallback != null)
                completeCallback();

            take.PlayComplete(target);

            target.SequenceComplete(this);

            if(!mIsAutoKill) {
                if(take.numLoop < 0 && take.loopBackToFrame > 0) {
                    (target as Animate).PlayAtFrame(take.name, take.loopBackToFrame);
                    return;
                }
            }
        }

        void OnStepComplete() {
            if(stepCompleteCallback != null)
                stepCompleteCallback();
        }
    }
}