using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace M8.Animator {
    [System.Serializable]
    public class EventData {
        public enum ValueType {
            Integer = 0,
            Long = 1,
            Float = 2,
            Double = 3,
            Vector2 = 4,
            Vector3 = 5,
            Vector4 = 6,
            Color = 7,
            Rect = 8,
            String = 9,
            Char = 10,
            Object = 11,
            Array = 12,
            Boolean = 13,
            Enum = 14,

            //special case for storing paths
            GameObject=100,
            Component=101,

            Invalid = -1
        }

        public string paramName;

        public ValueType valueType;

        public int val_int;
        public string val_string;
        public Vector4 val_vect4;
        public UnityEngine.Object val_obj;

        public static ValueType GetValueType(Type t) {
            ValueType valueType = ValueType.Invalid;
            if(t == typeof(bool)) valueType = ValueType.Boolean;
            else if(t == typeof(string)) valueType = ValueType.String;
            else if(t == typeof(char)) valueType = ValueType.Char;
            else if(t == typeof(int)) valueType = ValueType.Integer;
            else if(t == typeof(long)) valueType = ValueType.Long;
            else if(t == typeof(float)) valueType = ValueType.Float;
            else if(t == typeof(double)) valueType = ValueType.Double;
            else if(t == typeof(Vector2)) valueType = ValueType.Vector2;
            else if(t == typeof(Vector3)) valueType = ValueType.Vector3;
            else if(t == typeof(Vector4)) valueType = ValueType.Vector4;
            else if(t == typeof(Color)) valueType = ValueType.Color;
            else if(t == typeof(Rect)) valueType = ValueType.Rect;
            else if(t.IsArray) valueType = ValueType.Array;
            else if(t.IsEnum) valueType = ValueType.Enum;
            else if(t == typeof(GameObject)) valueType = ValueType.GameObject;
            else if(Utility.IsDerivedFromAny(t, typeof(Behaviour), typeof(MonoBehaviour), typeof(Component))) valueType = ValueType.Component;
            else if(Utility.IsDerivedFrom(t, typeof(UnityEngine.Object))) valueType = ValueType.Object;

            return valueType;
        }

        public bool val_bool { get { return val_int == 1; } set { val_int = value ? 1 : 0; } }
        public long val_long { get { return val_int; } set { val_int = (int)value; } }
        public float val_float { get { return val_vect4.x; } set { val_vect4.Set(value, 0, 0, 0); } }
        public double val_double { get { return val_float; } set { val_float = (float)value; } }
        public Vector2 val_vect2 { get { return new Vector2(val_vect4.x, val_vect4.y); } set { val_vect4.Set(value.x, value.y, 0, 0); } }
        public Vector3 val_vect3 { get { return new Vector3(val_vect4.x, val_vect4.y, val_vect4.z); } set { val_vect4.Set(value.x, value.y, value.z, 0); } }

        public Color val_color { get { return new Color(val_vect4.x, val_vect4.y, val_vect4.z, val_vect4.w); } set { val_vect4.Set(value.r, value.g, value.b, value.a); } }
        public Rect val_rect { get { return new Rect(val_vect4.x, val_vect4.y, val_vect4.z, val_vect4.w); } set { val_vect4.Set(value.xMin, value.yMin, value.width, value.height); } }
        public Enum val_enum { get { var enumType = getParamType(); return enumType == null ? null : Enum.ToObject(enumType, val_int) as Enum; } set { val_int = Convert.ToInt32(value); } }

        public virtual void setValueType(Type t) {
            valueType = GetValueType(t);
            if(valueType == ValueType.Object || valueType == ValueType.Enum || valueType == ValueType.Component)
                val_string = t.ToString();
        }

        /// <summary>
        /// target is used for specialized types, e.g. GameObject and Component (to grab ref. via path for meta)
        /// </summary>
        public virtual object toObject(ITarget target) {
            if(valueType == ValueType.GameObject) {
                if(val_obj)
                    return val_obj;

                if(string.IsNullOrEmpty(val_string))
                    return null;

                Transform t = target.GetCache(val_string);
                if(!t) {
                    t = Utility.GetTarget(target.root, val_string);
                    target.SetCache(val_string, t);
                }

                return t ? t.gameObject : null;
            }
            if(valueType == ValueType.Component) {
                if(val_obj)
                    return val_obj;

                if(string.IsNullOrEmpty(val_string))
                    return null;

                var pathParms = val_string.Split(':');

                if(pathParms.Length > 1) {
                    Transform t = target.GetCache(pathParms[1]);
                    if(!t) {
                        t = Utility.GetTarget(target.root, pathParms[1]);
                        target.SetCache(pathParms[1], t);
                    }

                    var compType = GetTypeFrom(pathParms[0]);

                    return t ? t.GetComponent(compType) : null;
                }

                return null;
            }
            if(valueType == ValueType.Boolean) return (val_bool/* as object*/);
            if(valueType == ValueType.String) return (val_string/* as object*/);
            if(valueType == ValueType.Char) {
                if(val_string == null || val_string.Length <= 0) return '\0';
                return (val_string[0]/* as object*/);
            }
            if(valueType == ValueType.Integer) return (val_int/* as object*/);
            if(valueType == ValueType.Long) return (val_long/* as object*/);
            if(valueType == ValueType.Float) return (val_float/* as object*/);
            if(valueType == ValueType.Double) return (val_double/* as object*/);
            if(valueType == ValueType.Vector2) return (val_vect2/* as object*/);
            if(valueType == ValueType.Vector3) return (val_vect3/* as object*/);
            if(valueType == ValueType.Vector4) return (val_vect4/* as object*/);
            if(valueType == ValueType.Color) return (val_color/* as object*/);
            if(valueType == ValueType.Rect) return (val_rect/* as object*/);
            if(valueType == ValueType.Object) return (val_obj ? val_obj : null/* as object*/);
            if(valueType == ValueType.Enum) return val_enum;            
            if(valueType == ValueType.Array) return null;

            Debug.LogError("Animator: Type not found for Event Parameter.");
            return null;
        }

        static Type GetTypeFrom(string TypeName) {

            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, etc.
            var type = Type.GetType(TypeName);

            // If it worked, then we're done here
            if(type != null)
                return type;

            // Get the name of the assembly (Assumption is that we are using
            // fully-qualified type names)
            int endInd = TypeName.IndexOf('.');
            if(endInd == -1) {
                try {
                    //no assembly?
                    return Type.GetType(TypeName);
                }
                catch(TypeInitializationException e) {
                    Debug.LogWarning(e.ToString());
                    return null;
                }
            }

            var assemblyName = TypeName.Substring(0, endInd);

            try {
                // Attempt to load the indicated Assembly
                var assembly = System.Reflection.Assembly.Load(assemblyName);

                // Ask that assembly to return the proper Type
                return assembly.GetType(TypeName);
            }
            catch { // Most likely not found
                return null;
            }
        }

        public virtual Type getParamType() {
            Type ret;

            if(valueType == ValueType.Boolean) ret = typeof(bool);
            else if(valueType == ValueType.Integer) ret = typeof(int);
            else if(valueType == ValueType.Long) ret = typeof(long);
            else if(valueType == ValueType.Float) ret = typeof(float);
            else if(valueType == ValueType.Double) ret = typeof(double);
            else if(valueType == ValueType.Vector2) ret = typeof(Vector2);
            else if(valueType == ValueType.Vector3) ret = typeof(Vector3);
            else if(valueType == ValueType.Vector4) ret = typeof(Vector4);
            else if(valueType == ValueType.Color) ret = typeof(Color);
            else if(valueType == ValueType.Rect) ret = typeof(Rect);
            else if(valueType == ValueType.String) ret = typeof(string);
            else if(valueType == ValueType.Char) ret = typeof(char);
            else if(valueType == ValueType.GameObject) ret = typeof(GameObject);
            else if(valueType == ValueType.Component) {
                if(!string.IsNullOrEmpty(val_string)) {
                    var pathParms = val_string.Split(':');
                    if(pathParms.Length > 0)
                        ret = GetTypeFrom(pathParms[0]);
                    else
                        ret = null;
                }
                else
                    ret = null;
            }
            else if(valueType == ValueType.Object || valueType == ValueType.Enum) {
                ret = GetTypeFrom(val_string);
                if(ret == null) {
                    try {
                        object obj = System.Activator.CreateInstance("UnityEngine.dll", val_string).Unwrap();
                        ret = obj != null ? obj.GetType() : null;
                    }
                    catch {
                        ret = null;
                        //TODO: find a better way
                        //Debug.LogError(e.ToString());
                    }
                }
            }
            else {
                ret = null;
                Debug.LogError("Animator: Type not found for Event Parameter.");
            }
            return ret;
        }

        public virtual void fromObject(ITarget target, object dat) {
            switch(valueType) {
                case ValueType.Integer:
                    val_int = Convert.ToInt32(dat);
                    break;
                case ValueType.Long:
                    val_long = Convert.ToInt64(dat);
                    break;
                case ValueType.Float:
                    val_float = Convert.ToSingle(dat);
                    break;
                case ValueType.Double:
                    val_double = Convert.ToDouble(dat);
                    break;
                case ValueType.Vector2:
                    val_vect2 = (Vector2)dat;
                    break;
                case ValueType.Vector3:
                    val_vect3 = (Vector3)dat;
                    break;
                case ValueType.Vector4:
                    val_vect4 = (Vector4)dat;
                    break;
                case ValueType.Color:
                    val_color = (Color)dat;
                    break;
                case ValueType.Rect:
                    val_rect = (Rect)dat;
                    break;
                case ValueType.String:
                    val_string = Convert.ToString(dat);
                    break;
                case ValueType.Char:
                    val_string = new string(Convert.ToChar(dat), 1);
                    break;
                case ValueType.Object:
                    val_obj = (UnityEngine.Object)dat;
                    val_string = val_obj ? val_obj.GetType().Name : "";
                    break;
                case ValueType.Boolean:
                    val_bool = Convert.ToBoolean(dat);
                    break;
                case ValueType.Enum:
                    val_enum = dat as Enum;
                    break;
                case ValueType.GameObject:

                    break;
            }
        }

        /// <summary>
        /// Only use this for meta, and if go is on the scene
        /// </summary>
        public void SetAsGameObject(ITarget target, GameObject go, bool isAsset) {
            valueType = ValueType.GameObject;

            if(isAsset || go == null) {
                val_string = "";
                val_obj = go;
            }
            else {
                val_string = Utility.GetPath(target.root, go);
                val_obj = target.meta ? null : go;
            }
        }

        /// <summary>
        /// Only use this for meta, and if go is on the scene
        /// </summary>
        public void SetAsComponent(ITarget target, Component comp, bool isAsset) {
            valueType = ValueType.Component;

            if(comp == null) {
                val_string = "";
                val_obj = null;
            }
            else if(isAsset || comp == null) {
                val_string = comp ? comp.GetType().ToString() : "";
                val_obj = comp;
            }
            else {
                string typeName = comp.GetType().ToString();
                string path = Utility.GetPath(target.root, comp.gameObject);

                //separate with ':'
                var sb = new System.Text.StringBuilder();
                sb.Append(typeName).Append(':').Append(path);

                val_string = sb.ToString();

                val_obj = target.meta ? null : comp;
            }
        }

        public virtual void CopyTo(EventData other) {
            other.paramName = paramName;
            other.valueType = valueType;
            other.val_int = val_int;
            other.val_string = val_string;
            other.val_vect4 = val_vect4;
            other.val_obj = val_obj;
        }

        public virtual AnimateTimeline.JSONEventParameter toJSON() {
            AnimateTimeline.JSONEventParameter e = new AnimateTimeline.JSONEventParameter();
            e.valueType = valueType;
            if(valueType == ValueType.Boolean) e.val_bool = val_bool;
            if(valueType == ValueType.String) e.val_string = (val_string/* as object*/);
            if(valueType == ValueType.Char) {
                if(val_string == null || val_string.Length <= 0) e.val_string = "\0";
                e.val_string = "" + val_string[0];
            }
            if(valueType == ValueType.Integer || valueType == ValueType.Long) e.val_int = (val_int/* as object*/);
            if(valueType == ValueType.Float || valueType == ValueType.Double) e.val_float = (val_float/* as object*/);
            if(valueType == ValueType.Vector2) {
                AnimateTimeline.JSONVector2 v2 = new AnimateTimeline.JSONVector2();
                v2.setValue(val_vect2);
                e.val_vect2 = v2;
            }
            if(valueType == ValueType.Vector3) {
                AnimateTimeline.JSONVector3 v3 = new AnimateTimeline.JSONVector3();
                v3.setValue(val_vect3);
                e.val_vect3 = v3;
            }
            if(valueType == ValueType.Vector4) {
                AnimateTimeline.JSONVector4 v4 = new AnimateTimeline.JSONVector4();
                v4.setValue(val_vect4);
                e.val_vect4 = v4;
            }
            if(valueType == ValueType.Color) {
                AnimateTimeline.JSONColor c = new AnimateTimeline.JSONColor();
                c.setValue(val_color);
                e.val_color = c;
            }
            if(valueType == ValueType.Rect) {
                AnimateTimeline.JSONRect r = new AnimateTimeline.JSONRect();
                r.setValue(val_rect);
                e.val_rect = r;
            }
            if(valueType == ValueType.Object) {
                if(val_obj.GetType() != typeof(GameObject)) {
                    // component
                    e.val_obj_extra = val_obj.name;
                    e.val_obj = val_obj.GetType().Name;
                }
                else {
                    // gameobject
                    e.val_obj_extra = null;
                    e.val_obj = val_obj.name;
                }

            }
            //Debug.LogError("Animator: Type not found for Event Parameter.");
            return e;
        }
    }

    [System.Serializable]
    public class EventParameter : EventData {

        public List<EventData> lsArray = new List<EventData>();

        private System.Type mCachedType = null;

        public EventParameter() {

        }

        public bool checkArrayIntegrity() {
            if(valueType == ValueType.Array) {
                if(lsArray != null && lsArray.Count > 0) {
                    ValueType valElem = lsArray[0].valueType;
                    foreach(EventData elem in lsArray) {
                        if(elem.valueType != valElem)
                            return false;
                    }
                }
            }

            return true;
        }

        public override void setValueType(Type t) {
            base.setValueType(t);
            mCachedType = null;
        }

        public override object toObject(ITarget target) {
            if(valueType == ValueType.Array) {
                System.Array array = lsArray.Count > 0 ? System.Array.CreateInstance(lsArray[0].getParamType(), lsArray.Count) : System.Array.CreateInstance(typeof(object), 0);
                for(int i = 0; i < lsArray.Count; i++) {
                    array.SetValue(lsArray[i].toObject(target), i);
                }
                return array;
            }

            return base.toObject(target);
        }

        public override Type getParamType() {
            if(mCachedType != null)
                return mCachedType;

            if(valueType == ValueType.Array) {
                if(lsArray.Count <= 0) mCachedType = typeof(object[]);
                else {
                    switch(lsArray[0].valueType) {
                        case ValueType.Integer:
                            mCachedType = typeof(int[]); break;
                        case ValueType.Long:
                            mCachedType = typeof(long[]); break;
                        case ValueType.Float:
                            mCachedType = typeof(float[]); break;
                        case ValueType.Double:
                            mCachedType = typeof(double[]); break;
                        case ValueType.Vector2:
                            mCachedType = typeof(Vector2[]); break;
                        case ValueType.Vector3:
                            mCachedType = typeof(Vector3[]); break;
                        case ValueType.Vector4:
                            mCachedType = typeof(Vector4[]); break;
                        case ValueType.Color:
                            mCachedType = typeof(Color[]); break;
                        case ValueType.Rect:
                            mCachedType = typeof(Rect[]); break;
                        case ValueType.String:
                            mCachedType = typeof(string[]); break;
                        case ValueType.Char:
                            mCachedType = typeof(char[]); break;
                        case ValueType.Object:
                            mCachedType = typeof(UnityEngine.Object[]); break;
                        case ValueType.Array:
                            mCachedType = null; break;
                        case ValueType.Boolean:
                            mCachedType = typeof(bool[]); break;
                        default:
                            mCachedType = null; break;
                    }
                }
                //else return lsArray[0].getParamType();
            }
            else
                mCachedType = base.getParamType();

            return mCachedType;
        }

        public override void fromObject(ITarget target, object dat) {
            if(valueType == ValueType.Array) {
                System.Array arr = (System.Array)dat;
                lsArray = new List<EventData>(arr.Length);
                System.Type t = arr.GetType().GetElementType();
                for(int i = 0; i < arr.Length; i++) {
                    object arrElem = arr.GetValue(i);

                    EventData a = new EventData();
                    a.setValueType(arrElem != null ? arrElem.GetType() : t);
                    a.fromObject(target, arrElem);
                    lsArray.Add(a);
                }
            }
            else
                base.fromObject(target, dat);
        }

        public string getStringValue() {
            if(valueType == ValueType.Boolean) return val_bool.ToString().ToLower();
            if(valueType == ValueType.String) return "\"" + val_string + "\"";
            if(valueType == ValueType.Char) {
                if(val_string == null || val_string.Length <= 0) return "''";
                else return "'" + val_string[0] + "'";
            }
            if(valueType == ValueType.Integer || valueType == ValueType.Long) return val_int.ToString();
            if(valueType == ValueType.Float || valueType == ValueType.Double) return val_float.ToString();
            if(valueType == ValueType.Vector2) return val_vect2.ToString();
            if(valueType == ValueType.Vector3) return val_vect3.ToString();
            if(valueType == ValueType.Vector4) return val_vect4.ToString();
            if(valueType == ValueType.Color) return val_color.ToString();
            if(valueType == ValueType.Rect) return val_rect.ToString();
            if(valueType == ValueType.GameObject) {
                if(val_obj)
                    return val_obj.name;

                return !string.IsNullOrEmpty(val_string) ? val_string : "None";
            }
            if(valueType == ValueType.Component) {
                if(val_obj)
                    return val_obj.name;

                if(!string.IsNullOrEmpty(val_string)) {
                    var parms = val_string.Split(':');
                    if(parms.Length == 1)
                        return parms[0]; //show Component type
                    else if(parms.Length > 1)
                        return parms[1]; //show path
                }

                return "None";
            }
            if(valueType == ValueType.Object) {
                if(!val_obj) return "None";
                else return val_obj.name;
            }

            if(valueType == ValueType.Array) return "Array";
            if(valueType == ValueType.Enum) return val_enum != null ? val_enum.ToString() : "";
            Debug.LogError("Animator: Type not found for Event Parameter.");
            return "object";
        }

        public override void CopyTo(EventData other) {
            if(valueType == ValueType.Array) {
                var otherParam = other as EventParameter;
                if(otherParam != null) {
                    otherParam.valueType = valueType;
                    otherParam.mCachedType = mCachedType;

                    otherParam.lsArray = new List<EventData>(lsArray.Count);
                    for(int i = 0; i < lsArray.Count; i++) {
                        if(lsArray[i] == null)
                            continue;

                        otherParam.lsArray[i] = (EventData)System.Activator.CreateInstance(lsArray[i].GetType());
                        lsArray[i].CopyTo(otherParam.lsArray[i]);
                    }
                }
            }
            else {
                base.CopyTo(other);
            }
        }

        public override AnimateTimeline.JSONEventParameter toJSON() {
            if(valueType == ValueType.Array && lsArray.Count > 0) {
                AnimateTimeline.JSONEventParameter e = new AnimateTimeline.JSONEventParameter();
                e.valueType = valueType;
                AnimateTimeline.JSONEventParameter[] arr = new AnimateTimeline.JSONEventParameter[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) {
                    //arrObj[i] = lsArray[i].val_bool;
                    arr[i] = lsArray[i].toJSON();
                }
                e.array = arr;
                /*
                if(lsArray[0].valueType == (int) ValueType.Boolean) {
                    //bool[] arrObj = new bool[lsArray.Count];
    //				AnimateTimeline.JSONEventParameter[] arr = new AnimateTimeline.JSONEventParameter[lsArray.Count];
    //				for(int i=0;i<lsArray.Count;i++){
    //					//arrObj[i] = lsArray[i].val_bool;
    //					arr[i] = lsArray[i].toJSON();
    //				}
                    //return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.String) {
                    string[] arrObj = new string[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_string;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Char) {
                    char[] arrObj = new char[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_string[0];
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Integer || lsArray[0].valueType == (int) ValueType.Long) {
                    int[] arrObj = new int[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_int;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Float || lsArray[0].valueType == (int) ValueType.Double) {
                    float[] arrObj = new float[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_float;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Vector2) {
                    Vector2[] arrObj = new Vector2[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_vect2;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Vector3) {
                    Vector3[] arrObj = new Vector3[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_vect3;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Vector4) {
                    Vector4[] arrObj = new Vector4[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_vect4;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Color) {
                    Color[] arrObj = new Color[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_color;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Rect) {
                    Rect[] arrObj = new Rect[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_rect;
                    }
                    return arrObj;
                }
                if(lsArray[0].valueType == (int) ValueType.Object) {
                    UnityEngine.Object[] arrObj = new UnityEngine.Object[lsArray.Count];
                    for(int i=0;i<lsArray.Count;i++){
                        arrObj[i] = lsArray[i].val_obj;
                    }
                    return arrObj;
                }*/
                return e;
            }

            return base.toJSON();
        }

        public object[] toArray(ITarget target) {
            object[] arr = new object[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++)
                arr[i] = lsArray[i].toObject(target);

            return arr;
        }
        public bool isArray() {
            if(lsArray.Count > 0) return true;
            return false;
        }

        public EventParameter CreateClone() {
            EventParameter a = new EventParameter();
            a.valueType = valueType;
            a.val_int = val_int;
            a.val_vect4 = val_vect4;
            a.val_string = val_string;
            a.val_obj = val_obj;
            foreach(EventParameter e in lsArray) {
                a.lsArray.Add(e.CreateClone());
            }
            return a;
        }

        public List<GameObject> getDependencies() {
            List<GameObject> ls = new List<GameObject>();
            if(valueType == ValueType.Object && val_obj) {
                if(val_obj is GameObject) ls.Add((GameObject)val_obj);
                else {
                    ls.Add((val_obj as Component).gameObject);
                }
            }
            else if(valueType == ValueType.Array) {
                if(lsArray.Count > 0 && lsArray[0].valueType == ValueType.Object) {
                    foreach(EventParameter param in lsArray) {
                        if(param.val_obj) {
                            if(param.val_obj is GameObject)
                                ls.Add((GameObject)param.val_obj);
                            else {
                                ls.Add((param.val_obj as Component).gameObject);
                            }
                        }
                    }
                }
            }
            return ls;
        }

        public bool updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
            bool didUpdateParameter = false;
            if(valueType == ValueType.Object && val_obj) {
                for(int i = 0; i < oldReferences.Count; i++) {
                    if(val_obj is GameObject) {
                        if(oldReferences[i] == val_obj) {
                            val_obj = newReferences[i];
                            didUpdateParameter = true;
                            break;
                        }
                    }
                    else {
                        if(oldReferences[i] == (val_obj as Component).gameObject) {
                            val_obj = newReferences[i].GetComponent(val_obj.GetType());
                            didUpdateParameter = true;
                            break;
                        }
                    }
                }
            }
            else if(valueType == ValueType.Array) {
                if(lsArray.Count > 0 && lsArray[0].valueType == ValueType.Object) {
                    for(int i = 0; i < oldReferences.Count; i++) {
                        foreach(EventParameter param in lsArray) {
                            if(!param.val_obj) continue;
                            if(param.val_obj is GameObject) {
                                if(oldReferences[i] == param.val_obj) {
                                    param.val_obj = newReferences[i];
                                    didUpdateParameter = true;
                                    break;
                                }
                            }
                            else {
                                if(oldReferences[i] == (param.val_obj as Component).gameObject) {
                                    param.val_obj = newReferences[i].GetComponent(param.val_obj.GetType());
                                    didUpdateParameter = true;
                                    break;
                                }
                            }
                        }

                    }

                }
            }
            return didUpdateParameter;
        }
    }
}