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

        public bool isForcedLoop {
            get { return mIsForcedLoop; }
            set {
                if(mIsForcedLoop != value) {
                    mIsForcedLoop = value;

                    //sequence needs to be rebuilt via Animate
                    if(sequence != null && take.numLoop > 0) {
                        sequence.Kill();
                        sequence = null;
                    }
                }
            }
        }

        private bool mIsAutoKill;

        public event System.Action completeCallback;
        public event System.Action stepCompleteCallback;

        private bool mIsForcedLoop;

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

        public void InsertCallback(Key key, TweenCallback callback) {
            sequence.InsertCallback(key.getWaitTime(take.frameRate, 0.0f), callback);
        }

        public void InsertCallback(float atPosition, TweenCallback callback) {
            sequence.InsertCallback(atPosition, callback);
        }

        public void Build(bool autoKill, UpdateType updateType, bool updateTimeIndependent) {
            if(sequence != null)
                sequence.Kill();

            //don't create sequence if there are no tracks
            if(take.trackValues == null || take.trackValues.Count == 0)
                return;

            //create sequence
            sequence = DOTween.Sequence();
            sequence.SetUpdate(updateType, updateTimeIndependent);
            sequence.SetAutoKill(mIsAutoKill = autoKill);
            sequence.Pause(); //don't allow play when dotween default autoplay is on

            int LoopCount;
            int loopBackFrame;
            var loopType = take.loopMode;

            if(isForcedLoop && take.numLoop > 0) {
                LoopCount = -1;
                loopBackFrame = 0;
            }
            else {
                LoopCount = take.numLoop;
                loopBackFrame = take.loopBackToFrame;
            }

            if(LoopCount < 0 && loopBackFrame > 0) {
                if(loopType == LoopType.Yoyo)
                    sequence.SetLoops(2, loopType);
                else
                    sequence.SetLoops(1, loopType); //allow sequence to end so we can loop back to specific frame
            }
            else
                sequence.SetLoops(LoopCount, loopType);

            sequence.OnComplete(OnSequenceComplete);
            sequence.OnStepComplete(OnStepComplete);

            //take.maintainCaches(target);

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
            if((LoopCount >= 0 || loopBackFrame <= 0) && take.endFramePadding > 0)
                sequence.AppendInterval(take.endFramePadding / (float)take.frameRate);
        }

        public void Reset() {
            if(sequence != null) {
                sequence.Pause();
                sequence.Goto(0);
            }
        }

        public void Restart() {
            if(sequence != null) {
                sequence.Restart();
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