using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using Holoville.HOTween.Core;
using Holoville.HOTween;

[AddComponentMenu("")]
public class AMMaterialTrack : AMTrack {
    public enum ValueType {
        None = -1,
        Color,
        Vector,
        Float,
        Range,
        TexEnv,
        TexOfs,
        TexScale
    }

    [SerializeField]
    Renderer obj;

    [SerializeField]
    Material _matOverride;

    [SerializeField]
    int _matInd = -1;

    [SerializeField]
    string _property;
    [SerializeField]
    ValueType _propertyType = ValueType.None;

    Material mMat;//material cache
    Material[] mMats; //material list cache

    Material mMatInstance;
    AMMaterialController mMatCtrl;
    int mPropId;
    bool mIsInit;

    public override int version { get { return 1; } }

    public Material materialOverride {
        get {
            return _matOverride;
        }
        set {
            _matOverride = value;

            mMat = null;
            mMats = null;

            //verify
        }
    }

    public int materialIndex {
        get { return _matInd; }
        set { _matInd = value; }
    }

    public string property {
        get { return _property; }
        set { _property = value; }
    }

    public ValueType propertyType {
        get { return _propertyType; }
        set { _propertyType = value; }
    }

    public Material materialInstance {
        get { return mMatInstance; }
    }

    protected override void SetSerializeObject(UnityEngine.Object obj) {
        this.obj = obj as Renderer;

        mMat = null;
        mMats = null;

        //verify
    }

    protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
        return targetGO ? targetGO.GetComponent<Renderer>() : obj;
    }

    public override bool canTween {
        get {
            return _propertyType != ValueType.TexEnv;
        }
    }

    public override string getTrackType() {
        return _matInd != -1 && !string.IsNullOrEmpty(_property) && _propertyType != ValueType.None ? _property : "Not Set";
    }

    public override System.Type GetRequiredComponent() {
        return typeof(Renderer);
    }

    public AMMaterialKey addKey(AMITarget target, OnAddKey addCall, int _frame) {
        Material mat = GetMaterial(target);
        //mat.get
        //

        AMMaterialKey k = null;

        foreach(AMMaterialKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                k = key;
            }
        }

        if(k == null) {
            k = addCall(gameObject, typeof(AMMaterialKey)) as AMMaterialKey;
            k.frame = _frame;
            k.interp = (int)AMKey.Interpolation.Linear;
            // add a new key
            keys.Add(k);
        }

        //set value
        if(!string.IsNullOrEmpty(property)) {
            switch(_propertyType) {
                case ValueType.Float:
                case ValueType.Range:
                    k.val = mat.GetFloat(property);
                    break;
                case ValueType.Vector:
                    k.vector = mat.GetVector(property);
                    break;
                case ValueType.Color:
                    k.color = mat.GetColor(property);
                    break;
                case ValueType.TexEnv:
                    k.texture = mat.GetTexture(property);
                    break;
                case ValueType.TexOfs:
                    k.texOfs = mat.GetTextureOffset(property);
                    break;
                case ValueType.TexScale:
                    k.texScale = mat.GetTextureScale(property);
                    break;
            }
        }
        else {
            Debug.LogWarning("Property is not set in: "+name);
        }

        updateCache(target);

        return k;
    }

    public bool hasSamePropertyAs(AMITarget target, AMMaterialTrack _track) {
        if(_track.GetTarget(target) == GetTarget(target) && _track.GetMaterial(target) == GetMaterial(target) && _track.getTrackType() == getTrackType())
            return true;
        return false;
    }

    public Material GetMaterial(AMITarget itarget) {
        if(!mMat) {
            if(_matOverride)
                mMat = _matOverride;

            Renderer r = GetTarget(itarget) as Renderer;
            if(r)
                mMat = GetMaterial(r);
        }

        return mMat;
    }

    private Material GetMaterial(Renderer r) {
        if(mMats == null || mMats.Length == 0) mMats = r.sharedMaterials;

        if(_matInd < 0 || _matInd >= mMats.Length || mMats[_matInd] == null) {
            for(int i = 0; i < mMats.Length; i++) {
                if(mMats[i]) {
                    _matInd = i;
                    break;
                }
            }
        }

        return mMats[_matInd];
    }

    public override void updateCache(AMITarget target) {
        base.updateCache(target);

        for(int i = 0; i < keys.Count; i++) {
            AMMaterialKey key = keys[i] as AMMaterialKey;

            if(key.version > 0 && key.version != version) {
                //TODO: ...
            }

            key.version = version;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else {
                if(!canTween || (i > 0 && !keys[i-1].canTween))
                    key.interp = (int)AMKey.Interpolation.None;

                key.endFrame = -1;
            }
        }
    }

    public override void previewFrame(AMITarget target, float frame, int frameRate, bool play, float playSpeed) {
        if(keys == null || keys.Count <= 0) {
            return;
        }

        //TODO: figure out how to preview frame during edit
        if(Application.isPlaying) {
            if(!mIsInit)
                Init(target);

            // if before or equal to first frame, or is the only frame
            AMMaterialKey ckey = keys[0] as AMMaterialKey;
            if((frame <= (float)ckey.frame) || ckey.endFrame == -1) {
                ckey.ApplyValue(_propertyType, _property, mPropId, mMatInstance);
                return;
            }
            // if not tweenable and beyond last frame
            ckey = keys[keys.Count - 1] as AMMaterialKey;
            if(!canTween && frame >= (float)ckey.frame) {
                ckey.ApplyValue(_propertyType, _property, mPropId, mMatInstance);
                return;
            }
            //if tweenable and beyond last tweenable
            ckey = keys[keys.Count - 2] as AMMaterialKey;
            if(frame >= (float)ckey.endFrame) {
                ckey.ApplyValue(_propertyType, _property, mPropId, mMatInstance);
                return;
            }
            // if lies on property action
            for(int i = 0; i < keys.Count; i++) {
                AMMaterialKey key = keys[i] as AMMaterialKey;
                AMMaterialKey keyNext = i + 1 < keys.Count ? keys[i + 1] as AMMaterialKey : null;

                if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
                //if(quickPreview && !key.targetsAreEqual()) return;	// quick preview; if action will execute then skip
                // if on startFrame or is no tween
                if(frame == (float)key.frame || ((!key.canTween || !canTween) && frame < (float)key.endFrame)) {
                    key.ApplyValue(_propertyType, _property, mPropId, mMatInstance);
                    return;
                }
                // if on endFrame
                if(frame == (float)key.endFrame) {
                    if(!key.canTween || !canTween || !keyNext)
                        continue;
                    else {
                        keyNext.ApplyValue(_propertyType, _property, mPropId, mMatInstance);
                        return;
                    }
                }
                // else find value using easing function

                float framePositionInAction = frame - (float)key.frame;
                if(framePositionInAction < 0f) framePositionInAction = 0f;

                float t;

                if(key.hasCustomEase()) {
                    t = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(frameRate), key.easeCurve);
                }
                else {
                    TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                    t = ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), key.amplitude, key.period);
                }

                AMMaterialKey.ApplyValueLerp(_propertyType, _property, mPropId, mMatInstance, key, keyNext, t);
                return;
            }
        }
    }

    public override void buildSequenceStart(AMSequence sequence) {
        //cache material instance
        Init(sequence.target);
    }

    public override void PlayStart(AMITarget itarget, float frame, int frameRate, float animScale) {
        //swap to material instance
        mMatCtrl.Apply(_matInd, GetMaterial(itarget));

        base.PlayStart(itarget, frame, frameRate, animScale);
    }

    public override void PlaySwitch(AMITarget itarget) {
        //revert material
        mMatCtrl.Revert(_matInd);
    }

    public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        Debug.LogError("need implement");
        return null;
    }

    public override List<GameObject> getDependencies(AMITarget target) {
        Renderer r = GetTarget(target) as Renderer;
        List<GameObject> ls = new List<GameObject>();
        if(r) ls.Add(r.gameObject);
        return ls;
    }

    public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
        GameObject go = GetTarget(target) as GameObject;
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        if(!go) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == go) {
                Component _component = newReferences[i].GetComponent("Renderer");

                // missing component
                if(!_component) {
                    Debug.LogWarning("Animator: Material Track component 'Renderer' not found on new reference for GameObject '" + go.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
                // missing property

                SetTarget(target, newReferences[i].transform);
                break;
            }
        }
        return lsFlagToKeep;
    }

    protected override void DoCopy(AMTrack track) {
        AMMaterialTrack ntrack = track as AMMaterialTrack;

        ntrack.obj = obj;
        ntrack._matOverride = _matOverride;
        ntrack._matInd = _matInd;
        ntrack._property = _property;
        ntrack._propertyType = _propertyType;
    }

    void Init(AMITarget target) {
        Renderer r = GetTarget(target) as Renderer;
        if(r) {
            Material mat = GetMaterial(r);
            if(mat) {
                mMatCtrl = r.GetComponent<AMMaterialController>();
                if(!mMatCtrl) mMatCtrl = r.gameObject.AddComponent<AMMaterialController>();
                mMatInstance = mMatCtrl.Instance(_matInd, mat);
            }
            else
                Debug.LogWarning("Material not found for track: "+name);
        }
        else
            Debug.LogWarning("Renderer not found for track: "+name);

        mPropId = Shader.PropertyToID(_property);

        mIsInit = true;
    }
}
