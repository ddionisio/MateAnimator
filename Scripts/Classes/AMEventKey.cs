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

    public bool frameLimit = true;
    public bool useSendMessage = true;
    public List<AMEventParameter> parameters = new List<AMEventParameter>();
    public string methodName;
    private MethodInfo cachedMethodInfo;
    
	public Component getComponent() { return component; }
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
	public bool setMethodInfo(GameObject target, Component component, MethodInfo methodInfo, ParameterInfo[] cachedParameterInfos, bool restoreValues) {
		MethodInfo _methodInfo = getMethodInfo(target);

        // if different component or methodinfo
		if((_methodInfo != methodInfo) || (this.component != component) || !isMatch(cachedParameterInfos)) {
            this.component = component;
			this.componentName = component.GetType().Name;
			methodName = methodInfo.Name;
			cachedMethodInfo = methodInfo;
            //this.parameters = new object[numParameters];

            Dictionary<string, ParamKeep> oldParams = null;
            if(restoreValues && parameters != null && parameters.Count > 0) {
                Debug.Log("Parameters have been changed, from code? Attempting to restore data.");
                oldParams = new Dictionary<string, ParamKeep>(parameters.Count);
                for(int i = 0; i < parameters.Count; i++) {
                    if(!string.IsNullOrEmpty(parameters[i].paramName) && (parameters[i].valueType != (int)AMEventParameter.ValueType.Array || parameters[i].checkArrayIntegrity())) {
                        oldParams.Add(parameters[i].paramName,
                            new ParamKeep() { type = parameters[i].getParamType(), val = parameters[i].toObject() });
                    }
                }
            }


            destroyParameters();
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

    public bool setFrameLimit(bool frameLimit) {
        if(this.frameLimit != frameLimit) {
            this.frameLimit = frameLimit;
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
    public void destroyParameters() {
        if(parameters == null) return;
        foreach(AMEventParameter param in parameters)
            param.destroy();
    }
    public override void destroy() {
        destroyParameters();
        base.destroy();
    }
    // copy properties from key
	public override AMKey CreateClone(GameObject go) {

		AMEventKey a = go ? go.AddComponent<AMEventKey>() : gameObject.AddComponent<AMEventKey>();
        a.enabled = false;
        a.frame = frame;
        a.component = component;
		a.componentName = componentName;
        a.useSendMessage = useSendMessage;
        a.frameLimit = frameLimit;
        // parameters
        a.methodName = methodName;
        a.cachedMethodInfo = cachedMethodInfo;
        foreach(AMEventParameter e in parameters) {
            a.parameters.Add(e.CreateClone());
        }
        return a;
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
    public void setObjectInArray(ref object obj, List<AMEventParameter> lsArray) {
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
            /*Type t = typeof(UnityEngine.Object);
            if(lsArray.Count > 0) t = lsArray[0].GetType();

            var list = typeof(List<>);
            var listOfType = list.MakeGenericType(t);
				
            var ls = Activator.CreateInstance(listOfType);
				
            for(int i=0;i<lsArray.Count;i++) {
                setObjectInArray(ref ls[i],ls[i].lsArray);
            }
            obj = ls.ToArray();*/
            object[] arrArray = new object[lsArray.Count];
            //t[] arrArray = new t[lsArray.Count];
            for(int i = 0; i < lsArray.Count; i++) setObjectInArray(ref arrArray[i], lsArray[i].lsArray);//arrArray[i] = (object[])lsArray[i].toArray();
            obj = arrArray;

            return;
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

    public override Tweener buildTweener(AMITarget itarget, Sequence sequence, UnityEngine.Object target, int frameRate) {
		if(methodName == null) return null;

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
		if(cachedMethodInfo == null)
			cachedMethodInfo = comp.GetType().GetMethod(methodName, GetParamTypes());

        if(frameLimit) {
			sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackLimitFrame, comp, frameRate, (object)buildParams());
        }
        else {
            if(useSendMessage) {
                if(parameters == null || parameters.Count <= 0)
					sequence.InsertCallback(getWaitTime(frameRate, 0.0f), comp.gameObject, methodName, null, SendMessageOptions.DontRequireReceiver);
                else
					sequence.InsertCallback(getWaitTime(frameRate, 0.0f), comp.gameObject, methodName, parameters[0].toObject(), SendMessageOptions.DontRequireReceiver);
            }
            else {
				sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackParams, comp, (object)buildParams());
            }
        }

        return null;
    }

    void OnMethodCallbackParams(TweenEvent dat) {
		Component comp = dat.parms[0] as Component;
		object[] parms = dat.parms[1] as object[];

        if(comp == null) return;

		cachedMethodInfo.Invoke(comp, parms);
    }

    //only call method if elapse is within frame
    void OnMethodCallbackLimitFrame(TweenEvent dat) {
		Component comp = dat.parms[0] as Component;

		if(comp == null) return;

		int frameRate = (int)dat.parms[1];
		object[] parms = dat.parms[2] as object[];

        float elapsed = dat.tween.elapsed;
        float curFrame = ((float)frameRate) * elapsed;

        if(curFrame > frame + getNumberOfFrames()) return;

        
        if(useSendMessage) {
            if(parms.Length == 0)
				comp.gameObject.SendMessage(methodName, null, SendMessageOptions.DontRequireReceiver);
            else
				comp.gameObject.SendMessage(methodName, parms[0], SendMessageOptions.DontRequireReceiver);
        }
        else {
			cachedMethodInfo.Invoke(comp, parms);
        }
    }
    #endregion
}
