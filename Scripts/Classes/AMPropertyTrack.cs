using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using Holoville.HOTween.Core;
using Holoville.HOTween;

[AddComponentMenu("")]
public class AMPropertyTrack : AMTrack {
    public enum ValueType {
        Integer = 0,
        Long = 1,
        Float = 2,
        Double = 3,
        Vector2 = 4,
        Vector3 = 5,
        Color = 6,
        Rect = 7,
        String = 9,
        Vector4 = 10,
        Quaternion = 11,

		Bool = 12,
		Sprite = 13,
    }
    public int valueType;

	[SerializeField]
    GameObject obj;
	[SerializeField]
    Component component;
	[SerializeField]
	string componentName;
	[SerializeField]
    string propertyName;
	[SerializeField]
    string fieldName;
	[SerializeField]
    string methodName;
	[SerializeField]
    string[] methodParameterTypes;

    private PropertyInfo cachedPropertyInfo;
    private FieldInfo cachedFieldInfo;
    private MethodInfo cachedMethodInfo;

	protected override void SetSerializeObject(UnityEngine.Object obj) {
		this.obj = obj as GameObject;

		propertyName = "";
		fieldName = "";
		methodName = "";
		methodParameterTypes = new string[0];
		
		cachedPropertyInfo = null;
		cachedFieldInfo = null;
		cachedMethodInfo = null;
	}
	
	protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
		return targetGO ? targetGO : obj;
	}

	Component GetTargetComp(AMITarget target) {
		Component comp;
		if(target.TargetIsMeta()) {
			component = null;
			GameObject go = GetTarget(target) as GameObject;
			comp = go ? go.GetComponent(componentName) : null;
		}
		else {
			comp = component;
		}
		return comp;
	}

	void RefreshData(Component comp) {
		if(!string.IsNullOrEmpty(propertyName)) {
			if(cachedPropertyInfo == null) cachedPropertyInfo = comp.GetType().GetProperty(propertyName);
			cachedFieldInfo = null;
			cachedMethodInfo = null;
		}
		else if(!string.IsNullOrEmpty(fieldName)) {
			if(cachedFieldInfo == null) cachedFieldInfo = comp.GetType().GetField(fieldName);
			cachedPropertyInfo = null;
			cachedMethodInfo = null;
		}
		else if(!string.IsNullOrEmpty(methodName)) {
			if(cachedMethodInfo == null) {
				Type[] t = new Type[methodParameterTypes.Length];
				for(int i = 0; i < methodParameterTypes.Length; i++) t[i] = Type.GetType(methodParameterTypes[i]);
				cachedMethodInfo = comp.GetType().GetMethod(methodName, t);
			}
			cachedFieldInfo = null;
			cachedPropertyInfo = null;
		}
	}

	public bool canTween {
		get {
			return !(valueType == (int)ValueType.Bool || valueType == (int)ValueType.String || valueType == (int)ValueType.Sprite);
		}
	}
    public override string getTrackType() {
		if(!string.IsNullOrEmpty(fieldName)) return fieldName;
		if(!string.IsNullOrEmpty(propertyName)) return propertyName;
		if(!string.IsNullOrEmpty(methodName)) {

        }
        return "Not Set";
    }
    public string getMemberInfoTypeName() {
		if(!string.IsNullOrEmpty(fieldName)) return "FieldInfo";
		if(!string.IsNullOrEmpty(propertyName)) return "PropertyInfo";
		if(!string.IsNullOrEmpty(methodName)) return "MethodInfo";
        return "Undefined";
    }
    public bool isPropertySet() {
		if(!string.IsNullOrEmpty(fieldName)) return true;
		if(!string.IsNullOrEmpty(propertyName)) return true;
		if(!string.IsNullOrEmpty(methodName)) {
        }
        return false;
    }

    // add key
    public AMKey addKey(AMITarget target, int _frame) {
		Component comp = GetTargetComp(target);
		RefreshData(comp);

        if(isValueTypeNumeric(valueType))
			return addKey(target, _frame, getPropertyValueNumeric(target));
		else if(valueType == (int)ValueType.Bool)
			return addKey(target, _frame, getPropertyValueBool(target) ? 1.0 : -1.0);
		else if(valueType == (int)ValueType.String)
			return addKey(target, _frame, getPropertyValueString(target));
        else if(valueType == (int)ValueType.Vector2)
			return addKey(target, _frame, getPropertyValueVector2(target));
        else if(valueType == (int)ValueType.Vector3)
			return addKey(target, _frame, getPropertyValueVector3(target));
        else if(valueType == (int)ValueType.Color)
			return addKey(target, _frame, getPropertyValueColor(target));
        else if(valueType == (int)ValueType.Rect)
			return addKey(target, _frame, getPropertyValueRect(target));
        else if(valueType == (int)ValueType.Vector4)
			return addKey(target, _frame, getPropertyValueVector4(target));
        else if(valueType == (int)ValueType.Quaternion)
			return addKey(target, _frame, getPropertyValueQuaternion(target));
		else if(valueType == (int)ValueType.Sprite)
			return addKey(target, _frame, getPropertyValueObject(target));
        else {
            Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
            return null;
        }
    }
    // add key numeric
	AMKey addKey(AMITarget target, int _frame, double val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
	// add key object
	AMKey addKey(AMITarget target, int _frame, UnityEngine.Object val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache(target);
				return null;
			}
		}
		AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
		a.enabled = false;
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)EaseType.Linear;
		// add a new key
		keys.Add(a);
		// update cache
		updateCache(target);
		return a;
	}
	// add key string
	AMKey addKey(AMITarget target, int _frame, string val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache(target);
				return null;
			}
		}
		AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
		a.enabled = false;
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)EaseType.Linear;
		// add a new key
		keys.Add(a);
		// update cache
		updateCache(target);
		return a;
	}
    // add key vector2
	AMKey addKey(AMITarget target, int _frame, Vector2 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    // add key vector3
	AMKey addKey(AMITarget target, int _frame, Vector3 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    // add key color
	AMKey addKey(AMITarget target, int _frame, Color val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    // add key rect
	AMKey addKey(AMITarget target, int _frame, Rect val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    // add key vector4
	AMKey addKey(AMITarget target, int _frame, Vector4 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    // add key quaternion
	AMKey addKey(AMITarget target, int _frame, Quaternion val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
				updateCache(target);
                return null;
            }
        }
        AMPropertyKey a = gameObject.AddComponent<AMPropertyKey>();
        a.enabled = false;
        a.frame = _frame;
        a.setValue(val);
        // set default ease type to linear
        a.easeType = (int)EaseType.Linear;
        // add a new key
        keys.Add(a);
        // update cache
		updateCache(target);
        return a;
    }
    
	public bool setComponent(AMITarget target, Component component) {
		if(target.TargetIsMeta()) {
			string cname = component.GetType().Name;
			this.component = null;
			if(componentName != cname) {
				componentName = cname;
				return true;
			}
		}
		else {
			if(this.component != component) {
				this.component = component;
				componentName = component.GetType().Name;
				return true;
			}
		}
        return false;
    }
    public bool setPropertyInfo(PropertyInfo propertyInfo) {
		string n = propertyInfo.Name;
        if(propertyName != n) {
            // set the property value type
            setValueType(propertyInfo.PropertyType);
            cachedPropertyInfo = propertyInfo;
			propertyName = n;
            
			cachedFieldInfo = null;
			fieldName = "";
			cachedMethodInfo = null;
			methodName = "";
            return true;
        }
        return false;
    }
    public bool setFieldInfo(FieldInfo fieldInfo) {
		string n = fieldInfo.Name;
        if(fieldName != n) {
            setValueType(fieldInfo.FieldType);
            cachedFieldInfo = fieldInfo;
			fieldName = n;

            cachedPropertyInfo = null;
			propertyName = "";
			cachedMethodInfo = null;
			methodName = "";
            return true;
        }
        return false;
    }

    public bool setMethodInfo(MethodInfo methodInfo, string[] parameterTypes, ValueType valueType) {
		string n = methodInfo.Name;
        if(this.valueType != (int)valueType || this.methodParameterTypes != parameterTypes || methodName != n) {
            setValueType((int)valueType);
			methodName = n;
            methodParameterTypes = parameterTypes;
            cachedMethodInfo = methodInfo;
            
			cachedFieldInfo = null;
			fieldName = "";
			cachedPropertyInfo = null;
			propertyName = "";
            return true;
        }
        return false;
    }
    // update cache (optimized)
    public override void updateCache(AMITarget target) {
        base.updateCache(target);

		Component comp = GetTargetComp(target);
		RefreshData(comp);

        for(int i = 0; i < keys.Count; i++) {
            AMPropertyKey key = keys[i] as AMPropertyKey;

            key.version = version;

			key.SetComponent(comp, target.TargetIsMeta());

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else {
				if(i > 0 && keys[i-1].easeType == AMKey.EaseTypeNone)
					key.easeType = AMKey.EaseTypeNone;

				key.endFrame = -1;
			}
            key.valueType = valueType;
            Type _type = null;
            bool showError = true;
            //int methodInfoType = -1;
            if(cachedFieldInfo != null) {
				key.SetFieldInfo(cachedFieldInfo);
				_type = cachedFieldInfo.FieldType;
                showError = false;
            }
            else if(cachedPropertyInfo != null) {
				key.SetPropertyInfo(cachedPropertyInfo);
				_type = cachedPropertyInfo.PropertyType;
                showError = false;
            }
            else if(cachedMethodInfo != null) {
				key.SetMethodInfo(cachedMethodInfo, methodParameterTypes);
            }
            if(showError) {
                Debug.LogError("Animator: Fatal Error; fieldInfo, propertyInfo and methodInfo are unset for Value Type " + valueType);
                key.destroy();
                return;
            }
            // set value
            if(isNumeric(_type)) {
                if(key.endFrame != -1) key.end_val = (double)(keys[i + 1] as AMPropertyKey).val;
            }
            else if(_type == typeof(Vector2)) {
                if(key.endFrame != -1) key.end_vect2 = (keys[i + 1] as AMPropertyKey).vect2;
            }
            else if(_type == typeof(Vector3)) {
                if(key.endFrame != -1) key.end_vect3 = (keys[i + 1] as AMPropertyKey).vect3;
            }
            else if(_type == typeof(Color)) {
                if(key.endFrame != -1) key.end_color = (keys[i + 1] as AMPropertyKey).color;
            }
            else if(_type == typeof(Rect)) {
                if(key.endFrame != -1) key.end_rect = (keys[i + 1] as AMPropertyKey).rect;
            }
            else if(_type == typeof(Vector4)) {
                if(key.endFrame != -1) key.end_vect4 = (keys[i + 1] as AMPropertyKey).vect4;
            }
            else if(_type == typeof(Quaternion)) {
                if(key.endFrame != -1) key.end_quat = (keys[i + 1] as AMPropertyKey).quat;
            }
            else if(key.canTween) {
                Debug.LogError("Animator: Fatal Error, property type '" + _type.ToString() + "' not found.");
                key.destroy();
                return;
            }
        }
    }

	bool getPropertyValueBool(AMITarget itarget) {
		if(cachedFieldInfo != null)
			return Convert.ToBoolean(cachedFieldInfo.GetValue(GetTargetComp(itarget)));
		else
			return Convert.ToBoolean(cachedPropertyInfo.GetValue(GetTargetComp(itarget), null));
	}

	string getPropertyValueString(AMITarget itarget) {
		if(cachedFieldInfo != null)
			return Convert.ToString(cachedFieldInfo.GetValue(GetTargetComp(itarget)));
		else
			return Convert.ToString(cachedPropertyInfo.GetValue(GetTargetComp(itarget), null));
	}

    // get numeric value. unsafe, must check to see if value is numeric first
	double getPropertyValueNumeric(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return Convert.ToDouble(cachedFieldInfo.GetValue(GetTargetComp(itarget)));
        // property
        else {
			return Convert.ToDouble(cachedPropertyInfo.GetValue(GetTargetComp(itarget), null));
        }

    }
    // get Vector2 value. unsafe, must check to see if value is Vector2 first
	Vector2 getPropertyValueVector2(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Vector2)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Vector2)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	Vector3 getPropertyValueVector3(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Vector3)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Vector3)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	Vector4 getPropertyValueVector4(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Vector4)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Vector4)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	Quaternion getPropertyValueQuaternion(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Quaternion)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Quaternion)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	Color getPropertyValueColor(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Color)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Color)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	Rect getPropertyValueRect(AMITarget itarget) {
        // field
		if(cachedFieldInfo != null)
			return (Rect)cachedFieldInfo.GetValue(GetTargetComp(itarget));
        // property
        else
			return (Rect)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);

    }
	UnityEngine.Object getPropertyValueObject(AMITarget itarget) {
		// field
		if(cachedFieldInfo != null)
			return (UnityEngine.Object)cachedFieldInfo.GetValue(GetTargetComp(itarget));
		// property
		else
			return (UnityEngine.Object)cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);
	}
    public static bool isNumeric(Type t) {
        if((t == typeof(int)) || (t == typeof(long)) || (t == typeof(float)) || (t == typeof(double)))
            return true;
        return false;
    }

    public static bool isValidType(Type t) {
		if(isNumeric(t) || (t == typeof(bool)) || (t == typeof(string)) || (t == typeof(Vector2)) || (t == typeof(Vector3)) || (t == typeof(Color)) || (t == typeof(Rect)) || (t == typeof(Vector4)) || (t == typeof(Quaternion)) || (t == typeof(Sprite)))
            return true;
        return false;
    }
    public void setValueType(int type) {
        valueType = type;
    }
    public void setValueType(Type t) {

        if(t == typeof(int))
            valueType = (int)ValueType.Integer;
        else if(t == typeof(long))
            valueType = (int)ValueType.Long;
        else if(t == typeof(float))
            valueType = (int)ValueType.Float;
        else if(t == typeof(double))
            valueType = (int)ValueType.Double;
        else if(t == typeof(Vector2))
            valueType = (int)ValueType.Vector2;
        else if(t == typeof(Vector3))
            valueType = (int)ValueType.Vector3;
        else if(t == typeof(Color))
            valueType = (int)ValueType.Color;
        else if(t == typeof(Rect))
            valueType = (int)ValueType.Rect;
        else if(t == typeof(Vector4))
            valueType = (int)ValueType.Vector4;
        else if(t == typeof(Quaternion))
            valueType = (int)ValueType.Quaternion;
		else if(t == typeof(bool))
			valueType = (int)ValueType.Bool;
		else if(t == typeof(string))
			valueType = (int)ValueType.String;
		else if(t == typeof(Sprite))
			valueType = (int)ValueType.Sprite;
        else {
            valueType = -1;
            Debug.LogWarning("Animator: Value type " + t.ToString() + " is unsupported.");
        }

    }
    // preview a frame in the scene view
    public void previewFrame(AMITarget target, float frame, bool quickPreview = false) {
        if(keys == null || keys.Count <= 0) {
            return;
        }

		GameObject go = GetTarget(target) as GameObject;
		Component comp = GetTargetComp(target);

		if(!comp || !go) return;

		RefreshData(comp);

        // if before or equal to first frame, or is the only frame
		AMPropertyKey ckey = keys[0] as AMPropertyKey;
		if((frame <= (float)ckey.frame) || ckey.endFrame == -1) {
			//go.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
            if(cachedFieldInfo != null) {
				cachedFieldInfo.SetValue(comp, ckey.getStartValue());
                refreshTransform(go);
            }
            else if(cachedPropertyInfo != null) {
				cachedPropertyInfo.SetValue(comp, ckey.getStartValue(), null);
				refreshTransform(go);
            }
            else if(cachedMethodInfo != null) {
            }
			ckey.refresh(go, ckey.getStartValue());
            return;
        }
        // if not tweenable and beyond last frame
		ckey = keys[keys.Count - 1] as AMPropertyKey;
		if(!canTween && frame >= (float)ckey.frame) {
			if(cachedFieldInfo != null) {
				cachedFieldInfo.SetValue(comp, ckey.getStartValue());
				refreshTransform(go);
			}
			else if(cachedPropertyInfo != null) {
				cachedPropertyInfo.SetValue(comp, ckey.getStartValue(), null);
				refreshTransform(go);
			}
			else if(cachedMethodInfo != null) {
			}

			if(valueType == (int)ValueType.Sprite) {
				SpriteRenderer sprRender = comp as SpriteRenderer;
				sprRender.sprite = ckey.getStartValue() as Sprite;
			}
			ckey.refresh(go, ckey.getStartValue());
			return;
		}
		//if tweenable and beyond last tweenable
		ckey = keys[keys.Count - 2] as AMPropertyKey;
		if(frame >= (float)ckey.endFrame) {
			if(cachedFieldInfo != null) {
				cachedFieldInfo.SetValue(comp, ckey.getEndValue());
				refreshTransform(go);
            }
			else if(cachedPropertyInfo != null) {
				cachedPropertyInfo.SetValue(comp, ckey.getEndValue(), null);
				refreshTransform(go);
            }
			else if(cachedMethodInfo != null) {
            }
			ckey.refresh(go, ckey.getEndValue());
            return;
        }
        // if lies on property action
        foreach(AMPropertyKey key in keys) {
            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            if(quickPreview && !key.targetsAreEqual()) return;	// quick preview; if action will execute then skip
            // if on startFrame or is no tween
            if(frame == (float)key.frame || ((key.easeType == AMKey.EaseTypeNone || !key.canTween) && frame < (float)key.endFrame)) {
				if(cachedFieldInfo != null) {
					cachedFieldInfo.SetValue(comp, key.getStartValue());
					refreshTransform(go);
                }
				else if(cachedPropertyInfo != null) {
					cachedPropertyInfo.SetValue(comp, key.getStartValue(), null);
					refreshTransform(go);
                }
				else if(cachedMethodInfo != null) {
                }
				key.refresh(go, key.getStartValue());
                return;
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
				if(key.easeType == AMKey.EaseTypeNone || !key.canTween)
					continue;
				else {
					if(cachedFieldInfo != null) {
						cachedFieldInfo.SetValue(comp, key.getEndValue());
						refreshTransform(go);
	                }
					else if(cachedPropertyInfo != null) {
						cachedPropertyInfo.SetValue(comp, key.getEndValue(), null);
						refreshTransform(go);
	                }
					else if(cachedMethodInfo != null) {
	                }
					key.refresh(go, key.getEndValue());
	                return;
				}
            }
            // else find value using easing function

            float framePositionInAction = frame - (float)key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            float t;

			if(key.easeType == AMKey.EaseTypeNone)
				t = 0.0f;
            else if(key.hasCustomEase()) {
                t = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(), key.easeCurve);
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                t = ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(), key.amplitude, key.period);
            }

            //qCurrent.x = ease(qStart.x,qEnd.x,percentage);
            if(key.valueType == (int)ValueType.Integer) {
                float vStartInteger = Convert.ToSingle(key.val);
                float vEndInteger = Convert.ToSingle(key.end_val);
                int vCurrentInteger = Mathf.RoundToInt(Mathf.Lerp(vStartInteger, vEndInteger, t));
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentInteger);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentInteger, null);
				refreshTransform(go); key.refresh(go, vCurrentInteger);
            }
            else if(key.valueType == (int)ValueType.Long) {
                float vStartLong = Convert.ToSingle(key.val);
                float vEndLong = Convert.ToSingle(key.end_val);
                long vCurrentLong = (long)Mathf.Lerp(vStartLong, vEndLong, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentLong);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentLong, null);
				refreshTransform(go); key.refresh(go, vCurrentLong);
            }
            else if(key.valueType == (int)ValueType.Float) {
                float vStartFloat = Convert.ToSingle(key.val);
                float vEndFloat = Convert.ToSingle(key.end_val);
                float vCurrentFloat = Mathf.Lerp(vStartFloat, vEndFloat, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentFloat);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentFloat, null);
				refreshTransform(go); key.refresh(go, vCurrentFloat);
            }
            else if(key.valueType == (int)ValueType.Double) {
                double vCurrentDouble = key.val + ((double)t) * (key.end_val - key.val);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentDouble);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentDouble, null);
				refreshTransform(go); key.refresh(go, vCurrentDouble);
            }
            else if(key.valueType == (int)ValueType.Vector2) {
                Vector2 vCurrentVector2 = Vector2.Lerp(key.vect2, key.end_vect2, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentVector2);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentVector2, null);
				refreshTransform(go); key.refresh(go, vCurrentVector2);
            }
            else if(key.valueType == (int)ValueType.Vector3) {
                Vector3 vCurrentVector3 = Vector3.Lerp(key.vect3, key.end_vect3, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentVector3);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentVector3, null);
				refreshTransform(go); key.refresh(go, vCurrentVector3);
            }
            else if(key.valueType == (int)ValueType.Color) {
                Color vCurrentColor = Color.Lerp(key.color, key.end_color, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentColor);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentColor, null);
				refreshTransform(go); key.refresh(go, vCurrentColor);
            }
            else if(key.valueType == (int)ValueType.Rect) {
                Rect vStartRect = key.rect;
                Rect vEndRect = key.end_rect;
                Rect vCurrentRect = new Rect();
                vCurrentRect.x = Mathf.Lerp(vStartRect.x, vEndRect.x, t);
                vCurrentRect.y = Mathf.Lerp(vStartRect.y, vEndRect.y, t);
                vCurrentRect.width = Mathf.Lerp(vStartRect.width, vEndRect.width, t);
                vCurrentRect.height = Mathf.Lerp(vStartRect.height, vEndRect.height, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentRect);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentRect, null);
				refreshTransform(go); key.refresh(go, vCurrentRect);
            }
            else if(key.valueType == (int)ValueType.Vector4) {
                Vector4 vCurrentVector4 = Vector4.Lerp(key.vect4, key.end_vect4, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentVector4);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentVector4, null);
				refreshTransform(go); key.refresh(go, vCurrentVector4);
            }
            else if(key.valueType == (int)ValueType.Quaternion) {
                Quaternion vCurrentQuat = Quaternion.Slerp(key.quat, key.end_quat, t);
				if(cachedFieldInfo != null) cachedFieldInfo.SetValue(comp, vCurrentQuat);
				else if(cachedPropertyInfo != null) cachedPropertyInfo.SetValue(comp, vCurrentQuat, null);
				refreshTransform(go); key.refresh(go, vCurrentQuat);
            }
            else {
                Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
            }

            return;
        }
    }
    public void refreshTransform(GameObject targetGO) {
		if(Application.isPlaying || !targetGO) return;
		Vector3 p = targetGO.transform.position;
		targetGO.transform.position = p;
    }

    public string getComponentName() {
		return componentName;
    }
    public string getValueInitialization(int codeLanguage, string varName) {
        string s = "";
        Debug.LogError("need implement");
        return s;
    }
    public static bool isValueTypeNumeric(int valueType) {
        if(valueType == (int)ValueType.Integer) return true;
        if(valueType == (int)ValueType.Long) return true;
        if(valueType == (int)ValueType.Float) return true;
        if(valueType == (int)ValueType.Double) return true;
        return false;
    }
    public bool hasSamePropertyAs(AMITarget target, AMPropertyTrack _track) {
		if(_track.GetTarget(target) == GetTarget(target) && _track.GetTargetComp(target) == GetTargetComp(target) && _track.getTrackType() == getTrackType())
            return true;
        return false;
    }

	public override AnimatorTimeline.JSONInit getJSONInit(AMITarget target) {
        Debug.LogError("need implement");
        return null;
    }

    public override List<GameObject> getDependencies(AMITarget target) {
		GameObject go = GetTarget(target) as GameObject;
        List<GameObject> ls = new List<GameObject>();
		if(go) ls.Add(go);
        return ls;
    }

	public override List<GameObject> updateDependencies(AMITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
		GameObject go = GetTarget(target) as GameObject;
        List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!go) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
			if(oldReferences[i] == go) {
                Component _component = newReferences[i].GetComponent(componentName);

                // missing component
                if(!_component) {
					Debug.LogWarning("Animator: Property Track component '" + componentName + "' not found on new reference for GameObject '" + go.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
                // missing property
                if(_component.GetType().GetProperty(propertyName) == null) {
					Debug.LogWarning("Animator: Property Track property '" + propertyName + "' not found on new reference for GameObject '" + go.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }

				SetTarget(target, newReferences[i]);
				setComponent(target, _component);
                break;
            }
        }
        return lsFlagToKeep;
    }

	protected override AMTrack doDuplicate(GameObject holder) {
        AMPropertyTrack ntrack = holder.AddComponent<AMPropertyTrack>();
        ntrack.enabled = false;
        ntrack.valueType = valueType;
        ntrack.obj = obj;
        ntrack.component = component;
		ntrack.componentName = componentName;
        ntrack.propertyName = propertyName;
        ntrack.fieldName = fieldName;
        ntrack.methodName = methodName;

        if(methodParameterTypes != null) {
            ntrack.methodParameterTypes = new string[methodParameterTypes.Length];
            Array.Copy(methodParameterTypes, ntrack.methodParameterTypes, methodParameterTypes.Length);
        }

        return ntrack;
    }
}
