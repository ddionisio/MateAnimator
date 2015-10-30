using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace MateAnimator{
	public class AMSequence {
	    private int mId;
	    private AMITarget mTarget;
	    private AMTakeData mTake;
	    private Sequence mSequence;
        private bool mIsAutoKill;

	    private AMActionTween mActionTween;
	    private List<AMActionData> mInsertActionTrack;

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

	    /// <summary>
	    /// Only call this during build, the inserted value will be appended to the current insertValueTrack and will
	    /// be processed after track is complete.
	    /// </summary>
	    public void Insert(AMActionData valueSet) {
	        if(mInsertActionTrack == null)
	            mInsertActionTrack = new List<AMActionData>();
	        mInsertActionTrack.Add(valueSet);
	    }

	    public void Build(string goName, bool autoKill, UpdateType updateType) {
	        if(mSequence != null) {
                mSequence.Kill();
	            mInsertActionTrack = null;
	            mActionTween = null;
	        }

            //create sequence
            mSequence = DOTween.Sequence();
            mSequence.SetId(string.Format("{0}:{1}", goName, mTake.name));
            mSequence.SetUpdate(updateType);
            mSequence.SetAutoKill(mIsAutoKill = autoKill);
            mSequence.SetLoops(mTake.numLoop, mTake.loopMode);
            mSequence.OnComplete(OnSequenceComplete);
            mSequence.OnStepComplete(OnSequenceStepComplete);
            
	        mTake.maintainCaches(mTarget);

	        float minWaitTime = float.MaxValue;

	        List<List<AMActionData>> trackValueSets = null;

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

	                //check to see if we have value sets for this track
	                if(mInsertActionTrack != null) {
	                    if(trackValueSets == null)
	                        trackValueSets = new List<List<AMActionData>>();
	                    trackValueSets.Add(mInsertActionTrack);
	                    mInsertActionTrack = null;
	                }
	            }
	        }

	        //build the value track
	        mInsertActionTrack = null;
	        if(trackValueSets != null && trackValueSets.Count > 0) {
	            mActionTween = new AMActionTween(trackValueSets);
                mSequence.Insert(mActionTween.startTime, DOTween.To(mActionTween, () => mSequence.IsBackwards(), x => { }, false, mActionTween.duration));
	        }

	        //prepend delay at the beginning
	        if(minWaitTime > 0.0f)
	            mSequence.PrependInterval(minWaitTime);
	    }

	    public void Reset(bool ignoreSequence) {
	        if(!ignoreSequence && mSequence != null) {
	            mSequence.Pause();
                mSequence.Goto(0);
	        }

	        if(mActionTween != null)
	            mActionTween.Reset();
	    }

	    /// <summary>
	    /// Only call this during OnDestroy
	    /// </summary>
	    public void Destroy() {
	        if(mSequence != null) {
                mSequence.Kill();
	            mSequence = null;
	        }

	        mActionTween = null;

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

        void OnSequenceStepComplete() {
            if(mActionTween != null)
                mActionTween.Reset();
        }
	}
}