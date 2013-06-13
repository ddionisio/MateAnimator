using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

[System.Serializable]
public class AMEventAction : AMAction {
	public Component component;
	public bool useSendMessage;
	public List<AMEventParameter> parameters;
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
	public override void execute(int frameRate, float delay) {
		if(useSendMessage) {
			if(component == null || methodName == null) return;
			if(parameters == null || parameters.Count <= 0) AMTween.SendMessage(component.gameObject, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "methodname", methodName));
			else {
				AMTween.SendMessage(component.gameObject, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "methodname", methodName, "parameter", parameters[0].toObject()));
			}
			return;
		}
		if(component == null || methodInfo == null) return;
		object[] arrParams = new object[parameters.Count];
		for(int i=0;i<parameters.Count;i++) {
			if(parameters[i].isArray()) {
				setObjectInArray(ref arrParams[i],parameters[i].lsArray);
			} else {
				arrParams[i] = parameters[i].toObject();
			}
		}
		if(arrParams.Length<=0) arrParams = null;
		AMTween.InvokeMethod(component,AMTween.Hash ("delay",getWaitTime(frameRate,delay),"methodinfo",methodInfo,"parameters",arrParams));
	}
	public void setObjectInArray(ref object obj, List<AMEventParameter> lsArray) {
		if(lsArray.Count<=0) return;
		int valueType = lsArray[0].valueType;
		if(valueType == (int) AMEventParameter.ValueType.String) {
			string[] arrString = new string[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrString[i] = (string)lsArray[i].toObject();
			obj = arrString;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Char) {
			char[] arrChar = new char[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrChar[i] = (char)lsArray[i].toObject();
			obj = arrChar;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Integer || valueType == (int) AMEventParameter.ValueType.Long) {
			int[] arrInt = new int[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrInt[i] = (int)lsArray[i].toObject();
			obj = arrInt;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Float || valueType == (int) AMEventParameter.ValueType.Double) {
			float[] arrFloat = new float[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrFloat[i] = (float)lsArray[i].toObject();
			obj = arrFloat;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Vector2) {
			Vector2[] arrVect2 = new Vector2[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrVect2[i] = new Vector2(lsArray[i].val_vect2.x,lsArray[i].val_vect2.y);
			obj = arrVect2;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Vector3) {
			Vector3[] arrVect3 = new Vector3[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrVect3[i] = new Vector3(lsArray[i].val_vect3.x,lsArray[i].val_vect3.y,lsArray[i].val_vect3.z);
			obj = arrVect3;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Vector4) {
			Vector4[] arrVect4 = new Vector4[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrVect4[i] =  new Vector4(lsArray[i].val_vect4.x,lsArray[i].val_vect4.y,lsArray[i].val_vect4.z,lsArray[i].val_vect4.w);
			obj = arrVect4;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Color) {
			Color[] arrColor = new Color[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrColor[i] = new Color(lsArray[i].val_color.r,lsArray[i].val_color.g,lsArray[i].val_color.b,lsArray[i].val_color.a);
			obj = arrColor;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Rect) {
			Rect[] arrRect = new Rect[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrRect[i] = new Rect(lsArray[i].val_rect.x,lsArray[i].val_rect.y,lsArray[i].val_rect.width,lsArray[i].val_rect.height);
			obj = arrRect;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Object) {
			UnityEngine.Object[] arrObject = new UnityEngine.Object[lsArray.Count];
			for(int i=0;i<lsArray.Count;i++) arrObject[i] = (UnityEngine.Object)lsArray[i].toObject();
			obj = arrObject;
			return;
		}
		if(valueType == (int) AMEventParameter.ValueType.Array) {
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
				for(int i=0;i<lsArray.Count;i++) setObjectInArray(ref arrArray[i],lsArray[i].lsArray);//arrArray[i] = (object[])lsArray[i].toArray();
				obj = arrArray;
				
				return;	
		}	
		obj = null;
	}
	public string ToString(int codeLanguage, int frameRate, string methodInfoVarName) {
		if(component == null) return null;
		string s = "";
		if(useSendMessage) {
			if(methodName == null) return null;
			
			
			if(codeLanguage == 0) {
				// c#
				s += "AMTween.SendMessage(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"methodname\", \""+methodName+"\"";
				if(parameters != null && parameters.Count>0) s += ", \"parameter\", "+parametersToString(codeLanguage);
				s += "));";
				
			} else {
				s += "AMTween.SendMessage(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"methodname\": \""+methodName+"\"";
				if(parameters != null && parameters.Count>0) s += ", \"parameter\": "+parametersToString(codeLanguage);
				s += "});";
			}
			return s;
		}
		
		if(codeLanguage == 0) {
			// c#
			s += "AMTween.InvokeMethod("+methodInfoVarName+"CMP, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"methodinfo\", "+methodInfoVarName;//obj.methodinfo";
			if(parameters != null && parameters.Count>0) s += ", \"parameters\", new object[]{"+parametersToString(codeLanguage)+"}";
			s += "));";
			
		} else {
			s += "AMTween.InvokeMethod("+methodInfoVarName+"CMP, {\"delay\": "+getWaitTime(frameRate,0f)+", \"methodinfo\": "+methodInfoVarName;//obj.methodinfo";
			if(parameters != null && parameters.Count>0) s += ", \"parameters\": ["+parametersToString(codeLanguage)+"]";
			s += "});";
		}
		return s;
	}
	
	private string parametersToString(int codeLanguage) {
		string s ="";
		for(int i=0;i<parameters.Count;i++) {
			s += declareObjectToString(parameters[i].toObject(),codeLanguage);	
			if(i<parameters.Count-1) s += ", ";
		}
		return s;
	}
	private string declareObjectToString(object obj, int codeLanguage) {
		if(obj is string) return "\""+obj.ToString()+"\"";
		if(obj is bool) return obj.ToString().ToLower();
		if(obj is char) return "'"+obj.ToString()+"'";
		if((codeLanguage == 0) && (obj is decimal)) return obj.ToString()+"m";
		if((codeLanguage == 0) && (obj is float)) return obj.ToString()+"f";
		if(obj is Vector2) {
			Vector2 obj_vect2 = (Vector2) obj;
			if(codeLanguage == 0) return "new Vector2("+obj_vect2.x+"f, "+obj_vect2.y+"f)";	
			else return "Vector2("+obj_vect2.x+", "+obj_vect2.y+")";	
		}
		if(obj is Vector3) {
			Vector3 obj_vect3 = (Vector3) obj;
			if(codeLanguage == 0) return "new Vector3("+obj_vect3.x+"f, "+obj_vect3.y+"f, "+obj_vect3.z+"f)";	
			else return "Vector3("+obj_vect3.x+", "+obj_vect3.y+", "+obj_vect3.z+")";	
		}
		if(obj is Vector4) {
			Vector4 obj_vect4 = (Vector4) obj;
			if(codeLanguage == 0) return "new Vector4("+obj_vect4.x+"f, "+obj_vect4.y+"f, "+obj_vect4.z+"f, "+obj_vect4.w+"f)";	
			else return "Vector4("+obj_vect4.x+", "+obj_vect4.y+", "+obj_vect4.z+", "+obj_vect4.w+")";		
		}
		if(obj is Color) {
			Color obj_color = (Color) obj;
			if(codeLanguage == 0) return "new Color("+obj_color.r+"f, "+obj_color.g+"f, "+obj_color.b+"f, "+obj_color.a+"f)";	
			else return "Color("+obj_color.r+", "+obj_color.g+", "+obj_color.b+", "+obj_color.a+")";		
		}
		if(obj is Rect) {
			Rect obj_rect = (Rect) obj;
			if(codeLanguage == 0) return "new Rect("+obj_rect.x+"f, "+obj_rect.y+"f, "+obj_rect.width+"f, "+obj_rect.height+"f)";	
			else return "Rect("+obj_rect.x+", "+obj_rect.y+", "+obj_rect.width+", "+obj_rect.height+")";		
		}
		if(obj is GameObject) {
			return "GameObject.Find(\""+(obj as GameObject).name+"\")";	
		}
		if(obj is Component) {
			return "GameObject.Find(\""+(obj as Component).gameObject.name+"\").GetComponent(\""+(obj as Component).GetType().Name+"\")";
		}
		if(obj is Array) {
			//Type t = null;
			//if((obj as Array).GetValue(0) != null) t = (obj as Array).GetValue(0).GetType();
			Type t = (obj as Array).GetType().GetElementType();
			string s = "";
			if(codeLanguage == 0) {
				// c#
				s += "new ";
				if(t == typeof(string)) {
					s += "string";	
				}else if(t == typeof(bool)) {
					s += "bool";	
				}else if(t == typeof(char)) {
					s += "char";	
				}else if(t == typeof(decimal)) {
					s += "decimal";	
				}else if(t == typeof(float)) {
					s += "float";	
				}else if(t == typeof(bool)) {
					s += "bool";	
				}else if(t == typeof(int)) {
					s += "int";	
				}else if(t == typeof(long)) {
					s += "long";	
				}else if(t == typeof(double)) {
					s += "double";	
				}else if(t == typeof(Vector2)) {
					s += "Vector2";	
				}else if(t == typeof(Vector3)) {
					s += "Vector3";	
				}else if(t == typeof(Color)) {
					s += "Color";	
				}else if(t == typeof(Rect)) {
					s += "Rect";	
				}else if(t == typeof(GameObject)) {
					s += "GameObject";	
				}else if(t == typeof(Component)) {
					s += (obj as Component).GetType().Name;	
				} else {
					s += "Object";
				}
				s += "[]{";
			} else {
				// js
				s += "[";	
			}
			// elements
			Array _obj = (Array) obj;
			for(int i=0;i<_obj.Length;i++){
				s += declareObjectToString(_obj.GetValue(i),codeLanguage);	
				if(i<_obj.Length-1) s+= ", ";
			}
			// close brackets
			if(codeLanguage == 0) s += "}";	// c#
			else s += "]";	// js
			return s;
		}
		if(obj == null) return null;
		else return obj.ToString();;
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(component == null) return null;
		
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.delay = getWaitTime(frameRate,0f);
		a.go = component.gameObject.name;
		if(useSendMessage) {
			if(methodName == null) return null;
			a.method = "sendmessage";
			a.strings = new string[]{methodName};
			if(parameters != null && parameters.Count > 0) {
				// set one param ( parameters[0].toObject() )
				a.eventParams = new AnimatorTimeline.JSONEventParameter[]{parameters[0].toJSON()};
			}
		} else {
			if(methodInfo == null) return null;
			
			a.method = "invokemethod";
			a.eventParams = new AnimatorTimeline.JSONEventParameter[parameters.Count];
			for(int i=0;i<parameters.Count;i++) {
				a.eventParams[i] = parameters[i].toJSON();	
			}
			a.strings = new string[]{component.GetType().Name,methodInfo.Name};
		}
		return a;
	}
}
