using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
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
            base.CopyTo(key);

            AMMaterialKey a = key as AMMaterialKey;

            a.texture = texture;
	        a._val4 = _val4;
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
            AMMaterialTrack.ValueType propType = matTrack.propertyType;

	        Material matInst = matTrack.materialInstance;

	        string prop = matTrack.property;
            int propId = Shader.PropertyToID(prop);

	        int frameRate = seq.take.frameRate;

            Tweener tween = null;

            int keyCount = track.keys.Count;
            AMMaterialKey endKey = index + 1 < keyCount ? track.keys[index + 1] as AMMaterialKey : null;
            float frameCount = endKey != null ? endKey.frame - frame + 1 : 1f;

	        switch(propType) {
	            case AMMaterialTrack.ValueType.Float:
	            case AMMaterialTrack.ValueType.Range:
                    if(!canTween || keyCount == 1) {//allow one key
                        var setTween = DOTween.To(new AMPlugValueSet<float>(), () => val, (x) => matInst.SetFloat(propId, x), val, frameCount/frameRate);
                        setTween.plugOptions.SetSequence(seq);
                        seq.Insert(this, setTween);
                    }
                    else {
                        if(targetsAreEqual(propType, endKey)) return;

                        tween = DOTween.To(new FloatPlugin(), () => matInst.GetFloat(propId), (x) => matInst.SetFloat(propId, x), endKey.val, getTime(frameRate));
                    }
	                break;
	            case AMMaterialTrack.ValueType.Vector:
                    if(!canTween || keyCount == 1) {//allow one key
                        var setTween = DOTween.To(new AMPlugValueSet<Vector4>(), () => vector, (x) => matInst.SetVector(propId, x), vector, frameCount/frameRate);
                        setTween.plugOptions.SetSequence(seq);
                        seq.Insert(this, setTween);
                    }
                    else {
                        if(targetsAreEqual(propType, endKey)) return;

                        tween = DOTween.To(AMPluginFactory.CreateVector4(), () => matInst.GetVector(propId), (x) => matInst.SetVector(propId, x), endKey.vector, getTime(frameRate));
                    }
	                break;
	            case AMMaterialTrack.ValueType.Color:
                    if(!canTween || keyCount == 1) {//allow one key
                        var val = color;
                        var setTween = DOTween.To(new AMPlugValueSet<Color>(), () => val, (x) => matInst.SetColor(propId, x), val, frameCount/frameRate);
                        setTween.plugOptions.SetSequence(seq);
                        seq.Insert(this, setTween);
                    }
                    else {
                        if(targetsAreEqual(propType, endKey)) return;

                        tween = DOTween.To(AMPluginFactory.CreateColor(), () => matInst.GetColor(propId), (x) => matInst.SetColor(propId, x), endKey.color, getTime(frameRate));
                    }
	                break;
	            case AMMaterialTrack.ValueType.TexOfs:
                    if(!canTween || keyCount == 1) {//allow one key
                        var val = texOfs;
                        var setTween = DOTween.To(new AMPlugValueSet<Vector2>(), () => val, (x) => matInst.SetTextureOffset(prop, x), val, frameCount/frameRate);
                        setTween.plugOptions.SetSequence(seq);
                        seq.Insert(this, setTween);
                    }
                    else {
                        if(targetsAreEqual(propType, endKey)) return;

                        tween = DOTween.To(AMPluginFactory.CreateVector2(), () => matInst.GetTextureOffset(prop), (x) => matInst.SetTextureOffset(prop, x), endKey.texOfs, getTime(frameRate));
                    }
	                break;
	            case AMMaterialTrack.ValueType.TexScale:
                    if(!canTween || keyCount == 1) {//allow one key
                        var val = texScale;
                        var setTween = DOTween.To(new AMPlugValueSet<Vector2>(), () => val, (x) => matInst.SetTextureScale(prop, x), val, frameCount/frameRate);
                        setTween.plugOptions.SetSequence(seq);
                        seq.Insert(this, setTween);
                    }
                    else {
                        if(targetsAreEqual(propType, endKey)) return;

                        tween = DOTween.To(AMPluginFactory.CreateVector2(), () => matInst.GetTextureScale(prop), (x) => matInst.SetTextureScale(prop, x), endKey.texScale, getTime(frameRate));
                    }
	                break;
	            case AMMaterialTrack.ValueType.TexEnv:
                    var texEnvTween = DOTween.To(new AMPlugValueSet<Texture>(), () => texture, (x) => matInst.SetTexture(propId, x), texture, frameCount/frameRate);
                    texEnvTween.plugOptions.SetSequence(seq);
                    seq.Insert(this, texEnvTween);
	                break;
	        }

            if(tween != null) {
                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase((Ease)easeType, amplitude, period);

                seq.Insert(this, tween);
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

            return true;
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
}