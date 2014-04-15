using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMPropertyKey : AMKey {
    public int valueType;

	[SerializeField]
    Component component;
	[SerializeField]
	string componentName = "";

	[SerializeField]
	string propertyName;
	[SerializeField]
	string fieldName;
	[SerializeField]
	string methodName;

	[SerializeField]
	string[] methodParameterTypes;

    public int endFrame;
    
	public void SetComponent(Component comp, bool nameOnly) {
		if(comp) {
			component = nameOnly ? null : comp;
			componentName = comp.GetType().Name;
		}
		else {
			component = null;
			componentName = "";
		}
	}

	public void SetPropertyInfo(PropertyInfo inf) {
		if(inf != null) { propertyName = inf.Name; fieldName = ""; methodName = ""; }
		else propertyName = "";
	}

	public void SetFieldInfo(FieldInfo inf) {
		if(inf != null) { fieldName = inf.Name; propertyName = ""; methodName = ""; }
		else fieldName = "";
	} // holds a field such as variables for user scripts, should be null if property is used

	public void SetMethodInfo(MethodInfo inf, string[] parms) {
		if(inf != null) { methodName = inf.Name; fieldName = ""; propertyName = ""; }
		else methodName = "";
		methodParameterTypes = parms;
	}

    //union for single values
    public double val;	// value as double
	public string valString; //string
	public UnityEngine.Object valObj;

    //union for vectors, color, rect
    public Vector4 vect4;
    public Vector2 vect2 { get { return new Vector2(vect4.x, vect4.y); } set { vect4.Set(value.x, value.y, 0, 0); } }
    public Vector3 vect3 { get { return new Vector3(vect4.x, vect4.y, vect4.z); } set { vect4.Set(value.x, value.y, value.z, 0); } }
    public Color color { get { return new Color(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.r, value.g, value.b, value.a); } }
    public Rect rect { get { return new Rect(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.xMin, value.yMin, value.width, value.height); } }
    public Quaternion quat { get { return new Quaternion(vect4.x, vect4.y, vect4.z, vect4.w); } set { vect4.Set(value.x, value.y, value.z, value.w); } }

    public double end_val;			// value as double (includes int/long)
    public Vector4 end_vect4;

    public Vector2 end_vect2 { get { return new Vector2(end_vect4.x, end_vect4.y); } set { end_vect4.Set(value.x, value.y, 0, 0); } }
    public Vector3 end_vect3 { get { return new Vector3(end_vect4.x, end_vect4.y, end_vect4.z); } set { end_vect4.Set(value.x, value.y, value.z, 0); } }
    public Color end_color { get { return new Color(end_vect4.x, end_vect4.y, end_vect4.z, end_vect4.w); } set { end_vect4.Set(value.r, value.g, value.b, value.a); } }
    public Rect end_rect { get { return new Rect(end_vect4.x, end_vect4.y, end_vect4.z, end_vect4.w); } set { end_vect4.Set(value.xMin, value.yMin, value.width, value.height); } }
    public Quaternion end_quat { get { return new Quaternion(end_vect4.x, end_vect4.y, end_vect4.z, end_vect4.w); } set { end_vect4.Set(value.x, value.y, value.z, value.w); } }

	public bool canTween {
		get { return !(valueType == (int)AMPropertyTrack.ValueType.Bool || valueType == (int)AMPropertyTrack.ValueType.String || valueType == (int)AMPropertyTrack.ValueType.Sprite); }
	}

	public bool setValue(UnityEngine.Object val) {
		if(this.valObj != val) {
			this.valObj = val;
			return true;
		}
		return false;
	}

	public bool setValue(bool val) {
		double nval = val ? 1.0 : -1.0;
		if(this.val != nval) {
			this.val = nval;
			return true;
		}
		return false;
	}

	public bool setValue(string val) {
		if(this.valString != val) {
			this.valString = val;
			return true;
		}
		return false;
	}

    public bool setValue(float val) {
        if(this.val != (double)val) {
            this.val = (double)val;
            return true;
        }
        return false;
    }
    public bool setValue(Quaternion quat) {
        if(this.quat != quat) {
            this.quat = quat;
            return true;
        }
        return false;
    }
    public bool setValue(Vector4 vect4) {
        if(this.vect4 != vect4) {
            this.vect4 = vect4;
            return true;
        }
        return false;
    }
    public bool setValue(Vector3 vect3) {
        if(this.vect3 != vect3) {
            this.vect3 = vect3;
            return true;
        }
        return false;
    }
    public bool setValue(Color color) {
        if(this.color != color) {
            this.color = color;
            return true;
        }
        return false;
    }
    public bool setValue(Rect rect) {
        if(this.rect != rect) {
            this.rect = rect;
            return true;
        }
        return false;
    }
    public bool setValue(Vector2 vect2) {
        if(this.vect2 != vect2) {
            this.vect2 = vect2;
            return true;
        }
        return false;
    }
    // set value from double
    public bool setValue(double val) {
        if(this.val != val) {
            this.val = val;
            return true;
        }
        return false;
    }
    // set value from int
    public bool setValue(int val) {
        if(this.val != (double)val) {
            this.val = (double)val;
            return true;
        }
        return false;
    }
    // set value from long
    public bool setValue(long val) {
        if(this.val != (double)val) {
            this.val = (double)val;
            return true;
        }
        return false;
    }

    // copy properties from key
    public override AMKey CreateClone(GameObject go) {

		AMPropertyKey a = go ? go.AddComponent<AMPropertyKey>() : gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
		a.component = component;
		a.componentName = componentName;
		a.propertyName = propertyName;
		a.fieldName = fieldName;
		a.methodName = methodName;

		a.methodParameterTypes = new string[methodParameterTypes.Length];
		System.Array.Copy(methodParameterTypes, a.methodParameterTypes, methodParameterTypes.Length);

        a.frame = frame;
        a.val = val;
		a.valObj = valObj;
		a.valString = valString;
        a.vect4 = vect4;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);

        return a;
    }

	//use in preview for specific refresh after setting the property with given obj
	//e.g. Sprite
	public void refresh(GameObject go, object obj) {
		if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
			SpriteRenderer sr = component ? component as SpriteRenderer : go.GetComponent<SpriteRenderer>();
			sr.sprite = obj as Sprite;
		}
	}

    #region action
    public override int getNumberOfFrames() {
        return endFrame - frame;
    }
    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }
    public override Tweener buildTweener(AMITarget itarget, Sequence sequence, UnityEngine.Object target, int frameRate) {
        if(targetsAreEqual()) return null;

		//get component and fill the cached method info
		Component comp;
		if(itarget.TargetIsMeta()) {
			if(string.IsNullOrEmpty(componentName)) return null;
			comp = (target as GameObject).GetComponent(componentName);
			
		}
		else {
			if(component == null) return null;
			comp = component;
		}

		if((endFrame == -1 && easeType != EaseTypeNone && canTween) || !comp || (string.IsNullOrEmpty(fieldName) && string.IsNullOrEmpty(propertyName) && string.IsNullOrEmpty(methodName))) return null;

        string varName = null;

		if(!string.IsNullOrEmpty(fieldName)) {
            varName = fieldName;
        }
		else if(!string.IsNullOrEmpty(propertyName)) {
            varName = propertyName;
        }
        //return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugQuaternionSlerp(endRotation)).Ease(easeCurve));
        if(varName != null) {
			if(valueType == (int)AMPropertyTrack.ValueType.Bool) {
				return HOTween.To(comp, endFrame == -1 ? 1.0f/(float)frameRate : getTime(frameRate), 
				                  new TweenParms().Prop(varName, new AMPlugNoTween(val > 0.0)));
			}
			else if(valueType == (int)AMPropertyTrack.ValueType.String) {
				return HOTween.To(comp, endFrame == -1 ? 1.0f/(float)frameRate : getTime(frameRate), 
				                  new TweenParms().Prop(varName, new AMPlugNoTween(valString)));
			}
			else if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
				return HOTween.To(comp, endFrame == -1 ? 1.0f/(float)frameRate : getTime(frameRate), 
				                  new TweenParms().Prop(varName, new AMPlugSprite(comp as SpriteRenderer, valObj ? valObj as Sprite : null)));
			}
			else if(easeType == EaseTypeNone) {
				float t = endFrame == -1 ? 1.0f/(float)frameRate : getTime(frameRate);

				switch((AMPropertyTrack.ValueType)valueType) {
				case AMPropertyTrack.ValueType.Integer:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(Convert.ToInt32(val))));
				case AMPropertyTrack.ValueType.Float:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(Convert.ToSingle(val))));
				case AMPropertyTrack.ValueType.Double:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(new AMPlugDouble(val))));
				case AMPropertyTrack.ValueType.Long:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(Convert.ToInt64(val))));
				case AMPropertyTrack.ValueType.Vector2:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(vect2)));
				case AMPropertyTrack.ValueType.Vector3:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(vect3)));
				case AMPropertyTrack.ValueType.Color:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(color)));
				case AMPropertyTrack.ValueType.Rect:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(rect)));
				case AMPropertyTrack.ValueType.Vector4:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(vect4)));
				case AMPropertyTrack.ValueType.Quaternion:
					return HOTween.To(comp, t, new TweenParms().Prop(varName, new AMPlugNoTween(quat)));
				}
			}
            else if(hasCustomEase()) {
                switch((AMPropertyTrack.ValueType)valueType) {
                    case AMPropertyTrack.ValueType.Integer:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, Convert.ToInt32(end_val)).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Float:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, Convert.ToSingle(end_val)).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Double:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugDouble(end_val)).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Long:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugLong(Convert.ToInt64(end_val))).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Vector2:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect2).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Vector3:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect3).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Color:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_color).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Rect:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_rect).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Vector4:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect4).Ease(easeCurve));
                    case AMPropertyTrack.ValueType.Quaternion:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugQuaternionSlerp(end_quat)).Ease(easeCurve));
                }
            }
            else {
                switch((AMPropertyTrack.ValueType)valueType) {
                    case AMPropertyTrack.ValueType.Integer:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, Convert.ToInt32(end_val)).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Float:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, Convert.ToSingle(end_val)).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Double:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugDouble(end_val)).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Long:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugLong(Convert.ToInt64(end_val))).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Vector2:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect2).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Vector3:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect3).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Color:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_color).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Rect:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_rect).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Vector4:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, end_vect4).Ease((EaseType)easeType, amplitude, period));
                    case AMPropertyTrack.ValueType.Quaternion:
					return HOTween.To(comp, getTime(frameRate), new TweenParms().Prop(varName, new AMPlugQuaternionSlerp(end_quat)).Ease((EaseType)easeType, amplitude, period));
                }
            }
        }

        Debug.LogError("Animator: No FieldInfo or PropertyInfo set.");
        return null;
    }

    public string getName() {
		if(!string.IsNullOrEmpty(fieldName)) return fieldName;
		else if(!string.IsNullOrEmpty(propertyName)) return propertyName;
        else if(!string.IsNullOrEmpty(methodName)) {
        }
        return "Unknown";
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
    public string getValueString(bool brief) {
        string s = "";
        if(AMPropertyTrack.isValueTypeNumeric(valueType)) {
            //s+= start_val.ToString();
            s += formatNumeric(val);
            if(!brief && endFrame != -1) s += " -> " + formatNumeric(end_val);
            //if(!brief && endFrame != -1) s += " -> "+end_val.ToString();
        }
		else if(valueType == (int)AMPropertyTrack.ValueType.Bool) {
			s += (val > 0.0 ? "(true)" : "(false)");
		}
		else if(valueType == (int)AMPropertyTrack.ValueType.String) {
			s += string.Format("\"{0}\"", valString);
		}
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector2) {
            s += vect2.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_vect2.ToString();
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector3) {
            s += vect3.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_vect3.ToString();
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Color) {
            //return null; 
            s += color.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_color.ToString();
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Rect) {
            //return null; 
            s += rect.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_rect.ToString();
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Vector4) {
            s += vect4.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_vect4.ToString();
        }
        else if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) {
            s += quat.ToString();
            if(!brief && endFrame != -1) s += " -> " + end_quat.ToString();
        }
		else if(valueType == (int)AMPropertyTrack.ValueType.Sprite) {
			s += string.Format("\"{0}\"", valObj ? valObj.name : "none");
		}
        return s;
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

    public bool targetsAreEqual() {
		if(valueType == (int)AMPropertyTrack.ValueType.String || valueType == (int)AMPropertyTrack.ValueType.Bool || valueType == (int)AMPropertyTrack.ValueType.Sprite)
			return false;
        if(valueType == (int)AMPropertyTrack.ValueType.Integer || valueType == (int)AMPropertyTrack.ValueType.Long || valueType == (int)AMPropertyTrack.ValueType.Float || valueType == (int)AMPropertyTrack.ValueType.Double)
            return val == end_val;
        if(valueType == (int)AMPropertyTrack.ValueType.Vector2) return (vect2 == end_vect2);
        if(valueType == (int)AMPropertyTrack.ValueType.Vector3) return (vect3 == end_vect3);
        if(valueType == (int)AMPropertyTrack.ValueType.Color) return (color == end_color); //return start_color.ToString()+" -> "+end_color.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Rect) return (rect == end_rect); //return start_rect.ToString()+" -> "+end_rect.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Vector4) return (vect4 == end_vect4);
        if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) return (quat == end_quat);

        Debug.LogError("Animator: Invalid ValueType " + valueType);
        return false;
    }

    public object getStartValue() {
        if(valueType == (int)AMPropertyTrack.ValueType.Integer) return Convert.ToInt32(val);
        if(valueType == (int)AMPropertyTrack.ValueType.Long) return Convert.ToInt64(val);
        if(valueType == (int)AMPropertyTrack.ValueType.Float) return Convert.ToSingle(val);
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
        return "Unknown";
    }
    public object getEndValue() {
        if(valueType == (int)AMPropertyTrack.ValueType.Integer) return Convert.ToInt32(end_val);
        if(valueType == (int)AMPropertyTrack.ValueType.Long) return Convert.ToInt64(end_val);
        if(valueType == (int)AMPropertyTrack.ValueType.Float) return Convert.ToSingle(end_val);
        if(valueType == (int)AMPropertyTrack.ValueType.Double) return end_val;
        if(valueType == (int)AMPropertyTrack.ValueType.Vector2) return end_vect2;
        if(valueType == (int)AMPropertyTrack.ValueType.Vector3) return end_vect3;
        if(valueType == (int)AMPropertyTrack.ValueType.Color) return end_color; //return start_color.ToString()+" -> "+end_color.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Rect) return end_rect; //return start_rect.ToString()+" -> "+end_rect.ToString();
        if(valueType == (int)AMPropertyTrack.ValueType.Vector4) return end_vect4;
        if(valueType == (int)AMPropertyTrack.ValueType.Quaternion) return end_quat;
		if(valueType == (int)AMPropertyTrack.ValueType.Bool) return val > 0.0;
		if(valueType == (int)AMPropertyTrack.ValueType.String) return valString;
		if(valueType == (int)AMPropertyTrack.ValueType.Sprite) return valObj;
        return "Unknown";
    }
    #endregion
}
