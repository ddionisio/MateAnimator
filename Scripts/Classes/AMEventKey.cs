using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[System.Serializable]
public class AMEventKey : AMKey {
	
	public Component component;
	public bool useSendMessage = true;
	public List<AMEventParameter> parameters = new List<AMEventParameter>();
	public string methodName;
	private MethodInfo cachedMethodInfo;
	public MethodInfo methodInfo{
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

	public bool setMethodInfo(Component component, MethodInfo methodInfo, ParameterInfo[] cachedParameterInfos) {
		// if different component or methodinfo
		if((this.methodInfo != methodInfo)||(this.component!=component)) {
			this.component = component;
			this.methodInfo = methodInfo;
			//this.parameters = new object[numParameters];
			destroyParameters();
			this.parameters = new List<AMEventParameter>();
			
			// add parameters
			for(int i=0;i<cachedParameterInfos.Length;i++) {
				AMEventParameter a = CreateInstance<AMEventParameter>();
				a.setValueType(cachedParameterInfos[i].ParameterType);
				this.parameters.Add (a);
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
	public override AMKey CreateClone ()
	{
		
		AMEventKey a = ScriptableObject.CreateInstance<AMEventKey>();
		a.frame = frame;
		a.component = component;
		a.useSendMessage = useSendMessage;
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
	
	public bool updateDependencies (List<GameObject> newReferences, List<GameObject> oldReferences, bool didUpdateObj, GameObject obj)
	{
		if(didUpdateObj && component) {
			string componentName = component.GetType().Name;
			component = obj.GetComponent(componentName);
			if(!component) Debug.LogError("Animator: Component '"+componentName+"' not found on new reference for GameObject '"+obj.name+"'. Some event track data may be lost.");
			cachedMethodInfo = null;
		}
		bool didUpdateParameter = false;
		foreach(AMEventParameter param in parameters) {
			if(param.updateDependencies(newReferences,oldReferences) && !didUpdateParameter) didUpdateParameter = true;
		}
		return didUpdateParameter;
	}
}
