using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace MateAnimator {
	public class AMSequence {
	    private int mId;
	    private AMITarget mTarget;
	    private AMTakeData mTake;
	    private Sequence mSequence;
        private bool mIsAutoKill;

	    public int id { get { return mId; } }
	    public AMITarget target { get { return mTarget; } }
	    public AMTakeData take { get { return mTake; } }
	    public Sequence sequence { get { return mSequence; } }
        
	    public AMSequence(AMITarget itarget, int id, AMTakeData take) {
	        mTarget = itarget;
	        mId = id;
	        mTake = take;

	        if(mTake.loopBackToFrame > 0 && mTake.numLoop <= 0)
	            mTake.numLoop = 1;
	    }

	    public void Insert(AMKey key, Tweener tween) {
	        mSequence.Insert(key.getWaitTime(mTake.frameRate, 0.0f), tween);
	    }

        public void Insert(float atPosition, Tweener tween) {
            mSequence.Insert(atPosition, tween);
        }

	    public void Build(bool autoKill, UpdateType updateType, bool updateTimeIndependent) {
	        if(mSequence != null)
                mSequence.Kill();

            //create sequence
            mSequence = DOTween.Sequence();
            mSequence.SetUpdate(updateType, updateTimeIndependent);
            mSequence.SetAutoKill(mIsAutoKill = autoKill);
            mSequence.SetLoops(mTake.numLoop, mTake.loopMode);
            mSequence.OnComplete(OnSequenceComplete);
            
	        mTake.maintainCaches(mTarget);

	        float minWaitTime = float.MaxValue;

	        foreach(AMTrack track in mTake.trackValues) {
	            Object tgt = null;
	            if((tgt = track.GetTarget(mTarget)) != null) {
	                track.buildSequenceStart(this);

	                int keyMax = track.keys.Count;
	                if(keyMax > 0) {
	                    for(int keyInd = 0; keyInd < keyMax; keyInd++) {
	                        AMKey key = track.keys[keyInd];
	                        key.build(this, track, keyInd, tgt);
	                    }

	                    float waitTime = track.keys[0].getWaitTime(mTake.frameRate, 0.0f);
	                    if(waitTime < minWaitTime)
	                        minWaitTime = waitTime;
	                }
	            }
	        }

	        //prepend delay at the beginning
	        if(minWaitTime > 0.0f)
	            mSequence.PrependInterval(minWaitTime);
	    }

	    public void Reset() {
	        if(mSequence != null) {
	            mSequence.Pause();
                mSequence.Goto(0);
	        }
	    }

	    /// <summary>
	    /// Only call this during OnDestroy
	    /// </summary>
	    public void Destroy() {
	        if(mSequence != null) {
                mSequence.Kill();
	            mSequence = null;
	        }

	        mTarget = null;
	        mTake = null;
	    }

        public void Trigger(AMKey key, AMTriggerData data) {
            mTarget.SequenceTrigger(this, key, data);
        }

	    void OnSequenceComplete() {
	        mTake.PlayComplete(mTarget);

	        mTarget.SequenceComplete(this);

            if(!mIsAutoKill) {
	            if(mTake.loopBackToFrame >= 0) {
                    if(mSequence.IsBackwards())
                        mSequence.Flip();

	                (mTarget as AnimatorData).PlayAtFrame(mTake.name, mTake.loopBackToFrame);
	                return;
	            }
	        }
	    }
	}
}