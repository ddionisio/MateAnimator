using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
    [System.Serializable]
    public class MaterialKey : PathKeyBase {
        public override SerializeType serializeType { get { return SerializeType.Material; } }

        public Texture texture;

        [SerializeField]
        Vector4 _val4;

        public float val { get { return _val4.x; } set { _val4.x = value; } }
        public Vector2 texOfs { get { return new Vector2(_val4.x, _val4.y); } set { _val4.x = value.x; _val4.y = value.y; } }
        public Vector2 texScale { get { return new Vector2(_val4.z, _val4.w); } set { _val4.z = value.x; _val4.w = value.y; } }
        public Vector4 vector { get { return _val4; } set { _val4 = value; } }
        public Color color { get { return new Color(_val4.x, _val4.y, _val4.z, _val4.w); } set { _val4.Set(value.r, value.g, value.b, value.a); } }

        public static void ApplyValueLerp(MaterialTrack.ValueType valueType, string prop, int propId, Material mat, MaterialKey fromKey, MaterialKey toKey, float t) {
            switch(valueType) {
                case MaterialTrack.ValueType.Float:
                case MaterialTrack.ValueType.Range:
                    mat.SetFloat(propId, Mathf.Lerp(fromKey.val, toKey.val, t));
                    break;
                case MaterialTrack.ValueType.Vector:
                    mat.SetVector(propId, Vector4.Lerp(fromKey.vector, toKey.vector, t));
                    break;
                case MaterialTrack.ValueType.Color:
                    mat.SetColor(propId, Color.Lerp(fromKey.color, toKey.color, t));
                    break;
                case MaterialTrack.ValueType.TexOfs:
                    mat.SetTextureOffset(prop, Vector2.Lerp(fromKey.texOfs, toKey.texOfs, t));
                    break;
                case MaterialTrack.ValueType.TexScale:
                    mat.SetTextureScale(prop, Vector2.Lerp(fromKey.texScale, toKey.texScale, t));
                    break;
            }
        }

        public static void ApplyValuePath(MaterialTrack.ValueType valueType, string prop, int propId, Material mat, MaterialKey key, float t) {
            float finalT;

            if(key.hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, key.easeCurve);
            else {
                var ease = Utility.GetEasingFunction(key.easeType);
                finalT = ease(t, 1f, key.amplitude, key.period);
                if(float.IsNaN(finalT)) { //this really shouldn't happen...
                    key.ApplyValue(valueType, prop, propId, mat);
                    return;
                }
            }

            var pt = key.path.GetPoint(finalT);

            var val = key.GetValueFromPathPoint(valueType, pt);
            if(val != null) {
                switch(valueType) {
                    case MaterialTrack.ValueType.Float:
                    case MaterialTrack.ValueType.Range:
                        mat.SetFloat(propId, (float)val);
                        break;
                    case MaterialTrack.ValueType.Vector:
                        mat.SetVector(propId, (Vector4)val);
                        break;
                    case MaterialTrack.ValueType.Color:
                        mat.SetColor(propId, (Color)val);
                        break;
                    case MaterialTrack.ValueType.TexOfs:
                        mat.SetTextureOffset(prop, (Vector2)val);
                        break;
                    case MaterialTrack.ValueType.TexScale:
                        mat.SetTextureScale(prop, (Vector2)val);
                        break;
                }
            }
        }

        protected override TweenPlugPathPoint GeneratePathPoint(Track track) {
            switch(((MaterialTrack)track).propertyType) {
                case MaterialTrack.ValueType.Color:
                    return new TweenPlugPathPoint(color);
                case MaterialTrack.ValueType.Vector:
                    return new TweenPlugPathPoint(vector);
                case MaterialTrack.ValueType.Float:
                case MaterialTrack.ValueType.Range:
                    return new TweenPlugPathPoint(val);
                case MaterialTrack.ValueType.TexOfs:
                    return new TweenPlugPathPoint(texOfs);
                case MaterialTrack.ValueType.TexScale:
                    return new TweenPlugPathPoint(texScale);
            }

            return new TweenPlugPathPoint(0);
        }

        public object GetValueFromPathPoint(MaterialTrack.ValueType valueType, TweenPlugPathPoint pt) {
            switch(valueType) {
                case MaterialTrack.ValueType.Color:
                    return pt.valueColor;
                case MaterialTrack.ValueType.Vector:
                    return pt.valueVector4;
                case MaterialTrack.ValueType.Float:
                case MaterialTrack.ValueType.Range:
                    return pt.valueFloat;
                case MaterialTrack.ValueType.TexOfs:
                case MaterialTrack.ValueType.TexScale:
                    return pt.valueVector2;
            }

            return null;
        }

        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as MaterialKey;

            a.texture = texture;
            a._val4 = _val4;
        }

        public void ApplyValue(MaterialTrack.ValueType valueType, string prop, int propId, Material mat) {
            switch(valueType) {
                case MaterialTrack.ValueType.Float:
                case MaterialTrack.ValueType.Range:
                    mat.SetFloat(propId, val);
                    break;
                case MaterialTrack.ValueType.Vector:
                    mat.SetVector(propId, vector);
                    break;
                case MaterialTrack.ValueType.Color:
                    mat.SetColor(propId, color);
                    break;
                case MaterialTrack.ValueType.TexEnv:
                    mat.SetTexture(propId, texture);
                    break;
                case MaterialTrack.ValueType.TexOfs:
                    mat.SetTextureOffset(prop, texOfs);
                    break;
                case MaterialTrack.ValueType.TexScale:
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

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(endFrame == -1) return;
            }

            var matTrack = track as MaterialTrack;
            var propType = matTrack.propertyType;

            Material matInst = matTrack.materialInstance;

            string prop = matTrack.property;
            int propId = Shader.PropertyToID(prop);

            int frameRate = seq.take.frameRate;

            Tweener tween = null;

            int keyCount = track.keys.Count;            
            float time = getTime(frameRate);

            if(interp == Interpolation.None) {
                switch(propType) {
                    case MaterialTrack.ValueType.Float:
                    case MaterialTrack.ValueType.Range:
                        tween = DOTween.To(TweenPlugValueSet<float>.Get(), () => matInst.GetFloat(propId), (x) => matInst.SetFloat(propId, x), val, time);
                        break;
                    case MaterialTrack.ValueType.Vector:
                        tween = DOTween.To(TweenPlugValueSet<Vector4>.Get(), () => matInst.GetVector(propId), (x) => matInst.SetVector(propId, x), vector, time);
                        break;
                    case MaterialTrack.ValueType.Color:
                        tween = DOTween.To(TweenPlugValueSet<Color>.Get(), () => matInst.GetColor(propId), (x) => matInst.SetColor(propId, x), color, time);
                        break;
                    case MaterialTrack.ValueType.TexOfs:
                        tween = DOTween.To(TweenPlugValueSet<Vector2>.Get(), () => matInst.GetTextureOffset(prop), (x) => matInst.SetTextureOffset(prop, x), texOfs, time);
                        break;
                    case MaterialTrack.ValueType.TexScale:
                        tween = DOTween.To(TweenPlugValueSet<Vector2>.Get(), () => matInst.GetTextureScale(prop), (x) => matInst.SetTextureScale(prop, x), texScale, time);
                        break;
                    case MaterialTrack.ValueType.TexEnv:
                        tween = DOTween.To(TweenPlugValueSet<Texture>.Get(), () => matInst.GetTexture(propId), (x) => matInst.SetTexture(propId, x), texture, time);
                        break;
                }
            }
            else if(interp == Interpolation.Linear || path == null) {
                var endKey = track.keys[index + 1] as MaterialKey;

                switch(propType) {
                    case MaterialTrack.ValueType.Float:
                    case MaterialTrack.ValueType.Range:
                        tween = DOTween.To(TweenPluginFactory.CreateFloat(), () => val, (x) => matInst.SetFloat(propId, x), endKey.val, time);
                        break;
                    case MaterialTrack.ValueType.Vector:
                        tween = DOTween.To(TweenPluginFactory.CreateVector4(), () => vector, (x) => matInst.SetVector(propId, x), endKey.vector, time);
                        break;
                    case MaterialTrack.ValueType.Color:
                        tween = DOTween.To(TweenPluginFactory.CreateColor(), () => color, (x) => matInst.SetColor(propId, x), endKey.color, time);
                        break;
                    case MaterialTrack.ValueType.TexOfs:
                        tween = DOTween.To(TweenPluginFactory.CreateVector2(), () => texOfs, (x) => matInst.SetTextureOffset(prop, x), endKey.texOfs, time);
                        break;
                    case MaterialTrack.ValueType.TexScale:
                        tween = DOTween.To(TweenPluginFactory.CreateVector2(), () => texScale, (x) => matInst.SetTextureScale(prop, x), endKey.texScale, time);
                        break;
                }
            }
            else {
                //options
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart };

                switch(propType) {
                    case MaterialTrack.ValueType.Float:
                    case MaterialTrack.ValueType.Range: {
                            var tweenPath = DOTween.To(TweenPlugPathFloat.Get(), () => val, (x) => matInst.SetFloat(propId, x), path, time);
                            tweenPath.plugOptions = options; tween = tweenPath;
                        }
                        break;
                    case MaterialTrack.ValueType.Vector: {
                            var tweenPath = DOTween.To(TweenPlugPathVector4.Get(), () => vector, (x) => matInst.SetVector(propId, x), path, time);
                            tweenPath.plugOptions = options; tween = tweenPath;
                        }
                        break;
                    case MaterialTrack.ValueType.Color: {
                            var tweenPath = DOTween.To(TweenPlugPathColor.Get(), () => color, (x) => matInst.SetColor(propId, x), path, time);
                            tweenPath.plugOptions = options; tween = tweenPath;
                        }
                        break;
                    case MaterialTrack.ValueType.TexOfs: {
                            var tweenPath = DOTween.To(TweenPlugPathVector2.Get(), () => texOfs, (x) => matInst.SetTextureOffset(propId, x), path, time);
                            tweenPath.plugOptions = options; tween = tweenPath;
                        }
                        break;
                    case MaterialTrack.ValueType.TexScale: {
                            var tweenPath = DOTween.To(TweenPlugPathVector2.Get(), () => texScale, (x) => matInst.SetTextureScale(propId, x), path, time);
                            tweenPath.plugOptions = options; tween = tweenPath;
                        }
                        break;
                }
            }

            if(tween != null) {
                if(canTween) {
                    if(hasCustomEase())
                        tween.SetEase(easeCurve);
                    else
                        tween.SetEase((Ease)easeType, amplitude, period);
                }

                seq.Insert(this, tween);
            }
        }

        public string getValueString(MaterialTrack.ValueType valueType, MaterialKey nextKey, bool brief) {
            System.Text.StringBuilder s = new System.Text.StringBuilder();

            switch(valueType) {
                case MaterialTrack.ValueType.Float:
                case MaterialTrack.ValueType.Range:
                    s.Append(formatNumeric(val));
                    if(!brief && nextKey != null) { s.Append(" -> "); s.Append(formatNumeric(nextKey.val)); }
                    break;
                case MaterialTrack.ValueType.Vector:
                    s.Append(_val4.ToString());
                    if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey._val4.ToString()); }
                    break;
                case MaterialTrack.ValueType.Color:
                    s.Append(color.ToString());
                    if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.color.ToString()); }
                    break;
                case MaterialTrack.ValueType.TexEnv:
                    s.Append(texture ? texture.name : "None");
                    break;
                case MaterialTrack.ValueType.TexOfs:
                    s.Append(texOfs.ToString());
                    if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.texOfs.ToString()); }
                    break;
                case MaterialTrack.ValueType.TexScale:
                    s.Append(texScale.ToString());
                    if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.texScale.ToString()); }
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

        public bool targetsAreEqual(MaterialTrack.ValueType valueType, MaterialKey nextKey) {
            if(nextKey != null) {
                if(valueType == MaterialTrack.ValueType.Float || valueType == MaterialTrack.ValueType.Range) return val == nextKey.val;
                if(valueType == MaterialTrack.ValueType.Vector) return vector == nextKey.vector;
                if(valueType == MaterialTrack.ValueType.Color) return color == nextKey.color;
                if(valueType == MaterialTrack.ValueType.TexOfs) return texOfs == nextKey.texOfs;
                if(valueType == MaterialTrack.ValueType.TexScale) return texScale == nextKey.texScale;
            }

            return true;
        }

        public object getValue(MaterialTrack.ValueType valueType) {
            if(valueType == MaterialTrack.ValueType.TexEnv) return texture ? texture : null;
            if(valueType == MaterialTrack.ValueType.Vector) return vector;
            if(valueType == MaterialTrack.ValueType.Color) return color;
            if(valueType == MaterialTrack.ValueType.TexOfs) return texOfs;
            if(valueType == MaterialTrack.ValueType.TexScale) return texScale;
            return val;
        }
        #endregion
    }
}