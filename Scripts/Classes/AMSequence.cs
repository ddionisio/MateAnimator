using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;

public class AMSequence {
    private int mId;
    private AMITarget mTarget;
    private AMTakeData mTake;
    private Sequence mSequence;

    private AMActionTween mActionTween;
    private List<AMActionData> mInsertActionTrack;

    public int id { get { return mId; } }
    public AMITarget target { get { return mTarget; } }
    public AMTakeData take { get { return mTake; } }
    public Sequence sequence { get { return mSequence; } }

    public TweenDelegate.TweenCallbackWParms triggerCallback { get { return OnTrigger; } }

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
            HOTween.Kill(mSequence);
            mInsertActionTrack = null;
            mActionTween = null;
        }

        mSequence = new Sequence(
            new SequenceParms()
            .Id(string.Format("{0}:{1}", goName, mTake.name))
            .UpdateType(updateType)
            .AutoKill(autoKill)
            .Loops(mTake.numLoop, mTake.loopMode)
            .OnComplete(OnSequenceComplete));

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
            mSequence.Insert(mActionTween.startTime, HOTween.To(this, mActionTween.duration, new TweenParms().Prop("id", mActionTween)));
            
        }

        //prepend delay at the beginning
        if(minWaitTime > 0.0f)
            mSequence.PrependInterval(minWaitTime);
    }

    public void Reset(bool ignoreSequence) {
        if(!ignoreSequence && mSequence != null) {
            mSequence.Pause();
            mSequence.GoTo(0);
        }

        if(mActionTween != null)
            mActionTween.Reset();
    }

    /// <summary>
    /// Only call this during OnDestroy
    /// </summary>
    public void Destroy() {
        if(mSequence != null) {
            HOTween.Kill(mSequence);
            mSequence = null;
        }

        mActionTween = null;

        mTarget = null;
        mTake = null;
    }

    void OnSequenceComplete() {
        mTake.stopAudio(mTarget);
        mTake.stopAnimations(mTarget);

        if(!mSequence.autoKillOnComplete) {
            if(mTake.loopBackToFrame >= 0) {
                if(mSequence.isReversed)
                    mSequence.Reverse();
                mSequence.GoTo(((float)mTake.loopBackToFrame) / ((float)mTake.frameRate));
                mSequence.Play();
                return;
            }
        }

        mTarget.TargetSequenceComplete(this);
    }

    void OnTrigger(TweenEvent dat) {
        mTarget.TargetSequenceTrigger(this, (AMKey)dat.parms[0], (AMTriggerData)dat.parms[1]);
    }
}