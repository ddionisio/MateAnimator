using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class EventKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Event; } }

        public bool useSendMessage = false;
        public List<EventParameter> parameters = new List<EventParameter>();
        public string methodName;
        public string componentType; //used for GameObjects

        private MethodInfo cachedMethodInfo;
        private Component cachedComponent;

        public bool isMatch(ParameterInfo[] cachedParameterInfos) {
            if(cachedParameterInfos != null && parameters != null && cachedParameterInfos.Length == parameters.Count) {
                for(int i = 0; i < cachedParameterInfos.Length; i++) {
                    if(parameters[i].valueType == EventData.ValueType.Array) {
                        if(!parameters[i].checkArrayIntegrity() || cachedParameterInfos[i].ParameterType != parameters[i].getParamType())
                            return false;
                    }
                    else if(!Utility.IsDerivedFrom(parameters[i].getParamType(), cachedParameterInfos[i].ParameterType))
                        return false;
                }
            }
            else
                return false;

            return true;
        }

        public override void maintainKey(ITarget itarget, UnityEngine.Object targetObj) {            
            cachedMethodInfo = null;
            cachedComponent = null;

            //clean up parameters for meta
            if(itarget.meta) {
                for(int i = 0; i < parameters.Count; i++) {
                    switch(parameters[i].valueType) {
                        case EventData.ValueType.Component:
                        case EventData.ValueType.GameObject:
                            if(!string.IsNullOrEmpty(parameters[i].val_string)) //check "path" if it's not empty, clean up val_obj
                                parameters[i].val_obj = null;
                            break;
                    }
                }
            }
        }

        public override int getNumberOfFrames(int frameRate) {
            return 1;
        }
        public void SetComponentType(string aComponentType) {
            componentType = aComponentType;
            cachedComponent = null;
            methodName = "";
            cachedMethodInfo = null;
            parameters = new List<EventParameter>();
        }
        public Component getComponentFromTarget(Object target) {
            if(!cachedComponent && !string.IsNullOrEmpty(componentType) && target is GameObject)
                cachedComponent = ((GameObject)target).GetComponent(componentType);

            return cachedComponent;
        }
        //set target to a valid ref. for meta
        public MethodInfo getMethodInfo(Object target) {
            //use component of target if componentType is not empty
            if(cachedComponent)
                target = cachedComponent;
            else if(!string.IsNullOrEmpty(componentType) && target is GameObject)
                target = cachedComponent = ((GameObject)target).GetComponent(componentType);

            if(target && !string.IsNullOrEmpty(methodName)) {
                var paramTypes = GetParamTypes();
                cachedMethodInfo = paramTypes != null ? target.GetType().GetMethod(methodName, paramTypes) : null;
                return cachedMethodInfo;
            }
            else {
                cachedMethodInfo = null;
                return null;
            }
        }

        //set target to a valid ref. for meta
        public bool setMethodInfo(Object methodObj, MethodInfo methodInfo, ParameterInfo[] cachedParameterInfos, bool restoreValues, System.Action<EventKey> onPreChange) {
            MethodInfo _methodInfo = getMethodInfo(methodObj);

            // if different component or methodinfo
            if(_methodInfo != methodInfo || !isMatch(cachedParameterInfos)) {
                if(onPreChange != null)
                    onPreChange(this);

                methodName = methodInfo.Name;
                cachedMethodInfo = methodInfo;
                //this.parameters = new object[numParameters];

                Dictionary<string, EventParameter> oldParams = null;
                if(restoreValues && parameters != null && parameters.Count > 0) {
                    Debug.Log("Parameters have been changed. Attempting to restore data.");
                    oldParams = new Dictionary<string, EventParameter>(parameters.Count);
                    for(int i = 0; i < parameters.Count; i++) {
                        if(parameters[i] == null)
                            continue;

                        if(!string.IsNullOrEmpty(parameters[i].paramName) && (parameters[i].valueType != EventData.ValueType.Array || parameters[i].checkArrayIntegrity()))
                            oldParams.Add(parameters[i].paramName, parameters[i].CreateClone());
                    }
                }

                this.parameters = new List<EventParameter>();

                // add parameters
                for(int i = 0; i < cachedParameterInfos.Length; i++) {
                    EventParameter oldParam;
                    if(oldParams != null && oldParams.TryGetValue(cachedParameterInfos[i].Name, out oldParam)) {
                        this.parameters.Add(oldParam);
                    }
                    else {
                        EventParameter a = new EventParameter();
                        a.paramName = cachedParameterInfos[i].Name;
                        a.setValueType(cachedParameterInfos[i].ParameterType);
                        this.parameters.Add(a);
                    }
                }

                return true;
            }
            return false;
        }

        public bool setUseSendMessage(bool useSendMessage, System.Action<EventKey> onPreChange) {
            if(this.useSendMessage != useSendMessage) {
                if(onPreChange != null)
                    onPreChange(this);

                this.useSendMessage = useSendMessage;
                return true;
            }
            return false;
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as EventKey;
            
            a.useSendMessage = useSendMessage;
            // parameters
            a.methodName = methodName;
            a.componentType = componentType;
            a.cachedMethodInfo = cachedMethodInfo;
            foreach(EventParameter e in parameters) {
                a.parameters.Add(e.CreateClone());
            }
        }

        public List<GameObject> getDependencies() {
            List<GameObject> ls = new List<GameObject>();
            foreach(EventParameter param in parameters) {
                ls = ls.Union(param.getDependencies()).ToList();
            }
            return ls;
        }

        public bool updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences, bool didUpdateObj, GameObject obj) {            
            bool didUpdateParameter = false;
            foreach(EventParameter param in parameters) {
                if(param.updateDependencies(newReferences, oldReferences) && !didUpdateParameter) didUpdateParameter = true;
            }
            return didUpdateParameter;
        }

        System.Type[] GetParamTypes() {
            List<System.Type> ret = new List<System.Type>(parameters.Count);
            foreach(EventParameter param in parameters) {
                var type = param.getParamType();
                //invalid param?
                if(type == null)
                    return null;

                ret.Add(type);
            }
            return ret.ToArray();
        }

        #region action
        public void setObjectInArray(ITarget target, ref object obj, List<EventData> lsArray) {
            if(lsArray.Count <= 0) return;
            EventData.ValueType valueType = lsArray[0].valueType;
            if(valueType == EventData.ValueType.String) {
                string[] arrString = new string[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrString[i] = (string)lsArray[i].toObject(target);
                obj = arrString;
                return;
            }
            if(valueType == EventData.ValueType.Char) {
                char[] arrChar = new char[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrChar[i] = (char)lsArray[i].toObject(target);
                obj = arrChar;
                return;
            }
            if(valueType == EventData.ValueType.Integer || valueType == EventData.ValueType.Long) {
                int[] arrInt = new int[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrInt[i] = (int)lsArray[i].toObject(target);
                obj = arrInt;
                return;
            }
            if(valueType == EventData.ValueType.Float || valueType == EventData.ValueType.Double) {
                float[] arrFloat = new float[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrFloat[i] = (float)lsArray[i].toObject(target);
                obj = arrFloat;
                return;
            }
            if(valueType == EventData.ValueType.Vector2) {
                Vector2[] arrVect2 = new Vector2[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrVect2[i] = new Vector2(lsArray[i].val_vect2.x, lsArray[i].val_vect2.y);
                obj = arrVect2;
                return;
            }
            if(valueType == EventData.ValueType.Vector3) {
                Vector3[] arrVect3 = new Vector3[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrVect3[i] = new Vector3(lsArray[i].val_vect3.x, lsArray[i].val_vect3.y, lsArray[i].val_vect3.z);
                obj = arrVect3;
                return;
            }
            if(valueType == EventData.ValueType.Vector4) {
                Vector4[] arrVect4 = new Vector4[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrVect4[i] = new Vector4(lsArray[i].val_vect4.x, lsArray[i].val_vect4.y, lsArray[i].val_vect4.z, lsArray[i].val_vect4.w);
                obj = arrVect4;
                return;
            }
            if(valueType == EventData.ValueType.Color) {
                Color[] arrColor = new Color[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrColor[i] = new Color(lsArray[i].val_color.r, lsArray[i].val_color.g, lsArray[i].val_color.b, lsArray[i].val_color.a);
                obj = arrColor;
                return;
            }
            if(valueType == EventData.ValueType.Rect) {
                Rect[] arrRect = new Rect[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrRect[i] = new Rect(lsArray[i].val_rect.x, lsArray[i].val_rect.y, lsArray[i].val_rect.width, lsArray[i].val_rect.height);
                obj = arrRect;
                return;
            }
            if(valueType == EventData.ValueType.Object || valueType == EventData.ValueType.GameObject || valueType == EventData.ValueType.Component) {
                UnityEngine.Object[] arrObject = new UnityEngine.Object[lsArray.Count];
                for(int i = 0; i < lsArray.Count; i++) arrObject[i] = (UnityEngine.Object)lsArray[i].toObject(target);
                obj = arrObject;
                return;
            }
            if(valueType == EventData.ValueType.Array) {
                //TODO: array of array not supported...
            }
            obj = null;
        }

        object[] buildParams(ITarget target) {
            if(parameters == null)
                return new object[0];

            object[] arrParams = new object[parameters.Count];
            for(int i = 0; i < parameters.Count; i++) {
                if(parameters[i].isArray()) {
                    setObjectInArray(target, ref arrParams[i], parameters[i].lsArray);
                }
                else {
                    arrParams[i] = parameters[i].toObject(target);
                }
            }
            return arrParams;
        }
                
        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            if(methodName == null) return;

            float duration = 1.0f / seq.take.frameRate;

            var tgt = target;

            //set target to component
            if(cachedComponent)
                tgt = cachedComponent;
            else if(!string.IsNullOrEmpty(componentType) && tgt is GameObject)
                tgt = cachedComponent = ((GameObject)tgt).GetComponent(componentType);

            if(!tgt) {
                if(!string.IsNullOrEmpty(componentType)) {
                    if(target)
                        Debug.LogWarning(string.Format("Track {0} Key {1}: Component ({2}) missing in {3}.", track.name, index, componentType, target.name));
                    else
                        Debug.LogWarning(string.Format("Track {0} Key {1}: Component ({2}) is missing.", track.name, index, componentType));
                }
                else
                    Debug.LogWarning(string.Format("Track {0} Key {1}: Target is missing.", track.name, index));

                return;
            }

            //can't send message if it's not a component
            Component compSendMsg = null;
            if(useSendMessage) {
                compSendMsg = tgt as Component;
            }

            if(compSendMsg) {
                if(parameters == null || parameters.Count <= 0)
                    seq.InsertCallback(this, () => compSendMsg.SendMessage(methodName, null, SendMessageOptions.DontRequireReceiver));
                else
                    seq.InsertCallback(this, () => compSendMsg.SendMessage(methodName, parameters[0].toObject(seq.target), SendMessageOptions.DontRequireReceiver));
            }
            else {
                var method = cachedMethodInfo != null ? cachedMethodInfo : tgt.GetType().GetMethod(methodName, GetParamTypes());

                object[] parms = buildParams(seq.target);

                seq.InsertCallback(this, () => method.Invoke(tgt, parms));
            }
        }

        #endregion
    }
}
