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
        Enum = 14,
    }
    public int valueType;

	[SerializeField]
    GameObject obj;
	[SerializeField]
    Component component;
	[SerializeField]
	string componentName;
	[SerializeField]
    string fieldName;
    [SerializeField]
    string propertyName;

    public override int version { get { return 2; } }

    private FieldInfo cachedFieldInfo;
    private PropertyInfo cachedPropertyInfo;

	protected override void SetSerializeObject(UnityEngine.Object obj) {
		this.obj = obj as GameObject;

        cachedFieldInfo = null;
        cachedPropertyInfo = null;
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

    public Component GetTargetComp(GameObject targetGO) {
        return component ? component : targetGO.GetComponent(componentName);
    }

	public void RefreshData(Component comp) {
        Type t = comp.GetType();
        if(!string.IsNullOrEmpty(propertyName)) {
            cachedPropertyInfo = t.GetProperty(propertyName);
            cachedFieldInfo = null;
        }
        else if(!string.IsNullOrEmpty(fieldName)) {
            cachedPropertyInfo = null;
            cachedFieldInfo = t.GetField(fieldName);
        }
	}
    public Type GetCachedInfoType(AMITarget target) {
        if(cachedFieldInfo == null && cachedPropertyInfo == null)
            RefreshData(GetTargetComp(target));

        return GetCachedInfoType();
    }

    /// <summary>
    /// Assumes property info has been set
    /// </summary>
    /// <returns></returns>
    public PropertyInfo GetCachedPropertyInfo() {
        return cachedPropertyInfo;
    }
    public FieldInfo GetCachedFieldInfo() {
        return cachedFieldInfo;
    }
    Type GetCachedInfoType() {
        if(cachedFieldInfo != null)
            return cachedFieldInfo.FieldType;
        else if(cachedPropertyInfo != null)
            return cachedPropertyInfo.PropertyType;
        return null;
    }
    object getCachedInfoValue(AMITarget itarget) {
        if(cachedFieldInfo != null)
            return cachedFieldInfo.GetValue(GetTargetComp(itarget));
        else if(cachedPropertyInfo != null)
            return cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);
        return null;
    }
    void setComponentValueFromCachedInfo(Component comp, object val) {
        if(cachedFieldInfo != null) {
            if(valueType == (int)ValueType.Enum)
                cachedFieldInfo.SetValue(comp, System.Enum.ToObject(cachedFieldInfo.FieldType, val));
            else
                cachedFieldInfo.SetValue(comp, val);
        }
        else if(cachedPropertyInfo != null) {
            if(valueType == (int)ValueType.Enum)
                cachedPropertyInfo.SetValue(comp, System.Enum.ToObject(cachedPropertyInfo.PropertyType, val), null);
            else
                cachedPropertyInfo.SetValue(comp, val, null);
        }
    }
	/// <summary>
	/// Call this to reset info, make sure this track is empty, so clear the keys first!
	/// </summary>
	public void clearInfo() {
		fieldName = "";
        propertyName = "";
        cachedFieldInfo = null;
        cachedPropertyInfo = null;
	}
	public bool canTween {
		get {
			return !(valueType == (int)ValueType.Bool || valueType == (int)ValueType.String || valueType == (int)ValueType.Sprite || valueType == (int)ValueType.Enum);
		}
	}
    public string getMemberName() {
        if(!string.IsNullOrEmpty(fieldName)) return fieldName;
        else if(!string.IsNullOrEmpty(propertyName)) return propertyName;
        return "";
    }
    public override string getTrackType() {
        if(!string.IsNullOrEmpty(fieldName)) return fieldName;
        else if(!string.IsNullOrEmpty(propertyName)) return propertyName;
        return "Not Set";
    }
	public override string GetRequiredComponent() {
		return componentName;
	}
    public string getMemberInfoTypeName() {
        if(!string.IsNullOrEmpty(propertyName))
            return "PropertyInfo";
        else if(!string.IsNullOrEmpty(fieldName))
            return "FieldInfo";
        return "Undefined";
    }
    public bool isPropertySet() {
        return !string.IsNullOrEmpty(propertyName) || !string.IsNullOrEmpty(fieldName);
    }

    // add key
    public AMPropertyKey addKey(AMITarget target, OnAddKey addCall, int _frame) {
		Component comp = GetTargetComp(target);
		RefreshData(comp);

        AMPropertyKey k = null;

        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                k = key;
            }
        }

        if(k == null) {
            k = addCall(gameObject, typeof(AMPropertyKey)) as AMPropertyKey;
            k.frame = _frame;
            k.easeType = (int)EaseType.Linear;
            // add a new key
            keys.Add(k);
        }

        if(isValueTypeNumeric(valueType))
            k.setValue(Convert.ToDouble(getCachedInfoValue(target)));
        else if(valueType == (int)ValueType.Bool)
            k.setValue(Convert.ToBoolean(getCachedInfoValue(target)));
        else if(valueType == (int)ValueType.String)
            k.setValue(Convert.ToString(getCachedInfoValue(target)));
        else if(valueType == (int)ValueType.Vector2)
            k.setValue((Vector2)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Vector3)
            k.setValue((Vector3)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Color)
            k.setValue((Color)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Rect)
            k.setValue((Rect)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Vector4)
            k.setValue((Vector4)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Quaternion)
            k.setValue((Quaternion)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Sprite)
            k.setValue((UnityEngine.Object)getCachedInfoValue(target));
        else if(valueType == (int)ValueType.Enum)
            k.setValue(Convert.ToDouble(getCachedInfoValue(target)));
        else {
            Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
        }

        // update cache
        updateCache(target);
        return k;
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
            propertyName = n;
            cachedPropertyInfo = propertyInfo;
            setValueType(propertyInfo.PropertyType);
            fieldName = "";
            cachedFieldInfo = null;
            return true;
        }
        return false;
    }
    public bool setFieldInfo(FieldInfo fieldInfo) {
		string n = fieldInfo.Name;
        if(fieldName != n) {
            fieldName = n;
            cachedFieldInfo = fieldInfo;
            setValueType(fieldInfo.FieldType);
            propertyName = "";
            cachedPropertyInfo = null;
            return true;
        }
        return false;
    }
	public override void maintainTrack(AMITarget itarget) {
		base.maintainTrack(itarget);

		if(string.IsNullOrEmpty(componentName)) {
			if(component)
				componentName = component.GetType().Name;
		}

		if(itarget.TargetIsMeta()) {
			component = null;
		}
		else if(!component && !string.IsNullOrEmpty(componentName)) {
			GameObject go = GetTarget(itarget) as GameObject;
			if(go)
				component = go.GetComponent(componentName);
		}
	}
    // update cache (optimized)
    public override void updateCache(AMITarget target) {
        base.updateCache(target);

		Component comp = GetTargetComp(target);
		RefreshData(comp);

        for(int i = 0; i < keys.Count; i++) {
            AMPropertyKey key = keys[i] as AMPropertyKey;

            if(key.version > 0 && key.version != version) {
                //TODO: ...
            }

            key.version = version;

            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else {
				if(i > 0 && keys[i-1].easeType == AMKey.EaseTypeNone)
					key.easeType = AMKey.EaseTypeNone;

				key.endFrame = -1;
			}

            Type _type = GetCachedInfoType();
            if(_type == null) {
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
            else if(canTween) {
                Debug.LogError("Animator: Fatal Error, property type '" + _type.ToString() + "' not found.");
                key.destroy();
                return;
            }
        }
    }
        
    public static bool isNumeric(Type t) {
        if(t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double))
            return true;
        return false;
    }

    public static bool isValidType(Type t) {
		if(isNumeric(t) || t == typeof(bool) || t == typeof(string) || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Color) || t == typeof(Rect) || t == typeof(Vector4) || t == typeof(Quaternion) || t == typeof(Sprite) || t.IsEnum)
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
        else if(t.BaseType == typeof(System.Enum))
            valueType = (int)ValueType.Enum;
        else {
            valueType = -1;
            Debug.LogWarning("Animator: Value type " + t.ToString() + " is unsupported.");
        }

    }

    public override void previewFrame(AMITarget target, float frame, int frameRate, AMTrack extraTrack = null) {
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
            setComponentValueFromCachedInfo(comp, ckey.getStartValue(valueType));
            refreshTransform(go);
            return;
        }
        // if not tweenable and beyond last frame
        ckey = keys[keys.Count - 1] as AMPropertyKey;
        if(!canTween && frame >= (float)ckey.frame) {
            setComponentValueFromCachedInfo(comp, ckey.getStartValue(valueType));
            refreshTransform(go);
            return;
        }
        //if tweenable and beyond last tweenable
        ckey = keys[keys.Count - 2] as AMPropertyKey;
        if(frame >= (float)ckey.endFrame) {
            setComponentValueFromCachedInfo(comp, ckey.getEndValue(valueType));
            refreshTransform(go);
            return;
        }
        // if lies on property action
        foreach(AMPropertyKey key in keys) {
            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            //if(quickPreview && !key.targetsAreEqual()) return;	// quick preview; if action will execute then skip
            // if on startFrame or is no tween
            if(frame == (float)key.frame || ((key.easeType == AMKey.EaseTypeNone || !canTween) && frame < (float)key.endFrame)) {
                setComponentValueFromCachedInfo(comp, key.getStartValue(valueType));
                refreshTransform(go);
                return;
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
                if(key.easeType == AMKey.EaseTypeNone || !canTween)
                    continue;
                else {
                    setComponentValueFromCachedInfo(comp, key.getEndValue(valueType));
                    refreshTransform(go);
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
                t = AMUtil.EaseCustom(0.0f, 1.0f, framePositionInAction / key.getNumberOfFrames(frameRate), key.easeCurve);
            }
            else {
                TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)key.easeType);
                t = ease(framePositionInAction, 0.0f, 1.0f, key.getNumberOfFrames(frameRate), key.amplitude, key.period);
            }

            //qCurrent.x = ease(qStart.x,qEnd.x,percentage);
            switch((ValueType)valueType) {
                case ValueType.Integer:
                    setComponentValueFromCachedInfo(comp, Mathf.RoundToInt(Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(key.end_val), t)));
                    break;
                case ValueType.Long:
                    setComponentValueFromCachedInfo(comp, (long)Mathf.RoundToInt(Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(key.end_val), t)));
                    break;
                case ValueType.Float:
                    setComponentValueFromCachedInfo(comp, Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(key.end_val), t));
                    break;
                case ValueType.Double:
                    setComponentValueFromCachedInfo(comp, key.val + ((double)t) * (key.end_val - key.val));
                    break;
                case ValueType.Vector2:
                    setComponentValueFromCachedInfo(comp, Vector2.Lerp(key.vect2, key.end_vect2, t));
                    break;
                case ValueType.Vector3:
                    setComponentValueFromCachedInfo(comp, Vector3.Lerp(key.vect3, key.end_vect3, t));
                    break;
                case ValueType.Color:
                    setComponentValueFromCachedInfo(comp, Color.Lerp(key.color, key.end_color, t));
                    break;
                case ValueType.Rect:
                    Rect vStartRect = key.rect;
                    Rect vEndRect = key.end_rect;
                    Rect vCurrentRect = new Rect();
                    vCurrentRect.x = Mathf.Lerp(vStartRect.x, vEndRect.x, t);
                    vCurrentRect.y = Mathf.Lerp(vStartRect.y, vEndRect.y, t);
                    vCurrentRect.width = Mathf.Lerp(vStartRect.width, vEndRect.width, t);
                    vCurrentRect.height = Mathf.Lerp(vStartRect.height, vEndRect.height, t);
                    setComponentValueFromCachedInfo(comp, vCurrentRect);
                    break;
                case ValueType.Vector4:
                    setComponentValueFromCachedInfo(comp, Vector4.Lerp(key.vect4, key.end_vect4, t));
                    break;
                case ValueType.Quaternion:
                    setComponentValueFromCachedInfo(comp, Quaternion.Slerp(key.quat, key.end_quat, t));
                    break;
                default:
                    Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
                    break;
            }
            refreshTransform(go);
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
                if(_component.GetType().GetProperty(fieldName) == null) {
					Debug.LogWarning("Animator: Property Track property '" + fieldName + "' not found on new reference for GameObject '" + go.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }

				SetTarget(target, newReferences[i].transform);
				setComponent(target, _component);
                break;
            }
        }
        return lsFlagToKeep;
    }

    protected override void DoCopy(AMTrack track) {
        AMPropertyTrack ntrack = track as AMPropertyTrack;
        ntrack.valueType = valueType;
        ntrack.obj = obj;
        ntrack.component = component;
		ntrack.componentName = componentName;
        ntrack.fieldName = fieldName;
    }
}
