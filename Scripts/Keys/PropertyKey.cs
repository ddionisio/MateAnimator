﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.CustomPlugins;
using DG.Tweening.Plugins;

namespace M8.Animator {
    [System.Serializable]
    public class PropertyKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Property; } }

        public const int pathResolution = 10;

        public int endFrame;

        //union for single values
        public double val;	// value as double
        public string valString; //string
        public UnityEngine.Object valObj;

        public bool valb { get { return val > 0.0; } set { val = value ? 1.0 : -1.0; } }

        //union for vectors, color, rect
        public Vector4 vect4;
        public Vector2 vect2 { get { return new Vector2(vect4.x, vect4.y); } set { vect4.Set(value.x, value.y, 0, 0); } }
        public Vector3 vect3 { get { return new Vector3(vect4.x, vect4.y, vect4.z); } set { vect4.Set(value.x, value.y, value.z, 0); } }
        public Color color { get { return new Color(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.r, value.g, value.b, value.a); } }
        public Rect rect { get { return new Rect(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.xMin, value.yMin, value.width, value.height); } }
        public Quaternion quat { get { return new Quaternion(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.x, value.y, value.z, value.w); } }

        public TweenPlugPathPoint[] path;

        public bool isClosed { get { return path.Length > 1 && path[0].Equal(path[path.Length - 1]); } }

        private TweenPlugPath mPathPreview;

        private TweenPlugPathPoint GeneratePathPoint(PropertyTrack.ValueType valueType) {
            switch(valueType) {
                case PropertyTrack.ValueType.Float:
                case PropertyTrack.ValueType.Integer:
                case PropertyTrack.ValueType.Long:
                case PropertyTrack.ValueType.Double:
                    return new TweenPlugPathPoint(System.Convert.ToSingle(val));

                case PropertyTrack.ValueType.Vector2:
                    return new TweenPlugPathPoint(vect2);
                case PropertyTrack.ValueType.Vector3:
                    return new TweenPlugPathPoint(vect3);
                case PropertyTrack.ValueType.Vector4:
                    return new TweenPlugPathPoint(vect4);

                case PropertyTrack.ValueType.Color:
                    return new TweenPlugPathPoint(color);

                case PropertyTrack.ValueType.Rect:
                    return new TweenPlugPathPoint(rect);

                case PropertyTrack.ValueType.Quaternion: //TODO: quaternion is treated as euler angles
                    return new TweenPlugPathPoint(quat.eulerAngles);
            }

            return new TweenPlugPathPoint(0);
        }

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(PropertyTrack track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    path = new TweenPlugPathPoint[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    path = new TweenPlugPathPoint[0];

                    if(keyInd + 1 < track.keys.Count)
                        endFrame = track.keys[keyInd + 1].frame;
                    else //fail-safe
                        endFrame = -1;
                    break;

                case Interpolation.Curve:
                    var pathList = new List<TweenPlugPathPoint>();

                    for(int i = keyInd; i < track.keys.Count; i++) {
                        var key = (PropertyKey)track.keys[i];

                        pathList.Add(key.GeneratePathPoint(track.valueType));
                        endFrame = key.frame;

                        if(key.interp != Interpolation.Curve)
                            break;
                    }

                    if(pathList.Count > 1) {
                        if(pathList.Count > 2)
                            path = pathList.ToArray();
                        else
                            path = new TweenPlugPathPoint[0]; //use linear mode
                    }
                    else {
                        endFrame = -1;
                        path = new TweenPlugPathPoint[0];
                    }
                    break;
            }
        }

        public void ClearCache() {
            mPathPreview = null;
        }

        public object GetValueFromPathPoint(PropertyTrack.ValueType valueType, TweenPlugPathPoint pt) {
            switch(valueType) {
                case PropertyTrack.ValueType.Float:
                    return pt.valueFloat;
                case PropertyTrack.ValueType.Integer:
                    return Mathf.RoundToInt(pt.valueFloat);
                case PropertyTrack.ValueType.Long:
                    return System.Convert.ToInt64(Mathf.RoundToInt(pt.valueFloat));
                case PropertyTrack.ValueType.Double:
                    return System.Convert.ToDouble(pt.valueFloat);

                case PropertyTrack.ValueType.Vector2:
                    return pt.valueVector2;
                case PropertyTrack.ValueType.Vector3:
                    return pt.valueVector3;
                case PropertyTrack.ValueType.Vector4:
                    return pt.valueVector4;

                case PropertyTrack.ValueType.Color:
                    return pt.valueColor;

                case PropertyTrack.ValueType.Rect:
                    return pt.valueRect;

                case PropertyTrack.ValueType.Quaternion: //TODO: quaternion is treated as euler angles
                    return Quaternion.Euler(pt.valueVector3);
            }

            return null;
        }

        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public object GetValueFromPath(PropertyTrack.ValueType valueType, float t) {

            if(mPathPreview == null) {
                if(path.Length <= 1) //invalid path
                    return getValue(valueType);

                mPathPreview = new TweenPlugPath(path.Length > 2 ? TweenPlugPathType.CatmullRom : TweenPlugPathType.Linear, path, pathResolution);
                mPathPreview.Init(isClosed);
            }

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return getValue(valueType);
            }

            var pt = mPathPreview.GetPoint(finalT, true);

            return GetValueFromPathPoint(valueType, pt);
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as PropertyKey;

            a.val = val;
            a.valObj = valObj;
            a.valString = valString;
            a.vect4 = vect4;
        }

        //use in preview for specific refresh after setting the property with given obj
        //e.g. Sprite
        /*public void refresh(GameObject go, object obj) {
            if(valueType == (int)PropertyTrack.ValueType.Sprite) {
                SpriteRenderer sr = component ? component as SpriteRenderer : go.GetComponent<SpriteRenderer>();
                sr.sprite = obj as Sprite;
            }
        }*/

        #region action
        public override int getNumberOfFrames(int frameRate) {
            if(!canTween && (endFrame == -1 || endFrame == frame))
                return 1;
            else if(endFrame == -1)
                return -1;
            return endFrame - frame;
        }

        DG.Tweening.Core.DOSetter<T> GenerateSetter<T>(PropertyTrack propTrack, Component comp) {
            PropertyInfo propInfo = propTrack.GetCachedPropertyInfo();
            if(propInfo != null) {
                return delegate (T x) { propInfo.SetValue(comp, x, null); };
            }

            FieldInfo fieldInfo = propTrack.GetCachedFieldInfo();
            if(fieldInfo != null) {
                return delegate (T x) { fieldInfo.SetValue(comp, x); };
            }

            return null;
        }

        DG.Tweening.Core.DOGetter<T> GenerateGetter<T>(PropertyTrack propTrack, Component comp) {
            PropertyInfo propInfo = propTrack.GetCachedPropertyInfo();
            if(propInfo != null) {
                return delegate () { return (T)propInfo.GetValue(comp, null); };
            }

            FieldInfo fieldInfo = propTrack.GetCachedFieldInfo();
            if(fieldInfo != null) {
                return delegate () { return (T)fieldInfo.GetValue(comp); };
            }

            return null;
        }

        Tweener GenerateSingleValueTweener(SequenceControl seq, PropertyTrack propTrack, float time, Component comp) {
            switch(propTrack.valueType) {
                case PropertyTrack.ValueType.Integer: {
                        int _val = System.Convert.ToInt32(val);
                        var tween = DOTween.To(new TweenPlugValueSet<int>(), GenerateGetter<int>(propTrack, comp), GenerateSetter<int>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Float: {
                        float _val = System.Convert.ToSingle(val);
                        var tween = DOTween.To(new TweenPlugValueSet<float>(), GenerateGetter<float>(propTrack, comp), GenerateSetter<float>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Double: {
                        var tween = DOTween.To(new TweenPlugValueSet<double>(), GenerateGetter<double>(propTrack, comp), GenerateSetter<double>(propTrack, comp), val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Long: {
                        long _val = System.Convert.ToInt64(val);
                        var tween = DOTween.To(new TweenPlugValueSet<long>(), GenerateGetter<long>(propTrack, comp), GenerateSetter<long>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Vector2: {
                        Vector2 _val = vect2;
                        var tween = DOTween.To(new TweenPlugValueSet<Vector2>(), GenerateGetter<Vector2>(propTrack, comp), GenerateSetter<Vector2>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Vector3: {
                        Vector3 _val = vect3;
                        var tween = DOTween.To(new TweenPlugValueSet<Vector3>(), GenerateGetter<Vector3>(propTrack, comp), GenerateSetter<Vector3>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Color: {
                        Color _val = color;
                        var tween = DOTween.To(new TweenPlugValueSet<Color>(), GenerateGetter<Color>(propTrack, comp), GenerateSetter<Color>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Rect: {
                        Rect _val = rect;
                        var tween = DOTween.To(new TweenPlugValueSet<Rect>(), GenerateGetter<Rect>(propTrack, comp), GenerateSetter<Rect>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Vector4: {
                        var tween = DOTween.To(new TweenPlugValueSet<Vector4>(), GenerateGetter<Vector4>(propTrack, comp), GenerateSetter<Vector4>(propTrack, comp), vect4, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Quaternion: {
                        Quaternion _val = quat;
                        var tween = DOTween.To(new TweenPlugValueSet<Quaternion>(), GenerateGetter<Quaternion>(propTrack, comp), GenerateSetter<Quaternion>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Bool: {
                        bool _val = valb;
                        var tween = DOTween.To(new TweenPlugValueSet<bool>(), GenerateGetter<bool>(propTrack, comp), GenerateSetter<bool>(propTrack, comp), _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.String: {
                        var tween = DOTween.To(new TweenPlugValueSet<string>(), GenerateGetter<string>(propTrack, comp), GenerateSetter<string>(propTrack, comp), valString, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Sprite: {
                        var spriteRenderer = comp as SpriteRenderer;
                        var _val = valObj as Sprite;
                        var tween = DOTween.To(new TweenPlugValueSet<Sprite>(), () => spriteRenderer.sprite, (x) => spriteRenderer.sprite = x, _val, time);
                        return tween;
                    }
                case PropertyTrack.ValueType.Enum: {
                        System.Type infType = propTrack.GetCachedInfoType(seq.target);
                        object enumVal = infType != null ? System.Enum.ToObject(infType, (int)val) : null;
                        if(enumVal != null) {
                            var tween = DOTween.To(new TweenPlugValueSet<object>(), GenerateGetter<object>(propTrack, comp), GenerateSetter<object>(propTrack, comp), enumVal, time);
                            return tween;
                        }
                        else {
                            Debug.LogError("Invalid enum value.");
                            break;
                        }
                    }
            }
            return null; //no type match
        }

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(endFrame == -1) return;
                if(interp == Interpolation.Curve && path.Length <= 1) return;
            }

            PropertyTrack propTrack = track as PropertyTrack;

            string varName = propTrack.getMemberName();
            if(string.IsNullOrEmpty(varName)) { Debug.LogError("Animator: No FieldInfo or PropertyInfo set."); return; }

            PropertyTrack.ValueType valueType = propTrack.valueType;

            //get component and fill the cached method info
            Component comp = propTrack.GetTargetComp(target as GameObject);
            if(comp == null) return;

            propTrack.RefreshData(comp);

            float numFrames = endFrame == -1 ? 1f : (float)(endFrame - frame);
            float time = numFrames / seq.take.frameRate;

            if(interp == Interpolation.None) {
                seq.Insert(this, GenerateSingleValueTweener(seq, propTrack, time, comp));
            }
            else if(interp == Interpolation.Linear || path.Length == 0) {
                //grab end frame
                var endKey = track.keys[index + 1] as PropertyKey;

                if(targetsAreEqual(valueType, endKey)) return;

                Tweener tweenLinear = null;

                switch(valueType) {
                    case PropertyTrack.ValueType.Integer:
                        tweenLinear = DOTween.To(new IntPlugin(), () => System.Convert.ToInt32(val), GenerateSetter<int>(propTrack, comp), System.Convert.ToInt32(endKey.val), time); break;
                    case PropertyTrack.ValueType.Float:
                        tweenLinear = DOTween.To(new FloatPlugin(), () => System.Convert.ToSingle(val), GenerateSetter<float>(propTrack, comp), System.Convert.ToSingle(endKey.val), time); break;
                    case PropertyTrack.ValueType.Double:
                        tweenLinear = DOTween.To(new DoublePlugin(), () => val, GenerateSetter<double>(propTrack, comp), endKey.val, time); break;
                    case PropertyTrack.ValueType.Long:
                        tweenLinear = DOTween.To(new LongPlugin(), () => System.Convert.ToInt64(val), GenerateSetter<long>(propTrack, comp), System.Convert.ToInt64(endKey.val), time); break;
                    case PropertyTrack.ValueType.Vector2:
                        tweenLinear = DOTween.To(TweenPluginFactory.CreateVector2(), () => vect2, GenerateSetter<Vector2>(propTrack, comp), endKey.vect2, time); break;
                    case PropertyTrack.ValueType.Vector3:
                        tweenLinear = DOTween.To(TweenPluginFactory.CreateVector3(), () => vect3, GenerateSetter<Vector3>(propTrack, comp), endKey.vect3, time); break;
                    case PropertyTrack.ValueType.Color:
                        tweenLinear = DOTween.To(TweenPluginFactory.CreateColor(), () => color, GenerateSetter<Color>(propTrack, comp), endKey.color, time); break;
                    case PropertyTrack.ValueType.Rect:
                        tweenLinear = DOTween.To(new RectPlugin(), () => rect, (x) => GenerateSetter<Rect>(propTrack, comp), endKey.rect, time); break;
                    case PropertyTrack.ValueType.Vector4:
                        tweenLinear = DOTween.To(TweenPluginFactory.CreateVector4(), () => vect4, GenerateSetter<Vector4>(propTrack, comp), endKey.vect4, time); break;
                    case PropertyTrack.ValueType.Quaternion:
                        tweenLinear = DOTween.To(new PureQuaternionPlugin(), () => quat, GenerateSetter<Quaternion>(propTrack, comp), endKey.quat, time); break;
                }

                if(tweenLinear != null) {
                    if(hasCustomEase())
                        tweenLinear.SetEase(easeCurve);
                    else
                        tweenLinear.SetEase(easeType, amplitude, period);

                    seq.Insert(this, tweenLinear);
                }
            }
            else {
                //create tween
                var pathType = path.Length == 2 ? TweenPlugPathType.Linear : TweenPlugPathType.CatmullRom;
                var pathData = new TweenPlugPath(pathType, path, pathResolution);

                //options
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart, isClosedPath = isClosed };

                Tweener tweenPath;

                switch(valueType) {
                    case PropertyTrack.ValueType.Integer: {
                        var tween = DOTween.To(TweenPlugPathInt.Get(), () => System.Convert.ToInt32(val), GenerateSetter<int>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Float: {
                        var tween = DOTween.To(TweenPlugPathFloat.Get(), () => System.Convert.ToSingle(val), GenerateSetter<float>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Double: {
                        var tween = DOTween.To(TweenPlugPathDouble.Get(), () => val, GenerateSetter<double>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Long: {
                        var tween = DOTween.To(TweenPlugPathLong.Get(), () => System.Convert.ToInt64(val), GenerateSetter<long>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Vector2: {
                        var tween = DOTween.To(TweenPlugPathVector2.Get(), () => vect2, GenerateSetter<Vector2>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Vector3: {
                        var tween = DOTween.To(TweenPlugPathVector3.Get(), () => vect3, GenerateSetter<Vector3>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Color: {
                        var tween = DOTween.To(TweenPlugPathColor.Get(), () => color, GenerateSetter<Color>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Rect: {
                        var tween = DOTween.To(TweenPlugPathRect.Get(), () => rect, GenerateSetter<Rect>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Vector4: {
                        var tween = DOTween.To(TweenPlugPathVector4.Get(), () => vect4, GenerateSetter<Vector4>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    case PropertyTrack.ValueType.Quaternion: {
                        var tween = DOTween.To(TweenPlugPathEuler.Get(), () => quat, GenerateSetter<Quaternion>(propTrack, comp), pathData, time);
                        tween.plugOptions = options; tweenPath = tween;
                    }
                    break;

                    default:
                        tweenPath = null;
                        break;
                }

                if(tweenPath != null) {
                    tweenPath.SetRelative(false);

                    if(hasCustomEase())
                        tweenPath.SetEase(easeCurve);
                    else
                        tweenPath.SetEase(easeType, amplitude, period);

                    seq.Insert(this, tweenPath);
                }
            }
            return;
        }

        public string getFloatArrayString(int codeLanguage, List<float> ls) {
            string s = "";
            if(codeLanguage == 0) s += "new float[]{";
            else s += "[";
            for(int i = 0; i < ls.Count; i++) {
                s += ls[i].ToString();
                if(codeLanguage == 0) s += "f";
                if(i < ls.Count - 1) s += ", ";
            }
            if(codeLanguage == 0) s += "}";
            else s += "]";
            return s;
        }
        public string getValueString(System.Type type, PropertyTrack.ValueType valueType, PropertyKey nextKey, bool brief) {
            System.Text.StringBuilder s = new System.Text.StringBuilder();

            if(PropertyTrack.isValueTypeNumeric(valueType)) {
                //s+= start_val.ToString();
                s.Append(formatNumeric(val));
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(formatNumeric(nextKey.val)); }
                //if(!brief && endFrame != -1) s += " -> "+end_val.ToString();
            }
            else if(valueType == PropertyTrack.ValueType.Bool) {
                s.Append(val > 0.0 ? "(true)" : "(false)");
            }
            else if(valueType == PropertyTrack.ValueType.String) {
                s.AppendFormat("\"{0}\"", valString);
            }
            else if(valueType == PropertyTrack.ValueType.Vector2) {
                s.Append(vect2.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.vect2.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Vector3) {
                s.Append(vect3.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.vect3.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Color) {
                //return null; 
                s.Append(color.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.color.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Rect) {
                //return null; 
                s.Append(rect.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.rect.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Vector4) {
                s.Append(vect4.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.vect4.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Quaternion) {
                s.Append(quat.ToString());
                if(!brief && nextKey != null) { s.Append(" -> "); s.Append(nextKey.quat.ToString()); }
            }
            else if(valueType == PropertyTrack.ValueType.Sprite) {
                s.AppendFormat("\"{0}\"", valObj ? valObj.name : "none");
            }
            else if(valueType == PropertyTrack.ValueType.Enum) {
                s.Append(System.Enum.ToObject(type, (int)val).ToString());
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
        // use for doubles
        private string formatNumeric(double input) {
            double _input = (input < 0d ? input * -1d : input);
            if(_input < 1d) {
                if(_input >= 0.01d) return input.ToString("N3");
                else if(_input >= 0.001d) return input.ToString("N4");
                else if(_input >= 0.0001d) return input.ToString("N5");
                else if(_input >= 0.00001d) return input.ToString("N6");
                else return input.ToString();
            }
            return input.ToString("N2");
        }

        public bool targetsAreEqual(PropertyTrack.ValueType valueType, PropertyKey nextKey) {
            if(nextKey != null) {
                if(valueType == PropertyTrack.ValueType.Integer || valueType == PropertyTrack.ValueType.Long || valueType == PropertyTrack.ValueType.Float || valueType == PropertyTrack.ValueType.Double)
                    return val == nextKey.val;
                if(valueType == PropertyTrack.ValueType.Vector2) return (vect2 == nextKey.vect2);
                if(valueType == PropertyTrack.ValueType.Vector3) return (vect3 == nextKey.vect3);
                if(valueType == PropertyTrack.ValueType.Color) return (color == nextKey.color); //return start_color.ToString()+" -> "+end_color.ToString();
                if(valueType == PropertyTrack.ValueType.Rect) return (rect == nextKey.rect); //return start_rect.ToString()+" -> "+end_rect.ToString();
                if(valueType == PropertyTrack.ValueType.Vector4) return (vect4 == nextKey.vect4);
                if(valueType == PropertyTrack.ValueType.Quaternion) return (quat == nextKey.quat);
            }
            return false;
        }

        public object getValue(PropertyTrack.ValueType valueType) {
            if(valueType == PropertyTrack.ValueType.Integer) return System.Convert.ToInt32(val);
            if(valueType == PropertyTrack.ValueType.Long) return System.Convert.ToInt64(val);
            if(valueType == PropertyTrack.ValueType.Float) return System.Convert.ToSingle(val);
            if(valueType == PropertyTrack.ValueType.Double) return val;
            if(valueType == PropertyTrack.ValueType.Vector2) return vect2;
            if(valueType == PropertyTrack.ValueType.Vector3) return vect3;
            if(valueType == PropertyTrack.ValueType.Color) return color; //return start_color.ToString()+" -> "+end_color.ToString();
            if(valueType == PropertyTrack.ValueType.Rect) return rect; //return start_rect.ToString()+" -> "+end_rect.ToString();
            if(valueType == PropertyTrack.ValueType.Vector4) return vect4;
            if(valueType == PropertyTrack.ValueType.Quaternion) return quat;
            if(valueType == PropertyTrack.ValueType.Bool) return val > 0.0;
            if(valueType == PropertyTrack.ValueType.String) return valString;
            if(valueType == PropertyTrack.ValueType.Sprite) return (valObj ? valObj : null);
            if(valueType == PropertyTrack.ValueType.Enum) return System.Convert.ToInt32(val);
            return "Unknown";
        }
        #endregion
    }
}