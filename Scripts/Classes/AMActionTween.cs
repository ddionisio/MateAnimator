using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace MateAnimator {
    public struct AMPlugValueSetOptions {
        public delegate int GetCounterCallback();

        private GetCounterCallback mOnCounter;
        private int mCounter;

        public AMPlugValueSetOptions(Sequence seq) {
            mOnCounter = () => seq.CompletedLoops();
            mCounter = -1;
        }

        public bool isRefresh {
            get {
                int _c = mOnCounter();
                if(mCounter != _c) {
                    mCounter = _c;
                    return true;
                }
                return false;
            }
        }

        public void Reset() {
            mCounter = -1;
        }
    }

    /// <summary>
    /// Note: Set getter as the value to be passed to setter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AMPlugValueSet<T> : ABSTweenPlugin<T, T, AMPlugValueSetOptions> {
        public override T ConvertToStartValue(TweenerCore<T, T, AMPlugValueSetOptions> t, T value) {
            return value;
        }

        public override void EvaluateAndApply(AMPlugValueSetOptions options, Tween t, bool isRelative, DOGetter<T> getter, DOSetter<T> setter, float elapsed, T startValue, T changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            if(options.isRefresh)
                setter(getter());
        }

        public override float GetSpeedBasedDuration(AMPlugValueSetOptions options, float unitsXSecond, T changeValue) {
            return 1.0f/unitsXSecond;
        }

        public override void Reset(TweenerCore<T, T, AMPlugValueSetOptions> t) {
            t.plugOptions.Reset();
        }

        public override void SetChangeValue(TweenerCore<T, T, AMPlugValueSetOptions> t) { }

        public override void SetFrom(TweenerCore<T, T, AMPlugValueSetOptions> t, bool isRelative) { }

        public override void SetRelativeEndValue(TweenerCore<T, T, AMPlugValueSetOptions> t) { }
    }

    public class AMActionTween : ABSTweenPlugin<bool, bool, NoOptions> {
	    private const int trackValIndStart = -1;

	    private AMActionData[][] mValueTracks;
	    private int[] mValueTrackCurIndices;
	    private float mStartTime;
	    private float mDuration;
        private bool mIsStarted;

        public AMActionTween(List<List<AMActionData>> trackValueSets) {
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
                Reset();
            }
        }

        public float startTime { get { return mStartTime; } }

        public float duration { get { return mDuration; } }

        public void Reset() {
            if(mValueTrackCurIndices != null) {
                for(int i = 0; i < mValueTrackCurIndices.Length; i++) {
                    mValueTrackCurIndices[i] = trackValIndStart;
                }
            }

            mIsStarted = false;
        }

        public override bool ConvertToStartValue(TweenerCore<bool, bool, NoOptions> t, bool value) {
            return value;
        }

        public override void EvaluateAndApply(NoOptions options, Tween tween, bool isRelative, DOGetter<bool> getter, DOSetter<bool> setter, float elapsed, bool startValue, bool changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice) {
            //wait one frame
            if(!mIsStarted) {
                mIsStarted = true;
                return;
            }

            bool isBackwards = getter(); //cheeky
            float t = mStartTime + elapsed;

            for(int i = 0, max = mValueTracks.Length; i < max; i++) {
                int curInd = mValueTrackCurIndices[i];

                //determine if we need to move
                if(curInd == trackValIndStart) {
                    //get the starting act, make sure t is within act's timeframe
                    int newInd = GetValueIndex(mValueTracks[i], t);
                    AMActionData act = mValueTracks[i][newInd];
                    if(t >= act.startTime) {
                        mValueTrackCurIndices[i] = newInd;
                        act.Apply(t - act.startTime, isBackwards); //usingInversePosition?
                    }
                }
                else {
                    int newInd = GetNextValueTrackIndex(mValueTracks[i], curInd, t);
                    if(newInd != curInd) {
                        mValueTrackCurIndices[i] = newInd;
                        AMActionData act = mValueTracks[i][newInd];
                        act.Apply(t - act.startTime, isBackwards);
                    }
                }
            }
        }

        public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, bool changeValue) {
            return 0f;
        }

        public override void Reset(TweenerCore<bool, bool, NoOptions> t) {
            Reset();
        }

        public override void SetChangeValue(TweenerCore<bool, bool, NoOptions> t) {

        }

        public override void SetFrom(TweenerCore<bool, bool, NoOptions> t, bool isRelative) {

        }

        public override void SetRelativeEndValue(TweenerCore<bool, bool, NoOptions> t) {

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

	    public abstract void Apply(float t, bool backwards);
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

	    public override void Apply(float t, bool backwards) {
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

	    public override void Apply(float t, bool backwards) {
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

	    public override void Apply(float t, bool backwards) {
	        mTrans.localRotation = mRot;
	    }
	}

	public class AMActionTransLocalRotEuler : AMActionData {
	    private Transform mTrans;
	    private Vector3 mRot;

	    public AMActionTransLocalRotEuler(AMKey key, int frameRate, Transform target, Vector3 rot)
	        : base(key, frameRate) {
	        mTrans = target;
	        mRot = rot;
	    }

	    public override void Apply(float t, bool backwards) {
	        mTrans.localEulerAngles = mRot;
	    }
	}

	public abstract class AMActionTransLocalRotEulerVal : AMActionData {
	    protected Transform mTrans;
	    protected float mVal;

	    public AMActionTransLocalRotEulerVal(AMKey key, int frameRate, Transform target, float val)
	        : base(key, frameRate) {
	        mTrans = target;
	        mVal = val;
	    }
	}

	public class AMActionTransLocalRotEulerX : AMActionTransLocalRotEulerVal {
	    public AMActionTransLocalRotEulerX(AMKey key, int frameRate, Transform target, float val) : base(key, frameRate, target, val) { }

	    public override void Apply(float t, bool backwards) {
	        Vector3 r = mTrans.localEulerAngles;
	        r.x = mVal;
	        mTrans.localEulerAngles = r;
	    }
	}

	public class AMActionTransLocalRotEulerY : AMActionTransLocalRotEulerVal {
	    public AMActionTransLocalRotEulerY(AMKey key, int frameRate, Transform target, float val) : base(key, frameRate, target, val) { }

	    public override void Apply(float t, bool backwards) {
	        Vector3 r = mTrans.localEulerAngles;
	        r.y = mVal;
	        mTrans.localEulerAngles = r;
	    }
	}

	public class AMActionTransLocalRotEulerZ : AMActionTransLocalRotEulerVal {
	    public AMActionTransLocalRotEulerZ(AMKey key, int frameRate, Transform target, float val) : base(key, frameRate, target, val) { }

	    public override void Apply(float t, bool backwards) {
	        Vector3 r = mTrans.localEulerAngles;
	        r.z = mVal;
	        mTrans.localEulerAngles = r;
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

	    public override void Apply(float t, bool backwards) {
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

	    public override void Apply(float t, bool backwards) {
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

	    public override void Apply(float t, bool backwards) {
	        mField.SetValue(mObj, mVal);
	    }
	}

	public abstract class AMActionMaterial : AMActionData {
	    protected Material mMat;
	    protected int mPropId;

	    public AMActionMaterial(AMKey key, int frameRate, Material mat, string prop)
	        : base(key, frameRate) {
	        mMat = mat;
	        mPropId = Shader.PropertyToID(prop);
	    }
	}

	public abstract class AMActionMaterialPropName : AMActionData {
	    protected Material mMat;
	    protected string mProp;

	    public AMActionMaterialPropName(AMKey key, int frameRate, Material mat, string prop)
	        : base(key, frameRate) {
	        mMat = mat;
	        mProp = prop;
	    }
	}

	public class AMActionMaterialFloatSet : AMActionMaterial {
	    private float mVal;

	    public AMActionMaterialFloatSet(AMKey key, int frameRate, Material mat, string prop, float val)
	        : base(key, frameRate, mat, prop) {
	        mVal = val;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetFloat(mPropId, mVal);
	    }
	}

	public class AMActionMaterialVectorSet : AMActionMaterial {
	    private Vector4 mVal;

	    public AMActionMaterialVectorSet(AMKey key, int frameRate, Material mat, string prop, Vector4 val)
	        : base(key, frameRate, mat, prop) {
	        mVal = val;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetVector(mPropId, mVal);
	    }
	}

	public class AMActionMaterialColorSet : AMActionMaterial {
	    private Color mVal;

	    public AMActionMaterialColorSet(AMKey key, int frameRate, Material mat, string prop, Color val)
	        : base(key, frameRate, mat, prop) {
	        mVal = val;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetVector(mPropId, mVal);
	    }
	}

	public class AMActionMaterialTexSet : AMActionMaterial {
	    private Texture mTex;

	    public AMActionMaterialTexSet(AMKey key, int frameRate, Material mat, string prop, Texture tex)
	        : base(key, frameRate, mat, prop) {
	            mTex = tex;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetTexture(mPropId, mTex);
	    }
	}

	public class AMActionMaterialTexOfsSet : AMActionMaterialPropName {
	    private Vector2 mOfs;

	    public AMActionMaterialTexOfsSet(AMKey key, int frameRate, Material mat, string prop, Vector2 ofs)
	        : base(key, frameRate, mat, prop) {
	        mOfs = ofs;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetTextureOffset(mProp, mOfs);
	    }
	}

	public class AMActionMaterialTexScaleSet : AMActionMaterialPropName {
	    private Vector2 mScale;

	    public AMActionMaterialTexScaleSet(AMKey key, int frameRate, Material mat, string prop, Vector2 s)
	        : base(key, frameRate, mat, prop) {
	        mScale = s;
	    }

	    public override void Apply(float t, bool backwards) {
	        mMat.SetTextureScale(mProp, mScale);
	    }
	}
}
