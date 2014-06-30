using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Holoville.HOTween;
using Holoville.HOTween.Core;
using Holoville.HOTween.Plugins.Core;

public class AMActionTween : ABSTweenPlugin {
    private const int trackValIndInit = -2;
    private const int trackValIndStart = -1;

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
            for(int i = 0; i < mValueTrackCurIndices.Length; i++)
                mValueTrackCurIndices[i] = trackValIndInit;
        }
    }

    public void Reset() {
        if(mValueTrackCurIndices != null) {
            for(int i = 0; i < mValueTrackCurIndices.Length; i++) {
                mValueTrackCurIndices[i] = trackValIndStart;
            }
        }
    }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() { }

    protected override void SetIncremental(int p_diffIncr) { }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        float t = mStartTime + p_totElapsed;

        //bool backward = mLastT > t;

        for(int i = 0, max = mValueTracks.Length; i < max; i++) {
            int curInd = mValueTrackCurIndices[i];

            //determine if we need to move
            if(curInd == trackValIndInit) //wait one frame
                mValueTrackCurIndices[i] = trackValIndStart;
            else if(curInd == trackValIndStart) {
                int newInd = GetValueIndex(mValueTracks[i], t);
                mValueTrackCurIndices[i] = newInd;
                AMActionData act = mValueTracks[i][newInd];
                act.Apply(t - act.startTime);
            }
            else {
                int newInd = GetNextValueTrackIndex(mValueTracks[i], curInd, t);
                if(newInd != curInd) {
                    mValueTrackCurIndices[i] = newInd;
                    AMActionData act = mValueTracks[i][newInd];
                    act.Apply(t - act.startTime);
                }
            }
        }
    }

    /// <summary>
    /// Returns index based on given time (clamped)
    /// </summary>
    int GetValueIndex(AMActionData[] values, float curTime) {
        if(values[0].startTime > curTime)
            return 0;

        int max = values.Length;
        for(int i = 0; i < max; i++) {
            if(values[i].startTime <= curTime && curTime <= values[i].endTime) {
                return i;
            }
        }
        return max-1;
    }

    /// <summary>
    /// Returns next index (clamped)
    /// </summary>
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

        if(retInd < 0) return 0;
        else if(retInd >= values.Length) return values.Length - 1;

        return retInd;
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

public abstract class AMActionData {
    protected float mStartTime;
    protected float mEndTime;

    public float startTime { get { return mStartTime; } }
    public float endTime { get { return mEndTime; } }

    public AMActionData(AMKey key, int frameRate) {
        mStartTime = key.getWaitTime(frameRate, 0.0f);
        mEndTime = mStartTime + ((float)key.getNumberOfFrames(frameRate))/((float)frameRate);
    }

    public AMActionData(float startTime, float endTime) {
        mStartTime = startTime;
        mEndTime = endTime;
    }

    public abstract void Apply(float t);
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

    public override void Apply(float t) {
        mGO.SetActive(mVal);
    }
}

public class AMActionTransLocalPos : AMActionData {
    private Transform mTrans;
    private Vector3 mPos;

    public AMActionTransLocalPos(AMKey key, int frameRate, Transform target, Vector3 pos)
        : base(key, frameRate) {
        mTrans = target;
        mPos = pos;
    }

    public override void Apply(float t) {
        mTrans.localPosition = mPos;
    }
}

public class AMActionTransLocalRot : AMActionData {
    private Transform mTrans;
    private Quaternion mRot;

    public AMActionTransLocalRot(AMKey key, int frameRate, Transform target, Quaternion rot)
        : base(key, frameRate) {
        mTrans = target;
        mRot = rot;
    }

    public override void Apply(float t) {
        mTrans.localRotation = mRot;
    }
}

public class AMActionSpriteSet : AMActionData {
    private SpriteRenderer mSpriteRender;
    private Sprite mSprite;

    public AMActionSpriteSet(AMKey key, int frameRate, SpriteRenderer target, Sprite spr)
        : base(key, frameRate) {
        mSpriteRender = target;
        mSprite = spr;
    }

    public override void Apply(float t) {
        mSpriteRender.sprite = mSprite;
    }
}

public class AMActionPropertySet : AMActionData {
    private object mObj;
    private PropertyInfo mProp;
    private object mVal;

    public AMActionPropertySet(AMKey key, int frameRate, object target, PropertyInfo prop, object val) 
    : base(key, frameRate) {
        mObj = target;
        mProp = prop;
        mVal = val;
    }

    public override void Apply(float t) {
        mProp.SetValue(mObj, mVal, null);
    }
}

public class AMActionFieldSet : AMActionData {
    private object mObj;
    private FieldInfo mField;
    private object mVal;

    public AMActionFieldSet(AMKey key, int frameRate, object target, FieldInfo f, object val)
        : base(key, frameRate) {
        mObj = target;
        mField = f;
        mVal = val;
    }

    public override void Apply(float t) {
        mField.SetValue(mObj, mVal);
    }
}

public class AMActionAudioPlay : AMActionData {
    private AudioSource mSrc;
    private AudioClip mClip;
    private bool mLoop;

    public AMActionAudioPlay(AMKey key, int frameRate, AudioSource src, AudioClip clip, bool loop)
        : base(key, frameRate) {
        mSrc = src;
        mClip = clip;
        mLoop = loop;
    }

    public override void Apply(float t) {
        if(mSrc.isPlaying && mSrc.loop && mSrc.clip == mClip) return;

        mSrc.loop = mLoop;
        mSrc.clip = mClip;
        //mSrc.time = t;
        mSrc.Play();
    }
}

