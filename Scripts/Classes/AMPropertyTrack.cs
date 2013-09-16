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
        Quaternion = 11
    }
    public int valueType;
    public GameObject obj;
    public Component component;
    public string propertyName;
    public string fieldName;
    public string methodName;
    public string[] methodParameterTypes;
    private PropertyInfo cachedPropertyInfo;
    private FieldInfo cachedFieldInfo;
    private MethodInfo cachedMethodInfo;
    public PropertyInfo propertyInfo {
        get {
            if(cachedPropertyInfo != null) {
                return cachedPropertyInfo;
            }
            if(!obj || !component || propertyName == null) {
                return null;
            }
            cachedPropertyInfo = component.GetType().GetProperty(propertyName);
            return cachedPropertyInfo;
        }
        set {
            if(value != null) propertyName = value.Name;
            else propertyName = null;
            cachedPropertyInfo = value;

        }
    }
    public FieldInfo fieldInfo {
        get {
            if(cachedFieldInfo != null) return cachedFieldInfo;
            if(!obj || !component || fieldName == null) return null;
            cachedFieldInfo = component.GetType().GetField(fieldName);
            return cachedFieldInfo;
        }
        set {
            if(value != null) fieldName = value.Name;
            else fieldName = null;
            cachedFieldInfo = value;
        }
    }		// holds a field such as variables for user scripts, should be null if property is used
    public MethodInfo methodInfo {
        get {
            if(cachedMethodInfo != null) return cachedMethodInfo;
            if(!obj || !component || methodName == null) return null;
            Type[] t = new Type[methodParameterTypes.Length];
            for(int i = 0; i < methodParameterTypes.Length; i++) t[i] = Type.GetType(methodParameterTypes[i]);
            cachedMethodInfo = component.GetType().GetMethod(methodName, t);
            return cachedMethodInfo;
        }
        set {
            if(value != null) methodName = value.Name;
            else methodName = null;
            cachedMethodInfo = value;
        }
    }
    public override string getTrackType() {
        if(fieldInfo != null) return fieldInfo.Name;
        if(propertyInfo != null) return propertyInfo.Name;
        if(methodInfo != null) {

        }
        return "Not Set";
    }
    public string getMemberInfoTypeName() {
        if(fieldInfo != null) return "FieldInfo";
        if(propertyInfo != null) return "PropertyInfo";
        if(methodInfo != null) return "MethodInfo";
        return "Undefined";
    }
    public bool isPropertySet() {
        if(fieldInfo != null) return true;
        if(propertyInfo != null) return true;
        if(methodInfo != null) {
        }
        return false;
    }

    // add key
    public AMKey addKey(int _frame) {
        if(isValueTypeNumeric(valueType))
            return addKey(_frame, getPropertyValueNumeric());
        else if(valueType == (int)ValueType.Vector2)
            return addKey(_frame, getPropertyValueVector2());
        else if(valueType == (int)ValueType.Vector3)
            return addKey(_frame, getPropertyValueVector3());
        else if(valueType == (int)ValueType.Color)
            return addKey(_frame, getPropertyValueColor());
        else if(valueType == (int)ValueType.Rect)
            return addKey(_frame, getPropertyValueRect());
        else if(valueType == (int)ValueType.Vector4)
            return addKey(_frame, getPropertyValueVector4());
        else if(valueType == (int)ValueType.Quaternion)
            return addKey(_frame, getPropertyValueQuaternion());
        else {
            Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
            return null;
        }
    }
    // add key numeric
    public AMKey addKey(int _frame, double val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key vector2
    public AMKey addKey(int _frame, Vector2 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key vector3
    public AMKey addKey(int _frame, Vector3 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key color
    public AMKey addKey(int _frame, Color val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key rect
    public AMKey addKey(int _frame, Rect val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key vector4
    public AMKey addKey(int _frame, Vector4 val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // add key quaternion
    public AMKey addKey(int _frame, Quaternion val) {
        foreach(AMPropertyKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                key.setValue(val);
                // update cache
                updateCache();
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
        updateCache();
        return a;
    }
    // determines whether the supplied object is equal to the object in this instance
    public bool isObjectUnique(GameObject obj) {
        if(this.obj != obj) return true;
        return false;
    }
    public bool setObject(GameObject obj) {
        if(this.obj != obj) {
            this.obj = obj;
            fieldInfo = null;
            propertyInfo = null;
            methodInfo = null;
            return true;
        }
        return false;
    }
    public bool setComponent(Component component) {
        if(this.component != component) {
            this.component = component;
            return true;
        }
        return false;
    }
    public bool setPropertyInfo(PropertyInfo propertyInfo) {
        if(this.propertyInfo != propertyInfo) {
            // set the property value type
            setValueType(propertyInfo.PropertyType);
            this.propertyInfo = propertyInfo;
            fieldInfo = null;
            return true;
        }
        return false;
    }
    public bool setFieldInfo(FieldInfo fieldInfo) {
        if(this.fieldInfo != fieldInfo) {
            setValueType(fieldInfo.FieldType);
            this.fieldInfo = fieldInfo;
            propertyInfo = null;
            return true;
        }
        return false;
    }

    public bool setMethodInfo(MethodInfo methodInfo, string[] parameterTypes, ValueType valueType) {
        if(this.valueType != (int)valueType || this.methodParameterTypes != parameterTypes || this.methodInfo != methodInfo) {
            setValueType((int)valueType);
            methodParameterTypes = parameterTypes;
            this.methodInfo = methodInfo;
            propertyInfo = null;
            fieldInfo = null;
            return true;
        }
        return false;
    }
    // update cache (optimized)
    public override void updateCache() {
        // sort keys
        sortKeys();
        for(int i = 0; i < keys.Count; i++) {
            AMPropertyKey key = keys[i] as AMPropertyKey;

            key.version = version;

            key.component = component;
            if(keys.Count > (i + 1)) key.endFrame = keys[i + 1].frame;
            else key.endFrame = -1;
            key.valueType = valueType;
            Type _type = null;
            bool showError = true;
            //int methodInfoType = -1;
            if(fieldInfo != null) {
                key.fieldInfo = fieldInfo;
                _type = fieldInfo.FieldType;
                showError = false;
            }
            else if(propertyInfo != null) {
                key.propertyInfo = propertyInfo;
                _type = propertyInfo.PropertyType;
                showError = false;
            }
            else if(methodInfo != null) {
                key.methodInfo = methodInfo;
                key.methodParameterTypes = methodParameterTypes;
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
            else {
                Debug.LogError("Animator: Fatal Error, property type '" + _type.ToString() + "' not found.");
                key.destroy();
                return;
            }
        }
        base.updateCache();

    }

    // get numeric value. unsafe, must check to see if value is numeric first
    public double getPropertyValueNumeric() {
        // field
        if(fieldInfo != null)
            return Convert.ToDouble(fieldInfo.GetValue(component));
        // property
        else {
            return Convert.ToDouble(propertyInfo.GetValue(component, null));
        }

    }
    // get Vector2 value. unsafe, must check to see if value is Vector2 first
    public Vector2 getPropertyValueVector2() {
        // field
        if(fieldInfo != null)
            return (Vector2)fieldInfo.GetValue(component);
        // property
        else
            return (Vector2)propertyInfo.GetValue(component, null);

    }
    public Vector3 getPropertyValueVector3() {
        // field
        if(fieldInfo != null)
            return (Vector3)fieldInfo.GetValue(component);
        // property
        else
            return (Vector3)propertyInfo.GetValue(component, null);

    }
    public Vector4 getPropertyValueVector4() {
        // field
        if(fieldInfo != null)
            return (Vector4)fieldInfo.GetValue(component);
        // property
        else
            return (Vector4)propertyInfo.GetValue(component, null);

    }
    public Quaternion getPropertyValueQuaternion() {
        // field
        if(fieldInfo != null)
            return (Quaternion)fieldInfo.GetValue(component);
        // property
        else
            return (Quaternion)propertyInfo.GetValue(component, null);

    }
    public Color getPropertyValueColor() {
        // field
        if(fieldInfo != null)
            return (Color)fieldInfo.GetValue(component);
        // property
        else
            return (Color)propertyInfo.GetValue(component, null);

    }
    public Rect getPropertyValueRect() {
        // field
        if(fieldInfo != null)
            return (Rect)fieldInfo.GetValue(component);
        // property
        else
            return (Rect)propertyInfo.GetValue(component, null);

    }
    public static bool isNumeric(Type t) {
        if((t == typeof(int)) || (t == typeof(long)) || (t == typeof(float)) || (t == typeof(double)))
            return true;
        return false;
    }

    public static bool isValidType(Type t) {
        if((t == typeof(int)) || (t == typeof(long)) || (t == typeof(float)) || (t == typeof(double)) || (t == typeof(Vector2)) || (t == typeof(Vector3)) || (t == typeof(Color)) || (t == typeof(Rect)) || (t == typeof(Vector4)) || (t == typeof(Quaternion)))
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
        else {
            valueType = -1;
            Debug.LogWarning("Animator: Value type " + t.ToString() + " is unsupported.");
        }

    }
    // preview a frame in the scene view
    public void previewFrame(float frame, bool quickPreview = false) {
        if(keys == null || keys.Count <= 0) {
            return;
        }
        if(!component || !obj) return;

        // if before or equal to first frame, or is the only frame
        if((frame <= (float)keys[0].frame) || ((keys[0] as AMPropertyKey).endFrame == -1)) {
            //obj.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
            if(fieldInfo != null) {
                fieldInfo.SetValue(component, (keys[0] as AMPropertyKey).getStartValue());
                refreshTransform();
            }
            else if(propertyInfo != null) {
                propertyInfo.SetValue(component, (keys[0] as AMPropertyKey).getStartValue(), null);
                refreshTransform();
            }
            else if(methodInfo != null) {
            }
            return;
        }
        // if beyond or equal to last frame
        if(frame >= (float)(keys[keys.Count - 2] as AMPropertyKey).endFrame) {
            if(fieldInfo != null) {
                fieldInfo.SetValue(component, (keys[keys.Count - 2] as AMPropertyKey).getEndValue());
                refreshTransform();
            }
            else if(propertyInfo != null) {
                propertyInfo.SetValue(component, (keys[keys.Count - 2] as AMPropertyKey).getEndValue(), null);
                refreshTransform();
            }
            else if(methodInfo != null) {
            }
            return;
        }
        // if lies on property action
        foreach(AMPropertyKey key in keys) {
            if((frame < (float)key.frame) || (frame > (float)key.endFrame)) continue;
            if(quickPreview && !key.targetsAreEqual()) return;	// quick preview; if action will execute then skip
            // if on startFrame
            if(frame == (float)key.frame) {
                if(fieldInfo != null) {
                    fieldInfo.SetValue(component, key.getStartValue());
                    refreshTransform();
                }
                else if(propertyInfo != null) {
                    propertyInfo.SetValue(component, key.getStartValue(), null);
                    refreshTransform();
                }
                else if(methodInfo != null) {
                }
                return;
            }
            // if on endFrame
            if(frame == (float)key.endFrame) {
                if(fieldInfo != null) {
                    fieldInfo.SetValue(component, key.getEndValue());
                    refreshTransform();
                }
                else if(propertyInfo != null) {
                    propertyInfo.SetValue(component, key.getEndValue(), null);
                    refreshTransform();
                }
                else if(methodInfo != null) {
                }
                return;
            }
            // else find value using easing function

            float framePositionInAction = frame - (float)key.frame;
            if(framePositionInAction < 0f) framePositionInAction = 0f;

            float t = 0.0f;

            if(key.hasCustomEase()) {
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
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentInteger);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentInteger, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Long) {
                float vStartLong = Convert.ToSingle(key.val);
                float vEndLong = Convert.ToSingle(key.end_val);
                long vCurrentLong = (long)Mathf.Lerp(vStartLong, vEndLong, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentLong);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentLong, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Float) {
                float vStartFloat = Convert.ToSingle(key.val);
                float vEndFloat = Convert.ToSingle(key.end_val);
                float vCurrentFloat = Mathf.Lerp(vStartFloat, vEndFloat, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentFloat);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentFloat, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Double) {
                double vCurrentDouble = key.val + ((double)t) * (key.end_val - key.val);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentDouble);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentDouble, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Vector2) {
                Vector2 vCurrentVector2 = Vector2.Lerp(key.vect2, key.end_vect2, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentVector2);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentVector2, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Vector3) {
                Vector3 vCurrentVector3 = Vector3.Lerp(key.vect3, key.end_vect3, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentVector3);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentVector3, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Color) {
                Color vCurrentColor = Color.Lerp(key.color, key.end_color, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentColor);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentColor, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Rect) {
                Rect vStartRect = key.rect;
                Rect vEndRect = key.end_rect;
                Rect vCurrentRect = new Rect();
                vCurrentRect.x = Mathf.Lerp(vStartRect.x, vEndRect.x, t);
                vCurrentRect.y = Mathf.Lerp(vStartRect.y, vEndRect.y, t);
                vCurrentRect.width = Mathf.Lerp(vStartRect.width, vEndRect.width, t);
                vCurrentRect.height = Mathf.Lerp(vStartRect.height, vEndRect.height, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentRect);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentRect, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Vector4) {
                Vector4 vCurrentVector4 = Vector4.Lerp(key.vect4, key.end_vect4, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentVector4);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentVector4, null);
                refreshTransform();
            }
            else if(key.valueType == (int)ValueType.Quaternion) {
                Quaternion vCurrentQuat = Quaternion.Slerp(key.quat, key.end_quat, t);
                if(fieldInfo != null) fieldInfo.SetValue(component, vCurrentQuat);
                else if(propertyInfo != null) propertyInfo.SetValue(component, vCurrentQuat, null);
                refreshTransform();
            }
            else {
                Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
            }

            return;
        }
    }
    public void refreshTransform() {
        if(Application.isPlaying || !obj) return;
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);
    }

    public string getComponentName() {
        if(fieldInfo != null) return fieldInfo.DeclaringType.Name;
        if(propertyInfo != null) return propertyInfo.DeclaringType.Name;
        if(methodInfo != null) {
        }
        return "Unknown";
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
    public bool hasSamePropertyAs(AMPropertyTrack _track) {
        if(_track.obj == obj && _track.component == component && _track.getTrackType() == getTrackType())
            return true;
        return false;
    }

    public override AnimatorTimeline.JSONInit getJSONInit() {
        Debug.LogError("need implement");
        return null;
    }

    public override List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        if(obj) ls.Add(obj);
        return ls;
    }

    public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        if(!obj) return lsFlagToKeep;
        for(int i = 0; i < oldReferences.Count; i++) {
            if(oldReferences[i] == obj) {
                string componentName = component.GetType().Name;
                Component _component = newReferences[i].GetComponent(componentName);

                // missing component
                if(!_component) {
                    Debug.LogWarning("Animator: Property Track component '" + componentName + "' not found on new reference for GameObject '" + obj.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }
                // missing property
                if(_component.GetType().GetProperty(propertyName) == null) {
                    Debug.LogWarning("Animator: Property Track property '" + propertyName + "' not found on new reference for GameObject '" + obj.name + "'. Duplicate not replaced.");
                    lsFlagToKeep.Add(oldReferences[i]);
                    return lsFlagToKeep;
                }

                obj = newReferences[i];
                component = _component;
                break;
            }
        }
        return lsFlagToKeep;
    }

    protected override AMTrack doDuplicate(AMTake newTake) {
        AMPropertyTrack ntrack = newTake.gameObject.AddComponent<AMPropertyTrack>();
        ntrack.enabled = false;
        ntrack.valueType = valueType;
        ntrack.obj = obj;
        ntrack.component = component;
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
