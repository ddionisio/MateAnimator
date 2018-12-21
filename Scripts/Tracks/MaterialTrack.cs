using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class MaterialTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Material; } }

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

        private Material mMat;//material cache
        private Material[] mMats; //material list cache

        private MaterialController mMatCtrl;
        private int mPropId;
        private bool mIsInit;

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

        public Material materialInstance { get; private set; }

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

        public override bool CheckComponent(GameObject go) {
            return go.GetComponent<Renderer>() != null;
        }

        public override void AddComponent(GameObject go) {
            go.AddComponent<Renderer>();
        }

        public MaterialKey addKey(ITarget target, int _frame) {
            Material mat = GetMaterial(target);
            //mat.get
            //

            MaterialKey k = null;

            foreach(MaterialKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    k = key;
                }
            }

            if(k == null) {
                k = new MaterialKey();
                k.frame = _frame;
                k.interp = Key.Interpolation.Linear;
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
                Debug.LogWarning("Property is not set in: " + name);
            }

            updateCache(target);

            return k;
        }

        public bool hasSamePropertyAs(ITarget target, MaterialTrack _track) {
            if(_track != null && _track.GetTarget(target) == GetTarget(target) && _track.GetMaterial(target) == GetMaterial(target) && _track.getTrackType() == getTrackType())
                return true;
            return false;
        }

        public Material GetMaterial(ITarget itarget) {
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

        public override void updateCache(ITarget target) {
            base.updateCache(target);

            for(int i = 0; i < keys.Count; i++) {
                MaterialKey key = keys[i] as MaterialKey;

                if(key.version > 0 && key.version != version) {
                    //TODO: ...
                }

                key.version = version;

                if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
                else {
                    if(!canTween || (i > 0 && !keys[i - 1].canTween))
                        key.interp = Key.Interpolation.None;

                    key.endFrame = -1;
                }
            }
        }

        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            if(keys == null || keys.Count <= 0) {
                return;
            }

            //TODO: figure out how to preview frame during edit
            if(Application.isPlaying) {
                if(!mIsInit)
                    Init(target);

                // if before or equal to first frame, or is the only frame
                MaterialKey firstKey = keys[0] as MaterialKey;
                if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                    firstKey.ApplyValue(_propertyType, _property, mPropId, materialInstance);
                    return;
                }

                // if lies on property action
                for(int i = 0; i < keys.Count; i++) {
                    MaterialKey key = keys[i] as MaterialKey;
                    MaterialKey keyNext = i + 1 < keys.Count ? keys[i + 1] as MaterialKey : null;

                    if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                    // if no ease
                    if(!key.canTween || keyNext == null) {
                        key.ApplyValue(_propertyType, _property, mPropId, materialInstance);
                        return;
                    }
                    // else find value using easing function

                    float numFrames = (float)key.getNumberOfFrames(frameRate);

                    float framePositionInAction = Mathf.Clamp(frame - (float)key.frame, 0f, numFrames);

                    float t;

                    if(key.hasCustomEase()) {
                        t = Utility.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(frameRate), key.easeCurve);
                    }
                    else {
                        var ease = Utility.GetEasingFunction((Ease)key.easeType);
                        t = ease(framePositionInAction, key.getNumberOfFrames(frameRate), key.amplitude, key.period);
                    }

                    MaterialKey.ApplyValueLerp(_propertyType, _property, mPropId, materialInstance, key, keyNext, t);
                    return;
                }
            }
        }

        public override void buildSequenceStart(SequenceControl sequence) {
            //cache material instance
            Init(sequence.target);
        }

        public override void PlayStart(ITarget itarget, float frame, int frameRate, float animScale) {
            //swap to material instance
            mMatCtrl.Apply(_matInd, GetMaterial(itarget));

            base.PlayStart(itarget, frame, frameRate, animScale);
        }

        public override void PlaySwitch(ITarget itarget) {
            //revert material
            mMatCtrl.Revert(_matInd);
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            Debug.LogError("need implement");
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            Renderer r = GetTarget(target) as Renderer;
            List<GameObject> ls = new List<GameObject>();
            if(r) ls.Add(r.gameObject);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
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

        protected override void DoCopy(Track track) {
            var ntrack = track as MaterialTrack;

            ntrack.obj = obj;
            ntrack._matOverride = _matOverride;
            ntrack._matInd = _matInd;
            ntrack._property = _property;
            ntrack._propertyType = _propertyType;
        }

        void Init(ITarget target) {
            Renderer r = GetTarget(target) as Renderer;
            if(r) {
                Material mat = GetMaterial(r);
                if(mat) {
                    mMatCtrl = r.GetComponent<MaterialController>();
                    if(!mMatCtrl) mMatCtrl = r.gameObject.AddComponent<MaterialController>();
                    materialInstance = mMatCtrl.Instance(_matInd, mat);
                }
                else
                    Debug.LogWarning("Material not found for track: " + name);
            }
            else
                Debug.LogWarning("Renderer not found for track: " + name);

            mPropId = Shader.PropertyToID(_property);

            mIsInit = true;
        }
    }
}