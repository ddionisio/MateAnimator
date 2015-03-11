using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMPropertyKey : AMKey {
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

    // copy properties from key
    public override void CopyTo(AMKey key) {
		AMPropertyKey a = key as AMPropertyKey;
        a.enabled = false;
		
        a.frame = frame;
        a.val = val;
		a.valObj = valObj;
		a.valString = valString;
        a.vect4 = vect4;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
    }

	//use in preview for specific refresh after setting the property with given obj
	//e.g. Sprite
	/*public void refresh(GameObject go, object obj) {
		if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
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

    AMActionData GenerateActionData(AMPropertyTrack propTrack, int frameRate, Component comp, object obj) {
        propTrack.RefreshData(comp);

        PropertyInfo propInfo = propTrack.GetCachedPropertyInfo();
        if(propInfo != null)
            return new AMActionPropertySet(this, frameRate, comp, propInfo, obj);
        else {
            FieldInfo fieldInfo = propTrack.GetCachedFieldInfo();
            if(fieldInfo != null)
                return new AMActionFieldSet(this, frameRate, comp, fieldInfo, obj);
        }
        return null;
    }

    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        AMPropertyTrack propTrack = track as AMPropertyTrack;

        if(endFrame == -1 && canTween && propTrack.canTween) return;

        int valueType = propTrack.valueType;
                
		//get component and fill the cached method info
        Component comp = propTrack.GetTargetComp(target as GameObject);

        if(comp == null) return;

        string varName = propTrack.getMemberName();

        int frameRate = seq.take.frameRate;

        //change to use setvalue track in AMSequence
        if(!string.IsNullOrEmpty(varName)) {
            if(propTrack.canTween) {
                //allow tracks with just one key
                if(track.keys.Count == 1)
                    interp = (int)Interpolation.None;

                if(!canTween) {
                    object obj = null;

                    switch((AMPropertyTrack.ValueType)valueType) {
                        case AMPropertyTrack.ValueType.Integer:
                            obj = System.Convert.ToInt32(val); break;
                        case AMPropertyTrack.ValueType.Float:
                            obj = System.Convert.ToSingle(val); break;
                        case AMPropertyTrack.ValueType.Double:
                            obj = val; break;
                        case AMPropertyTrack.ValueType.Long:
                            obj = System.Convert.ToInt64(val); break;
                        case AMPropertyTrack.ValueType.Vector2:
                            obj = vect2; break;
                        case AMPropertyTrack.ValueType.Vector3:
                            obj = vect3; break;
                        case AMPropertyTrack.ValueType.Color:
                            obj = color; break;
                        case AMPropertyTrack.ValueType.Rect:
                            obj = rect; break;
                        case AMPropertyTrack.ValueType.Vector4:
                            obj = vect4; break;
                        case AMPropertyTrack.ValueType.Quaternion:
                            obj = quat; break;
                    }

                    if(obj != null)
                        seq.Insert(GenerateActionData(propTrack, frameRate, comp, obj));
                }
                else {
                    //grab end frame
                    AMPropertyKey endKey = track.keys[index + 1] as AMPropertyKey;

                    if(targetsAreEqual(valueType, endKey)) return;

                    if(hasCustomEase()) {
                        switch((AMPropertyTrack.ValueType)valueType) {
                            case AMPropertyTrack.ValueType.Integer:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, System.Convert.ToInt32(endKey.val)).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Float:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, System.Convert.ToSingle(endKey.val)).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Double:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugDouble(endKey.val)).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Long:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugLong(System.Convert.ToInt64(endKey.val))).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Vector2:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect2).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Vector3:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect3).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Color:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.color).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Rect:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.rect).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Vector4:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect4).Ease(easeCurve))); break;
                            case AMPropertyTrack.ValueType.Quaternion:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugQuaternionSlerp(endKey.quat)).Ease(easeCurve))); break;
                        }
                    }
                    else {
                        switch((AMPropertyTrack.ValueType)valueType) {
                            case AMPropertyTrack.ValueType.Integer:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, System.Convert.ToInt32(endKey.val)).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Float:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, System.Convert.ToSingle(endKey.val)).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Double:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugDouble(endKey.val)).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Long:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugLong(System.Convert.ToInt64(endKey.val))).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Vector2:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect2).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Vector3:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect3).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Color:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.color).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Rect:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.rect).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Vector4:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, endKey.vect4).Ease((EaseType)easeType, amplitude, period))); break;
                            case AMPropertyTrack.ValueType.Quaternion:
                                seq.Insert(this, HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugQuaternionSlerp(endKey.quat)).Ease((EaseType)easeType, amplitude, period))); break;
                        }
                    }
                }
            }
            else {
                if(endFrame == -1) endFrame = frame + 1;

                if(valueType == (int)AMPropertyTrack.ValueType.Bool) {
                    seq.Insert(GenerateActionData(propTrack, frameRate, comp, val > 0.0f));
			    }
			    else if(valueType == (int)AMPropertyTrack.ValueType.String) {
                    seq.Insert(GenerateActionData(propTrack, frameRate, comp, valString));
			    }
			    else if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
                    seq.Insert(new AMActionSpriteSet(this, frameRate, comp as SpriteRenderer, valObj as Sprite));
			    }
                else if(valueType == (int)AMPropertyTrack.ValueType.Enum) {
                    System.Type infType = propTrack.GetCachedInfoType(seq.target);
                    object enumVal = infType != null ? System.Enum.ToObject(infType, (int)val) : null;
                    if(enumVal != null) {
                        seq.Insert(GenerateActionData(propTrack, frameRate, comp, enumVal));
                    }
                    else {
                        Debug.LogError("Invalid enum value.");
                    }
                }
            }
        }
        else
            Debug.LogError("Animator: No FieldInfo or PropertyInfo set.");

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
    public string getValueString(System.Type type, int valueType, AMPropertyKey nextKey, bool brief) {
        System.Text.StringBuilder s = new System.Text.StringBuilder();

        if(AMPropertyTrack.isValueTypeNumeric(valueType)) {
            //s+= start_val.ToString();
            s.Append(formatNumeric(val));
            if(!brief && nextKey) { s.Append(" -> "); s.Append(formatNumeric(nextKey.val)); }
            //if(!brief && endFrame != -1) s += " -> "+end_val.ToString();
        }
		else if(valueType == (int)AMPropertyTrack.ValueType.Bool) {
			s.Append(val > 0.0 ? "(true)" : "(false)");
		}
		else if(valueType == (int)AMPropertyTrack.ValueType.String) {
			s.AppendFormat("\"{0}\"", valString);
		}
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector2) {
            s.Append(vect2.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.vect2.ToString()); }
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector3) {
            s.Append(vect3.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.vect3.ToString()); }
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Color) {
            //return null; 
            s.Append(color.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.color.ToString()); }
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Rect) {
            //return null; 
            s.Append(rect.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.rect.ToString()); }
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector4) {
            s.Append(vect4.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.vect4.ToString()); }
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) {
            s.Append(quat.ToString());
            if(!brief && nextKey) { s.Append(" -> "); s.Append(nextKey.quat.ToString()); }
        }
		else if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
			s.AppendFormat("\"{0}\"", valObj ? valObj.name : "none");
		}
        else if(valueType == (int)AMPropertyTrack.ValueType.Enum) {
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

    public bool targetsAreEqual(int valueType, AMPropertyKey nextKey) {
        if(nextKey) {
            if(valueType == (int)AMPropertyTrack.ValueType.Integer || valueType == (int)AMPropertyTrack.ValueType.Long || valueType == (int)AMPropertyTrack.ValueType.Float || valueType == (int)AMPropertyTrack.ValueType.Double)
                return val == nextKey.val;
            if(valueType == (int)AMPropertyTrack.ValueType.Vector2) return (vect2 == nextKey.vect2);
            if(valueType == (int)AMPropertyTrack.ValueType.Vector3) return (vect3 == nextKey.vect3);
            if(valueType == (int)AMPropertyTrack.ValueType.Color) return (color == nextKey.color); //return start_color.ToString()+" -> "+end_color.ToString();
            if(valueType == (int)AMPropertyTrack.ValueType.Rect) return (rect == nextKey.rect); //return start_rect.ToString()+" -> "+end_rect.ToString();
            if(valueType == (int)AMPropertyTrack.ValueType.Vector4) return (vect4 == nextKey.vect4);
            if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) return (quat == nextKey.quat);
        }
        return false;
    }

    public object getValue(int valueType) {
		if(valueType == (int)AMPropertyTrack.ValueType.Integer) return System.Convert.ToInt32(val);
		if(valueType == (int)AMPropertyTrack.ValueType.Long) return System.Convert.ToInt64(val);
		if(valueType == (int)AMPropertyTrack.ValueType.Float) return System.Convert.ToSingle(val);
        if(valueType == (int)AMPropertyTrack.ValueType.Double) return val;
        if(valueType == (int)AMPropertyTrack.ValueType.Vector2) return vect2;
        if(valueType == (int)AMPropertyTrack.ValueType.Vector3) return vect3;
        if(valueType == (int)AMPropertyTrack.ValueType.Color) return color; //return start_color.ToString()+" -> "+end_color.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Rect) return rect; //return start_rect.ToString()+" -> "+end_rect.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Vector4) return vect4;
        if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) return quat;
		if(valueType == (int)AMPropertyTrack.ValueType.Bool) return val > 0.0;
		if(valueType == (int)AMPropertyTrack.ValueType.String) return valString;
		if(valueType == (int)AMPropertyTrack.ValueType.Sprite) return (valObj ? valObj : null);
        if(valueType == (int)AMPropertyTrack.ValueType.Enum) return System.Convert.ToInt32(val);
        return "Unknown";
    }
    #endregion
}
