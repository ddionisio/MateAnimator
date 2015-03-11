using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMMaterialKey : AMKey {
    public int endFrame;

    public Texture texture;

    [SerializeField]
    Vector4 _val4;

    public float val { get { return _val4.x; } set { _val4.x = value; } }
    public Vector2 texOfs { get { return new Vector2(_val4.x, _val4.y); } set { _val4.x = value.x; _val4.y = value.y; } }
    public Vector2 texScale { get { return new Vector2(_val4.z, _val4.w); } set { _val4.z = value.x; _val4.w = value.y; } }
    public Vector4 vector { get { return _val4; } set { _val4 = value; } }
    public Color color { get { return new Color(_val4.x, _val4.y, _val4.z, _val4.w); } set { _val4.Set(value.r, value.g, value.b, value.a); } }

    public static void ApplyValueLerp(AMMaterialTrack.ValueType valueType, string prop, int propId, Material mat, AMMaterialKey fromKey, AMMaterialKey toKey, float t) {
        switch(valueType) {
            case AMMaterialTrack.ValueType.Float:
            case AMMaterialTrack.ValueType.Range:
                mat.SetFloat(propId, Mathf.Lerp(fromKey.val, toKey.val, t));
                break;
            case AMMaterialTrack.ValueType.Vector:
                mat.SetVector(propId, Vector4.Lerp(fromKey.vector, toKey.vector, t));
                break;
            case AMMaterialTrack.ValueType.Color:
                mat.SetColor(propId, Color.Lerp(fromKey.color, toKey.color, t));
                break;
            case AMMaterialTrack.ValueType.TexOfs:
                mat.SetTextureOffset(prop, Vector2.Lerp(fromKey.texOfs, toKey.texOfs, t));
                break;
            case AMMaterialTrack.ValueType.TexScale:
                mat.SetTextureScale(prop, Vector2.Lerp(fromKey.texScale, toKey.texScale, t));
                break;
        }
    }

    public override void CopyTo(AMKey key) {
        AMMaterialKey a = key as AMMaterialKey;
        a.enabled = false;

        a.frame = frame;

        a.texture = texture;
        a._val4 = _val4;
                
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
    }

    public void ApplyValue(AMMaterialTrack.ValueType valueType, string prop, int propId, Material mat) {
        switch(valueType) {
            case AMMaterialTrack.ValueType.Float:
            case AMMaterialTrack.ValueType.Range:
                mat.SetFloat(propId, val);
                break;
            case AMMaterialTrack.ValueType.Vector:
                mat.SetVector(propId, vector);
                break;
            case AMMaterialTrack.ValueType.Color:
                mat.SetColor(propId, color);
                break;
            case AMMaterialTrack.ValueType.TexEnv:
                mat.SetTexture(propId, texture);
                break;
            case AMMaterialTrack.ValueType.TexOfs:
                mat.SetTextureOffset(prop, texOfs);
                break;
            case AMMaterialTrack.ValueType.TexScale:
                mat.SetTextureScale(prop, texScale);
                break;
        }
    }
        
    #region action
    public override int getNumberOfFrames(int frameRate) {
        if(!canTween && (endFrame == -1 || endFrame == frame))
            return 1;
        else if(endFrame == -1)
            return -1;
        return endFrame - frame;
    }

    const string bsField = "endFrame";

    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        AMMaterialTrack matTrack = track as AMMaterialTrack;

        Material matInst = matTrack.materialInstance;
        string prop = matTrack.property;
        AMMaterialTrack.ValueType propType = matTrack.propertyType;

        int frameRate = seq.take.frameRate;

        switch(propType) {
            case AMMaterialTrack.ValueType.Float:
            case AMMaterialTrack.ValueType.Range:
                if(!canTween || matTrack.keys.Count == 1) //allow one key
                    seq.Insert(new AMActionMaterialFloatSet(this, frameRate, matInst, prop, val));
                else {
                    AMMaterialKey endKey = track.keys[index + 1] as AMMaterialKey;
                    if(targetsAreEqual(propType, endKey)) return;

                    if(hasCustomEase())
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialFloat(matInst, prop, endKey.val)).Ease(easeCurve)));
                    else
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialFloat(matInst, prop, endKey.val)).Ease((EaseType)easeType, amplitude, period)));
                }
                break;
            case AMMaterialTrack.ValueType.Vector:
                if(!canTween || matTrack.keys.Count == 1) //allow one key
                    seq.Insert(new AMActionMaterialVectorSet(this, frameRate, matInst, prop, vector));
                else {
                    AMMaterialKey endKey = track.keys[index + 1] as AMMaterialKey;
                    if(targetsAreEqual(propType, endKey)) return;

                    if(hasCustomEase())
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialVector4(matInst, prop, endKey.vector)).Ease(easeCurve)));
                    else
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialVector4(matInst, prop, endKey.vector)).Ease((EaseType)easeType, amplitude, period)));
                }
                break;
            case AMMaterialTrack.ValueType.Color:
                if(!canTween || matTrack.keys.Count == 1) //allow one key
                    seq.Insert(new AMActionMaterialColorSet(this, frameRate, matInst, prop, color));
                else {
                    AMMaterialKey endKey = track.keys[index + 1] as AMMaterialKey;
                    if(targetsAreEqual(propType, endKey)) return;

                    if(hasCustomEase())
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialColor(matInst, prop, endKey.color)).Ease(easeCurve)));
                    else
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialColor(matInst, prop, endKey.color)).Ease((EaseType)easeType, amplitude, period)));
                }
                break;
            case AMMaterialTrack.ValueType.TexOfs:
                if(!canTween || matTrack.keys.Count == 1) //allow one key
                    seq.Insert(new AMActionMaterialTexOfsSet(this, frameRate, matInst, prop, texOfs));
                else {
                    AMMaterialKey endKey = track.keys[index + 1] as AMMaterialKey;
                    if(targetsAreEqual(propType, endKey)) return;

                    if(hasCustomEase())
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialTexOfs(matInst, prop, endKey.texOfs)).Ease(easeCurve)));
                    else
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialTexOfs(matInst, prop, endKey.texOfs)).Ease((EaseType)easeType, amplitude, period)));
                }
                break;
            case AMMaterialTrack.ValueType.TexScale:
                if(!canTween || matTrack.keys.Count == 1) //allow one key
                    seq.Insert(new AMActionMaterialTexScaleSet(this, frameRate, matInst, prop, texScale));
                else {
                    AMMaterialKey endKey = track.keys[index + 1] as AMMaterialKey;
                    if(targetsAreEqual(propType, endKey)) return;

                    if(hasCustomEase())
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialTexScale(matInst, prop, endKey.texScale)).Ease(easeCurve)));
                    else
                        seq.Insert(this, HOTween.To(this, getTime(frameRate), new TweenParms().Prop(bsField, new AMPlugMaterialTexScale(matInst, prop, endKey.texScale)).Ease((EaseType)easeType, amplitude, period)));
                }
                break;
            case AMMaterialTrack.ValueType.TexEnv:
                seq.Insert(new AMActionMaterialTexSet(this, frameRate, matInst, prop, texture));
                break;
        }
    }

    public string getValueString(AMMaterialTrack.ValueType valueType, AMMaterialKey nextKey, bool brief) {
        System.Text.StringBuilder s = new System.Text.StringBuilder();

        switch(valueType) {
            case AMMaterialTrack.ValueType.Float:
            case AMMaterialTrack.ValueType.Range:
                s.Append(formatNumeric(val));
                if(!brief && nextKey) { s.Append(" -> "); s.Append(formatNumeric(nextKey.val)); }
                break;
            case AMMaterialTrack.ValueType.Vector:
                s.Append(_val4.ToString());
                if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey._val4.ToString()); }
                break;
            case AMMaterialTrack.ValueType.Color:
                s.Append(color.ToString());
                if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.color.ToString()); }
                break;
            case AMMaterialTrack.ValueType.TexEnv:
                s.Append(texture ? texture.name : "None");
                break;
            case AMMaterialTrack.ValueType.TexOfs:
                s.Append(texOfs.ToString());
                if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.texOfs.ToString()); }
                break;
            case AMMaterialTrack.ValueType.TexScale:
                s.Append(texScale.ToString());
                if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.texScale.ToString()); }
                break;
        }

        return s.ToString();
    }
    // use for floats
    private string formatNumeric(float input) {
        double _input = (input < 0f ? input * -1f : input);
        if(_input < 1f) {
            if(_input >= 0.01f) return input.ToString("N3");
            else if(_input >= 0.001f) return input.ToString("N4");
            else if(_input >= 0.0001f) return input.ToString("N5");
            else if(_input >= 0.00001f) return input.ToString("N6");
            else return input.ToString();
        }
        return input.ToString("N2");
    }

    public bool targetsAreEqual(AMMaterialTrack.ValueType valueType, AMMaterialKey nextKey) {
        if(nextKey) {
            if(valueType == AMMaterialTrack.ValueType.Float || valueType == AMMaterialTrack.ValueType.Range) return val == nextKey.val;
            if(valueType == AMMaterialTrack.ValueType.Vector) return vector == nextKey.vector;
            if(valueType == AMMaterialTrack.ValueType.Color) return color == nextKey.color;
            if(valueType == AMMaterialTrack.ValueType.TexOfs) return texOfs == nextKey.texOfs;
            if(valueType == AMMaterialTrack.ValueType.TexScale) return texScale == nextKey.texScale;
        }
        return false;
    }

    public object getValue(AMMaterialTrack.ValueType valueType) {
        if(valueType == AMMaterialTrack.ValueType.TexEnv) return texture ? texture : null;
        if(valueType == AMMaterialTrack.ValueType.Vector) return vector;
        if(valueType == AMMaterialTrack.ValueType.Color) return color;
        if(valueType == AMMaterialTrack.ValueType.TexOfs) return texOfs;
        if(valueType == AMMaterialTrack.ValueType.TexScale) return texScale;
        return val;
    }
    #endregion
}
