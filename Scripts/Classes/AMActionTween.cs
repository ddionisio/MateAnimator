using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;
using Holoville.HOTween.Plugins.Core;

public class AMActionTween : ABSTweenPlugin {
    private AMActionData[][] mValueTracks;
    private int[] mValueTrackCurIndices;
    private float mStartTime;
    private float mDuration;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public float startTime { get { return mStartTime; } }

    public float duration { get { return mDuration; } }

    public AMActionTween(List<List<AMActionData>> trackValueSets)
        : base(null, false) {
        ignoreAccessor = true;

        if(trackValueSets != null) {
            mStartTime = float.MaxValue;
            float maxEnd = 0.0f;

            mValueTracks = new AMActionData[trackValueSets.Count][];
            for(int i = 0; i < mValueTracks.Length; i++) {
                mValueTracks[i] = trackValueSets[i].ToArray();

                if(mValueTracks[i][0].startTime < mStartTime)
                    mStartTime = mValueTracks[i][0].startTime;
                if(mValueTracks[i][mValueTracks[i].Length-1].endTime > maxEnd)
                    maxEnd = mValueTracks[i][mValueTracks[i].Length-1].endTime;
            }

            mDuration = maxEnd - mStartTime;

            mValueTrackCurIndices = new int[mValueTracks.Length];
            for(int i = 0; i < mValueTracks.Length; i++)
                mValueTrackCurIndices[i] = -1;
        }
    }

    public void ResetValueTrackIndices() {
        if(mValueTrackCurIndices != null) {
            for(int i = 0; i < mValueTrackCurIndices.Length; i++)
                mValueTrackCurIndices[i] = -1;
        }
    }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() { }

    protected override void SetIncremental(int p_diffIncr) { }

    protected override void DoUpdate(float p_totElapsed) {
        float t = mStartTime + p_totElapsed;

        for(int i = 0, max = mValueTracks.Length; i < max; i++) {
            int curInd = mValueTrackCurIndices[i];

            //determine if we need to move
            if(curInd == -1 || curInd >= mValueTracks[i].Length) {
                int newInd = GetValueIndex(mValueTracks[i], t);
                mValueTrackCurIndices[i] = newInd;
                if(newInd >= 0 && newInd < mValueTracks[i].Length)
                    mValueTracks[i][newInd].Apply();
            }
            else {
                int newInd = GetNextValueTrackIndex(mValueTracks[i], curInd, t);
                if(newInd != curInd) {
                    mValueTrackCurIndices[i] = newInd;
                    if(newInd >= 0 && newInd < mValueTracks[i].Length)
                        mValueTracks[i][newInd].Apply();
                }
            }
        }
    }

    int GetValueIndex(AMActionData[] values, float curTime) {
        for(int i = 0, max = values.Length; i < max; i++) {
            if(values[i].startTime <= curTime && curTime <= values[i].endTime) {
                return i;
            }
        }
        return -1;
    }

    int GetNextValueTrackIndex(AMActionData[] values, int curInd, float curTime) {
        AMActionData val = values[curInd];

        int retInd = curInd;

        if(curTime < val.startTime) { //go backwards
            for(retInd = curInd - 1; retInd >= 0; retInd--) {
                if(values[retInd].startTime <= curTime) {
                    if(values[retInd].endTime < curTime) //current time hasn't reached this segment yet
                        retInd = curInd;
                    break;
                }
                else { //this segment has been skipped
                    curInd = retInd;
                }
            }
        }
        else if(curTime > val.endTime) { //forward
            for(retInd = curInd + 1; retInd < values.Length; retInd++) {
                if(values[retInd].endTime >= curTime) {
                    if(values[retInd].startTime > curTime) //current time hasn't reached this segment yet
                        retInd = curInd;
                    break;
                }
                else { //this segment has been skipped
                    curInd = retInd;
                }
            }
        }

        return retInd;
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

public abstract class AMActionData {
    private float mStartTime;
    private float mEndTime;

    public float startTime { get { return mStartTime; } }
    public float endTime { get { return mEndTime; } }

    public AMActionData(AMKey key, int frameRate) {
        mStartTime = key.getWaitTime(frameRate, 0.0f);
        mEndTime = mStartTime + ((float)key.getNumberOfFrames())/((float)frameRate);
    }

    public AMActionData(float startTime, float endTime) {
        mStartTime = startTime;
        mEndTime = endTime;
    }

    public abstract void Apply();
}

public class AMActionGOActive : AMActionData {
    private GameObject mGO;
    private bool mVal;

    public AMActionGOActive(AMKey key, int frameRate, GameObject target, bool val)
        : base(key, frameRate) {
        mGO = target;
        mVal = val;
    }

    public AMActionGOActive(float startTime, float endTime, GameObject target, bool val)
        : base(startTime, endTime) {
        mGO = target;
        mVal = val;
    }

    public override void Apply() {
        mGO.SetActive(mVal);
    }
}