using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMEventKey : AMKey {

    private struct ParamKeep {
        public System.Type type;
        public object val;
    }

	[SerializeField]
    Component component = null;
	[SerializeField]
	string componentName = "";

    public bool useSendMessage = true;
    public List<AMEventParameter> parameters = new List<AMEventParameter>();
    public string methodName;
    private MethodInfo cachedMethodInfo;
    
	public string getComponentName() { return componentName; }

    public bool isMatch(ParameterInfo[] cachedParameterInfos) {
        if(cachedParameterInfos != null && parameters != null && cachedParameterInfos.Length == parameters.Count) {
            for(int i = 0; i < cachedParameterInfos.Length; i++) {
                if(parameters[i].valueType == (int)AMEventParameter.ValueType.Array) {
                    if(!parameters[i].checkArrayIntegrity() || cachedParameterInfos[i].ParameterType != parameters[i].getParamType())
                        return false;
                }
                else if(cachedParameterInfos[i].ParameterType != parameters[i].getParamType())
                    return false;
            }
        }
        else
            return false;

        return true;
    }

	public override string GetRequiredComponent() {
		return componentName;
	}

	public override void maintainKey(AMITarget itarget, UnityEngine.Object targetObj) {
		if(string.IsNullOrEmpty(componentName)) {
			if(component) {
				componentName = component.GetType().Name;
			}
		}

		if(itarget.TargetIsMeta()) {
			component = null;
		}
		else if(!component && !string.IsNullOrEmpty(componentName) && targetObj) {
			component = ((GameObject)targetObj).GetComponent(componentName);
		}

		cachedMethodInfo = null;
	}

	//set target to a valid ref. for meta
	public MethodInfo getMethodInfo(GameObject target) {
		if(target) {
			if(string.IsNullOrEmpty(componentName)) return null;
			if(cachedMethodInfo != null) return cachedMethodInfo;
			if(methodName == null) return null;
			Component comp = target.GetComponent(componentName);
			cachedMethodInfo = comp.GetType().GetMethod(methodName, GetParamTypes());
			return cachedMethodInfo;
		}
		else {
			if(component == null) return null;
			if(cachedMethodInfo != null) return cachedMethodInfo;
			if(methodName == null) return null;
			cachedMethodInfo = component.GetType().GetMethod(methodName, GetParamTypes());
			return cachedMethodInfo;
		}
	}

	//set target to a valid ref. for meta
	public bool setMethodInfo(GameObject target, Component component, bool setComponent, MethodInfo methodInfo, ParameterInfo[] cachedParameterInfos, bool restoreValues) {
		MethodInfo _methodInfo = getMethodInfo(target);

        // if different component or methodinfo
        string _componentName = component.GetType().Name;
        if((_methodInfo != methodInfo) || (this.componentName != _componentName) || !isMatch(cachedParameterInfos)) {
            this.component = setComponent ? component : null;
            this.componentName = _componentName;
			methodName = methodInfo.Name;
			cachedMethodInfo = methodInfo;
            //this.parameters = new object[numParameters];

            Dictionary<string, ParamKeep> oldParams = null;
            if(restoreValues && parameters != null && parameters.Count > 0) {
                Debug.Log("Parameters have been changed, from code? Attempting to restore data.");
                oldParams = new Dictionary<string, ParamKeep>(parameters.Count);
                for(int i = 0; i < parameters.Count; i++) {
                    if(!string.IsNullOrEmpty(parameters[i].paramName) && (parameters[i].valueType != (int)AMEventParameter.ValueType.Array || parameters[i].checkArrayIntegrity())) {
                        try {
                            object valObj = parameters[i].toObject();

                            oldParams.Add(parameters[i].paramName, new ParamKeep() { type = parameters[i].getParamType(), val = valObj });
                        }
                        catch {
                            continue;
                        }
                    }
                }
            }
            
            this.parameters = new List<AMEventParameter>();

            // add parameters
            for(int i = 0; i < cachedParameterInfos.Length; i++) {
                AMEventParameter a = new AMEventParameter();
                a.paramName = cachedParameterInfos[i].Name;
                a.setValueType(cachedParameterInfos[i].ParameterType);

                //see if we can restore value from previous
                if(oldParams != null && oldParams.ContainsKey(a.paramName)) {
                    ParamKeep keep = oldParams[a.paramName];
                    if(keep.type == cachedParameterInfos[i].ParameterType)
                        a.fromObject(keep.val);
                }

                this.parameters.Add(a);
            }

            return true;
        }
        return false;
    }

    public bool setUseSendMessage(bool useSendMessage) {
        if(this.useSendMessage != useSendMessage) {
            this.useSendMessage = useSendMessage;
            return true;
        }
        return false;
    }

    /*public bool setParameters(object[] parameters) {
        if(this.parameters != parameters) {
            this.parameters = parameters;
            return true;
        }
        return false;
    }*/
    
    // copy properties from key
    public override void CopyTo(AMKey key) {

		AMEventKey a = key as AMEventKey;
        a.enabled = false;
        a.frame = frame;
        a.component = component;
		a.componentName = componentName;
        a.useSendMessage = useSendMessage;
        // parameters
        a.methodName = methodName;
        a.cachedMethodInfo = cachedMethodInfo;
        foreach(AMEventParameter e in parameters) {
            a.parameters.Add(e.CreateClone());
        }
    }

    public List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();
        foreach(AMEventParameter param in parameters) {
            ls = ls.Union(param.getDependencies()).ToList();
        }
        return ls;
    }

    public bool updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences, bool didUpdateObj, GameObject obj) {
        if(didUpdateObj && component) {
            component = obj.GetComponent(componentName);
            if(!component) Debug.LogError("Animator: Component '" + componentName + "' not found on new reference for GameObject '" + obj.name + "'. Some event track data may be lost.");
            cachedMethodInfo = null;
        }
        bool didUpdateParameter = false;
        foreach(AMEventParameter param in parameters) {
            if(param.updateDependencies(newReferences, oldReferences) && !didUpdateParameter) didUpdateParameter = true;
        }
        return didUpdateParameter;
    }

    #region action
    public void setObjectInArray(ref object obj, List<AMEventData> lsArray) {
        if(lsArray.Count <= 0) return;
        int valueType = lsArray[0].valueType;
        if(valueType == (int)AMEventParameter.ValueType.String) {
            string[] arrString = new string[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrString[i] = (string)lsArray[i].toObject();
            obj = arrString;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Char) {
            char[] arrChar = new char[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrChar[i] = (char)lsArray[i].toObject();
            obj = arrChar;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Integer || valueType == (int)AMEventParameter.ValueType.Long) {
            int[] arrInt = new int[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrInt[i] = (int)lsArray[i].toObject();
            obj = arrInt;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Float || valueType == (int)AMEventParameter.ValueType.Double) {
            float[] arrFloat = new float[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrFloat[i] = (float)lsArray[i].toObject();
            obj = arrFloat;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Vector2) {
            Vector2[] arrVect2 = new Vector2[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrVect2[i] = new Vector2(lsArray[i].val_vect2.x, lsArray[i].val_vect2.y);
            obj = arrVect2;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Vector3) {
            Vector3[] arrVect3 = new Vector3[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrVect3[i] = new Vector3(lsArray[i].val_vect3.x, lsArray[i].val_vect3.y, lsArray[i].val_vect3.z);
            obj = arrVect3;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Vector4) {
            Vector4[] arrVect4 = new Vector4[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrVect4[i] = new Vector4(lsArray[i].val_vect4.x, lsArray[i].val_vect4.y, lsArray[i].val_vect4.z, lsArray[i].val_vect4.w);
            obj = arrVect4;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Color) {
            Color[] arrColor = new Color[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrColor[i] = new Color(lsArray[i].val_color.r, lsArray[i].val_color.g, lsArray[i].val_color.b, lsArray[i].val_color.a);
            obj = arrColor;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Rect) {
            Rect[] arrRect = new Rect[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrRect[i] = new Rect(lsArray[i].val_rect.x, lsArray[i].val_rect.y, lsArray[i].val_rect.width, lsArray[i].val_rect.height);
            obj = arrRect;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Object) {
            UnityEngine.Object[] arrObject = new UnityEngine.Object[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) arrObject[i] = (UnityEngine.Object)lsArray[i].toObject();
            obj = arrObject;
            return;
        }
        if(valueType == (int)AMEventParameter.ValueType.Array) {
            //TODO: array of array not supported...
        }
        obj = null;
    }

    object[] buildParams() {
		if(parameters == null)
			return new object[0];

        object[] arrParams = new object[parameters.Count];
        for(int i = 0; i < parameters.Count; i++) {
            if(parameters[i].isArray()) {
                setObjectInArray(ref arrParams[i], parameters[i].lsArray);
            }
            else {
                arrParams[i] = parameters[i].toObject();
            }
        }
        return arrParams;
    }

	System.Type[] GetParamTypes() {
		List<System.Type> ret = new List<System.Type>(parameters.Count);
		foreach(AMEventParameter param in parameters) {
			ret.Add(param.getParamType());
		}
		return ret.ToArray();
	}

    public override void build(AMSequence seq, AMTrack track, UnityEngine.Object target) {
		if(methodName == null) return;

		//get component and fill the cached method info
		Component comp;
		if(seq.target.TargetIsMeta()) {
			if(string.IsNullOrEmpty(componentName)) return;
			comp = (target as GameObject).GetComponent(componentName);

		}
		else {
			if(component == null) return;
			comp = component;
		}
		if(cachedMethodInfo == null)
			cachedMethodInfo = comp.GetType().GetMethod(methodName, GetParamTypes());

        float waitTime = getWaitTime(seq.take.frameRate, 0.0f);

        if(useSendMessage) {
            if(parameters == null || parameters.Count <= 0)
                seq.sequence.InsertCallback(waitTime, comp.gameObject, methodName, null, SendMessageOptions.DontRequireReceiver);
            else
                seq.sequence.InsertCallback(waitTime, comp.gameObject, methodName, parameters[0].toObject(), SendMessageOptions.DontRequireReceiver);
        }
        else {
            seq.sequence.InsertCallback(waitTime, OnMethodCallbackParams, comp, (object)buildParams());
        }
    }

    void OnMethodCallbackParams(TweenEvent dat) {
		Component comp = dat.parms[0] as Component;
		object[] parms = dat.parms[1] as object[];

        if(comp == null) return;

		cachedMethodInfo.Invoke(comp, parms);
    }

    #endregion
}
