using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[AddComponentMenu("")]
public class AMEventKey : AMKey {

    private struct ParamKeep {
        public System.Type type;
        public object val;
    }

    public Component component;
    public bool frameLimit = true;
    public bool useSendMessage = true;
    public List<AMEventParameter> parameters = new List<AMEventParameter>();
    public string methodName;
    private MethodInfo cachedMethodInfo;
    public MethodInfo methodInfo {
        get {
            if(component == null) return null;
            if(cachedMethodInfo != null) return cachedMethodInfo;
            if(methodName == null) return null;
            cachedMethodInfo = component.GetType().GetMethod(methodName);
            return cachedMethodInfo;
        }
        set {
            if(value != null) methodName = value.Name;
            else methodName = null;
            cachedMethodInfo = value;

        }
    }

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

    public bool setMethodInfo(Component component, MethodInfo methodInfo, ParameterInfo[] cachedParameterInfos, bool restoreValues) {
        // if different component or methodinfo
        if((this.methodInfo != methodInfo) || (this.component != component) || !isMatch(cachedParameterInfos)) {
            this.component = component;
            this.methodInfo = methodInfo;
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
    public override AMKey CreateClone() {

        AMEventKey a = gameObject.AddComponent<AMEventKey>();
        a.enabled = false;
        a.frame = frame;
        a.component = component;
        a.useSendMessage = useSendMessage;
        a.frameLimit = frameLimit;
        // parameters
        a.methodName = methodName;
        a.methodInfo = methodInfo;
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
            string componentName = component.GetType().Name;
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
}
