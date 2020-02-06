using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class PropertyTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Property; } }

        public enum ValueType {
            Invalid = -1,

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
        public ValueType valueType;

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
        private bool isCached;

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            this.obj = string.IsNullOrEmpty(_targetPath) ? obj as GameObject : null;

            //verify component, if invalid, reset component
            if(!string.IsNullOrEmpty(componentName) && obj is GameObject) {
                var go = (GameObject)obj;
                if(go.GetComponent(componentName) == null)
                    componentName = "";
            }

            //reset property if no component
            if(string.IsNullOrEmpty(componentName)) {
                fieldName = "";
                propertyName = "";
            }

            //reset cache
            component = null;
            cachedFieldInfo = null;
            cachedPropertyInfo = null;
            isCached = false;
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            return targetGO ? targetGO : obj;
        }

        Component GetTargetComp(ITarget target) {
            Component comp;
            if(target.meta) {
                component = null;
                GameObject go = GetTarget(target) as GameObject;
                comp = go ? go.GetComponent(componentName) : null;
            }
            else {
                //need to re-grab component
                if(!component && !string.IsNullOrEmpty(componentName)) {
                    GameObject go = GetTarget(target) as GameObject;
                    component = go ? go.GetComponent(componentName) : null;
                }

                comp = component;
            }
            return comp;
        }

        public Component GetTargetComp(GameObject targetGO) {
            return component ? component : targetGO.GetComponent(componentName);
        }

        public void RefreshData(Component comp) {
            if(!comp) return;

            Type t = comp.GetType();
            if(!string.IsNullOrEmpty(propertyName)) {
                cachedPropertyInfo = t.GetProperty(propertyName);
                cachedFieldInfo = null;
            }
            else if(!string.IsNullOrEmpty(fieldName)) {
                cachedPropertyInfo = null;
                cachedFieldInfo = t.GetField(fieldName);
            }
            isCached = true;
        }
        public Type GetCachedInfoType(ITarget target) {
            if(!isCached)
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
        object getCachedInfoValue(ITarget itarget) {
            if(cachedFieldInfo != null)
                return cachedFieldInfo.GetValue(GetTargetComp(itarget));
            else if(cachedPropertyInfo != null)
                return cachedPropertyInfo.GetValue(GetTargetComp(itarget), null);
            return null;
        }
        void setComponentValueFromCachedInfo(Component comp, object val) {
            if(cachedFieldInfo != null) {
                if(valueType == ValueType.Enum)
                    cachedFieldInfo.SetValue(comp, System.Enum.ToObject(cachedFieldInfo.FieldType, val));
                else
                    cachedFieldInfo.SetValue(comp, val);
            }
            else if(cachedPropertyInfo != null) {
                if(valueType == ValueType.Enum)
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
            isCached = false;
        }
        public override bool canTween {
            get {
                ValueType vt = (ValueType)valueType;
                return !(vt == ValueType.Bool || vt == ValueType.String || vt == ValueType.Sprite || vt == ValueType.Enum);
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
        public override bool CheckComponent(GameObject go) {
            if(string.IsNullOrEmpty(componentName)) return true;

            return go.GetComponent(componentName) != null;
        }
        public override void AddComponent(GameObject go) {
            if(string.IsNullOrEmpty(componentName))
                return;

            var type = Type.GetType(componentName);
            if(type != null)
                go.AddComponent(type);
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
        public PropertyKey addKey(ITarget target, int _frame) {
            Component comp = GetTargetComp(target);
            RefreshData(comp);

            PropertyKey k = null, prevKey = null;

            foreach(PropertyKey key in keys) {
                // if key exists on frame, update key
                if(key.frame == _frame) {
                    k = key;
                }
                else if(key.frame < _frame)
                    prevKey = key;
            }

            if(k == null) {
                k = new PropertyKey();
                k.frame = _frame;

                //copy previous frame tween settings
                if(prevKey != null) {
                    k.interp = prevKey.interp;
                    k.easeType = prevKey.easeType;
                    k.easeCurve = prevKey.easeCurve;
                }
                else
                    k.interp = canTween ? Key.Interpolation.Linear : Key.Interpolation.None; //default

                // add a new key
                keys.Add(k);
            }

            if(isValueTypeNumeric(valueType))
                k.val = Convert.ToDouble(getCachedInfoValue(target));
            else if(valueType == ValueType.Bool)
                k.valb = Convert.ToBoolean(getCachedInfoValue(target));
            else if(valueType == ValueType.String)
                k.valString = Convert.ToString(getCachedInfoValue(target));
            else if(valueType == ValueType.Vector2)
                k.vect2 = (Vector2)getCachedInfoValue(target);
            else if(valueType == ValueType.Vector3)
                k.vect3 = (Vector3)getCachedInfoValue(target);
            else if(valueType == ValueType.Color)
                k.color = (Color)getCachedInfoValue(target);
            else if(valueType == ValueType.Rect)
                k.rect = (Rect)getCachedInfoValue(target);
            else if(valueType == ValueType.Vector4)
                k.vect4 = (Vector4)getCachedInfoValue(target);
            else if(valueType == ValueType.Quaternion)
                k.quat = (Quaternion)getCachedInfoValue(target);
            else if(valueType == ValueType.Sprite)
                k.valObj = (UnityEngine.Object)getCachedInfoValue(target);
            else if(valueType == ValueType.Enum)
                k.val = Convert.ToDouble(getCachedInfoValue(target));
            else {
                Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
            }

            // update cache
            updateCache(target);
            return k;
        }

        public bool setComponent(ITarget target, Component component) {
            if(target.meta) {
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
                isCached = true;
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
                isCached = true;
                return true;
            }
            return false;
        }
        public override void maintainTrack(ITarget itarget) {
            base.maintainTrack(itarget);

            if(string.IsNullOrEmpty(componentName)) {
                if(component)
                    componentName = component.GetType().Name;
            }

            if(itarget.meta) {
                component = null;
            }
            else if(!component && !string.IsNullOrEmpty(componentName)) {
                GameObject go = GetTarget(itarget) as GameObject;
                if(go)
                    component = go.GetComponent(componentName);
            }
        }
        // update cache (optimized)
        public override void updateCache(ITarget target) {
            base.updateCache(target);

            Component comp = GetTargetComp(target);
            if(comp == null)
                return;

            RefreshData(comp);

            Type _type = GetCachedInfoType();
            if(_type == null) {
                Debug.LogError("Animator: Fatal Error; fieldInfo, propertyInfo and methodInfo are unset for Value Type " + valueType);
                return;
            }

            for(int i = 0; i < keys.Count; i++) {
                PropertyKey key = keys[i] as PropertyKey;
                PropertyKey keyNext = i + 1 < keys.Count ? keys[i + 1] as PropertyKey : null;

                if(key.version > 0 && key.version != version) {
                    //TODO: ...
                }

                key.version = version;

                if(keyNext != null) key.endFrame = keyNext.frame;
                else {
                    key.endFrame = key.canTween ? -1 : key.frame; //last key, invalid if tween, one frame for end
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
        public void setValueType(Type t) {

            if(t == typeof(int))
                valueType = ValueType.Integer;
            else if(t == typeof(long))
                valueType = ValueType.Long;
            else if(t == typeof(float))
                valueType = ValueType.Float;
            else if(t == typeof(double))
                valueType = ValueType.Double;
            else if(t == typeof(Vector2))
                valueType = ValueType.Vector2;
            else if(t == typeof(Vector3))
                valueType = ValueType.Vector3;
            else if(t == typeof(Color))
                valueType = ValueType.Color;
            else if(t == typeof(Rect))
                valueType = ValueType.Rect;
            else if(t == typeof(Vector4))
                valueType = ValueType.Vector4;
            else if(t == typeof(Quaternion))
                valueType = ValueType.Quaternion;
            else if(t == typeof(bool))
                valueType = ValueType.Bool;
            else if(t == typeof(string))
                valueType = ValueType.String;
            else if(t == typeof(Sprite))
                valueType = ValueType.Sprite;
            else if(t.BaseType == typeof(System.Enum))
                valueType = ValueType.Enum;
            else {
                valueType = ValueType.Invalid;
                Debug.LogWarning("Animator: Value type " + t.ToString() + " is unsupported.");
            }

        }

        public override void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) {
            if(keys == null || keys.Count <= 0) {
                return;
            }

            GameObject go = GetTarget(target) as GameObject;
            Component comp = GetTargetComp(target);

            if(!comp || !go) return;

            if(!isCached)
                RefreshData(comp);

            // if before or equal to first frame, or is the only frame
            PropertyKey firstKey = keys[0] as PropertyKey;
            if(firstKey.endFrame == -1 || (frame <= (float)firstKey.frame && !firstKey.canTween)) {
                //go.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
                setComponentValueFromCachedInfo(comp, firstKey.getValue(valueType));
                if(comp is Transform)
                    refreshTransform(go);
                return;
            }

            // if lies on property action
            for(int i = 0; i < keys.Count; i++) {
                PropertyKey key = keys[i] as PropertyKey;
                PropertyKey keyNext = i + 1 < keys.Count ? keys[i + 1] as PropertyKey : null;

                if(frame >= (float)key.endFrame && keyNext != null && (!keyNext.canTween || keyNext.endFrame != -1)) continue;
                // if no ease
                if(!key.canTween || keyNext == null) {
                    setComponentValueFromCachedInfo(comp, key.getValue(valueType));
                    if(comp is Transform)
                        refreshTransform(go);
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
                    var ease = Utility.GetEasingFunction(key.easeType);
                    t = ease(framePositionInAction, key.getNumberOfFrames(frameRate), key.amplitude, key.period);
                }

                //qCurrent.x = ease(qStart.x,qEnd.x,percentage);
                switch((ValueType)valueType) {
                    case ValueType.Integer:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Mathf.RoundToInt(Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(keyNext.val), t)) : Convert.ToInt32(key.val));
                        break;
                    case ValueType.Long:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? (long)Mathf.RoundToInt(Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(keyNext.val), t)) : Convert.ToInt64(key.val));
                        break;
                    case ValueType.Float:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Mathf.Lerp(Convert.ToSingle(key.val), Convert.ToSingle(keyNext.val), t) : Convert.ToSingle(key.val));
                        break;
                    case ValueType.Double:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? key.val + ((double)t) * (keyNext.val - key.val) : key.val);
                        break;
                    case ValueType.Vector2:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Vector2.Lerp(key.vect2, keyNext.vect2, t) : key.vect2);
                        break;
                    case ValueType.Vector3:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Vector3.Lerp(key.vect3, keyNext.vect3, t) : key.vect3);
                        break;
                    case ValueType.Color:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Color.Lerp(key.color, keyNext.color, t) : key.color);
                        break;
                    case ValueType.Rect:
                        if(keyNext != null) {
                            Rect vStartRect = key.rect;
                            Rect vEndRect = keyNext.rect;
                            Rect vCurrentRect = new Rect();
                            vCurrentRect.x = Mathf.Lerp(vStartRect.x, vEndRect.x, t);
                            vCurrentRect.y = Mathf.Lerp(vStartRect.y, vEndRect.y, t);
                            vCurrentRect.width = Mathf.Lerp(vStartRect.width, vEndRect.width, t);
                            vCurrentRect.height = Mathf.Lerp(vStartRect.height, vEndRect.height, t);
                            setComponentValueFromCachedInfo(comp, vCurrentRect);
                        }
                        else
                            setComponentValueFromCachedInfo(comp, key.rect);
                        break;
                    case ValueType.Vector4:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Vector4.Lerp(key.vect4, keyNext.vect4, t) : key.vect4);
                        break;
                    case ValueType.Quaternion:
                        setComponentValueFromCachedInfo(comp, keyNext != null ? Quaternion.Slerp(key.quat, keyNext.quat, t) : key.quat);
                        break;
                    default:
                        Debug.LogError("Animator: Invalid ValueType " + valueType.ToString());
                        break;
                }
                if(comp is Transform)
                    refreshTransform(go);
                return;
            }
        }
        public void refreshTransform(GameObject targetGO) {
            if(Application.isPlaying || !targetGO) return;
            Vector3 lp = targetGO.transform.localPosition;
            targetGO.transform.localPosition = lp;
        }

        public string getComponentName() {
            return componentName;
        }
        public string getValueInitialization(int codeLanguage, string varName) {
            string s = "";
            Debug.LogError("need implement");
            return s;
        }
        public static bool isValueTypeNumeric(ValueType valueType) {
            if(valueType == ValueType.Integer) return true;
            if(valueType == ValueType.Long) return true;
            if(valueType == ValueType.Float) return true;
            if(valueType == ValueType.Double) return true;
            return false;
        }
        public bool hasSamePropertyAs(ITarget target, PropertyTrack _track) {
            if(_track != null && _track.getTrackType() == getTrackType())
                return true;
            return false;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            Debug.LogError("need implement");
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            GameObject go = GetTarget(target) as GameObject;
            List<GameObject> ls = new List<GameObject>();
            if(go) ls.Add(go);
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
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

                    SetTarget(target, newReferences[i].transform, !string.IsNullOrEmpty(targetPath));
                    setComponent(target, _component);
                    break;
                }
            }
            return lsFlagToKeep;
        }

        protected override void DoCopy(Track track) {
            var ntrack = track as PropertyTrack;
            ntrack.valueType = valueType;
            ntrack.obj = obj;
            ntrack.component = component;
            ntrack.componentName = componentName;
            ntrack.fieldName = fieldName;
            ntrack.propertyName = propertyName;
        }
    }
}