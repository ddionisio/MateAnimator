using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

[System.Serializable]
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
		MorphChannels = 8,
		String = 9
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
	public PropertyInfo propertyInfo{
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
	public FieldInfo fieldInfo{
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
	public MethodInfo methodInfo{
		get {
			if(cachedMethodInfo != null) return cachedMethodInfo;
			if(!obj || !component || methodName == null) return null;
			Type[] t = new Type[methodParameterTypes.Length];
			for(int i=0;i<methodParameterTypes.Length;i++) t[i] = Type.GetType(methodParameterTypes[i]);
			cachedMethodInfo = component.GetType().GetMethod(methodName,t);
			return cachedMethodInfo;
		}
		set {
			if(value != null) methodName = value.Name;
			else methodName = null;
			cachedMethodInfo = value;
		}
	}
	private MethodInfo cachedMethodInfoMorphNames;
	public MethodInfo methodInfoMorphNames {
		get {
			if(cachedMethodInfoMorphNames != null) return cachedMethodInfoMorphNames;
			if(!obj || !component || valueType != (int)ValueType.MorphChannels) return null;
			cachedMethodInfoMorphNames = component.GetType().GetMethod("GetChannelNames");
			return cachedMethodInfo;
		}
	}
	public override string getTrackType() {
		if(fieldInfo != null) return fieldInfo.Name;
		if(propertyInfo != null) return propertyInfo.Name;
		if(methodInfo != null) {
			if(valueType == (int)ValueType.MorphChannels) return "Morph";	
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
			if(valueType == (int)ValueType.MorphChannels) return true;	
		}
		return false;
	}
	
	// add key
	public void addKey(int _frame) {
		if (isValueTypeNumeric(valueType))
			addKey(_frame,getPropertyValueNumeric());
		else if (valueType == (int)ValueType.Vector2)
			addKey(_frame,getPropertyValueVector2());
		else if (valueType == (int)ValueType.Vector3)
			addKey(_frame,getPropertyValueVector3());
		else if (valueType == (int)ValueType.Color)
			addKey(_frame,getPropertyValueColor());
		else if(valueType == (int)ValueType.Rect)
			addKey (_frame,getPropertyValueRect());
		else if(valueType == (int)ValueType.MorphChannels)
			addKeyMegaMorph(_frame,new List<float>());
		else
			Debug.LogError("Animator: Invalid ValueType "+valueType.ToString());
	}
	// add key mega morph
	public void addKeyMegaMorph(int _frame, List<float> channels) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValueMegaMorph(channels);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValueMegaMorph(channels);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.easeInOutSine;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add key numeric
	public void addKey(int _frame, double val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.linear;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add key vector2
	public void addKey(int _frame, Vector2 val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.linear;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add key vector3
	public void addKey(int _frame, Vector3 val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.linear;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add key color
	public void addKey(int _frame, Color val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.linear;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
	}
	// add key rect
	public void addKey(int _frame, Rect val) {
		foreach(AMPropertyKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
				key.setValue(val);
				// update cache
				updateCache();
				return;
			}
		}
		AMPropertyKey a = ScriptableObject.CreateInstance<AMPropertyKey>();
		a.frame = _frame;
		a.setValue(val);
		// set default ease type to linear
		a.easeType = (int)AMTween.EaseType.linear;
		// add a new key
		keys.Add (a);
		// update cache
		updateCache();
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
		// destroy cache
		destroyCache();
		// create new cache
		cache = new List<AMAction>();
		for(int i=0;i<keys.Count;i++) {
			
				AMPropertyAction a = ScriptableObject.CreateInstance<AMPropertyAction> ();
				a.component = component;
				a.startFrame = keys[i].frame;
				if(keys.Count>(i+1)) a.endFrame = keys[i+1].frame;
				else a.endFrame = -1;
				a.valueType = valueType;
				Type _type = null;
				bool showError = true;
				int methodInfoType = -1;
				if(fieldInfo != null) {
					a.fieldInfo = fieldInfo;
					_type = fieldInfo.FieldType;
					showError = false;
				} else if(propertyInfo != null) {
					a.propertyInfo = propertyInfo;	
					_type = propertyInfo.PropertyType;
					showError = false;
				} else if(methodInfo != null) {
					a.methodInfo = methodInfo;
					a.methodParameterTypes = methodParameterTypes;
					if(valueType == (int)ValueType.MorphChannels) {
						methodInfoType = (int)ValueType.MorphChannels;
						showError = false;
					}
				}
				if (showError){
					Debug.LogError("Animator: Fatal Error; fieldInfo, propertyInfo and methodInfo are unset for Value Type "+valueType);
					a.destroy();
					return;
				}
				// set value
				if(methodInfoType == (int)ValueType.MorphChannels) {
					a.start_morph =  new List<float>((keys[i] as AMPropertyKey).morph);
					if(a.endFrame!=-1) a.end_morph = new List<float>((keys[i+1] as AMPropertyKey).morph);
				}
				else if(isNumeric(_type)) {
					a.start_val = (double)(keys[i] as AMPropertyKey).val;
					if(a.endFrame!=-1) a.end_val = (double)(keys[i+1] as AMPropertyKey).val;
				} else if (_type == typeof(Vector2)) {
					a.start_vect2 = (keys[i] as AMPropertyKey).vect2;
					if(a.endFrame!=-1) a.end_vect2= (keys[i+1] as AMPropertyKey).vect2;
				} else if (_type == typeof(Vector3)) {
					a.start_vect3 = (keys[i] as AMPropertyKey).vect3;
					if(a.endFrame!=-1) a.end_vect3= (keys[i+1] as AMPropertyKey).vect3;
				}else if (_type == typeof(Color)) {
					a.start_color = (keys[i] as AMPropertyKey).color;
					if(a.endFrame!=-1) a.end_color= (keys[i+1] as AMPropertyKey).color;
				}else if (_type == typeof(Rect)) {
					a.start_rect = (keys[i] as AMPropertyKey).rect;
					if(a.endFrame!=-1) a.end_rect= (keys[i+1] as AMPropertyKey).rect;
				}else {
					Debug.LogError("Animator: Fatal Error, property type '"+_type.ToString()+"' not found.");
					a.destroy();
					return;
				}
				// set action ease
				a.easeType = (keys[i] as AMPropertyKey).easeType;
				a.customEase = new List<float>(keys[i].customEase);
				// add to cache
				cache.Add (a);
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
			return Convert.ToDouble(propertyInfo.GetValue(component,null));
		}
		
	}
	// get Vector2 value. unsafe, must check to see if value is Vector2 first
	public Vector2 getPropertyValueVector2() {
		// field
		if(fieldInfo != null)
			return (Vector2)fieldInfo.GetValue(component);
		// property
		else 
			return (Vector2)propertyInfo.GetValue(component,null);

	}
	public Vector3 getPropertyValueVector3() {
		// field
		if(fieldInfo != null)
			return (Vector3)fieldInfo.GetValue(component);
		// property
		else 
			return (Vector3)propertyInfo.GetValue(component,null);

	}
	public Color getPropertyValueColor() {
		// field
		if(fieldInfo != null)
			return (Color)fieldInfo.GetValue(component);
		// property
		else 
			return (Color)propertyInfo.GetValue(component,null);

	}
	public Rect getPropertyValueRect() {
		// field
		if(fieldInfo != null)
			return (Rect)fieldInfo.GetValue(component);
		// property
		else 
			return (Rect)propertyInfo.GetValue(component,null);

	}
	public static bool isNumeric(Type t) {
		if((t == typeof(int)) || (t == typeof(long)) || (t == typeof(float)) || (t == typeof(double)))
			return true;
		return false;
	}
	
	public static bool isValidType(Type t) {
		if((t == typeof(int)) || (t == typeof(long)) || (t == typeof(float)) || (t == typeof(double)) || (t == typeof(Vector2)) || (t == typeof(Vector3)) || (t == typeof(Color)) || (t == typeof(Rect)))
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
		else if (t == typeof(Vector2))
			valueType = (int)ValueType.Vector2;	
		else if (t == typeof(Vector3)) 
			valueType = (int)ValueType.Vector3;	
		else if (t == typeof(Color)) 
			valueType = (int)ValueType.Color;	
		else if(t == typeof(Rect))
			valueType = (int)ValueType.Rect;
		else {
			valueType = -1;
			Debug.LogWarning("Animator: Value type "+t.ToString()+" is unsupported.");
		}

	}
	// preview a frame in the scene view
	public void previewFrame(float frame, bool quickPreview = false) {
		if(cache == null || cache.Count <= 0) {
			return;	
		}
		if(!component || !obj) return;
		int morph_count = 0;
		// if before or equal to first frame, or is the only frame
		if((frame <= (float) cache[0].startFrame)||((cache[0] as AMPropertyAction).endFrame == -1)) {
			//obj.rotation = (cache[0] as AMPropertyAction).getStartQuaternion();
			if(fieldInfo != null) {
				fieldInfo.SetValue(component,(cache[0] as AMPropertyAction).getStartValue());
				refreshTransform();
			} else if(propertyInfo != null) {
				propertyInfo.SetValue(component,(cache[0] as AMPropertyAction).getStartValue(),null);
				refreshTransform();
			} else if(methodInfo != null) {
				try {
					string[] channelNames = getMorphNames();
					morph_count = channelNames.Length;
				} catch {
					// do nothing
				}
				previewMorph((cache[0] as AMPropertyAction).start_morph,morph_count);
			}
			return;
		}
		// if beyond or equal to last frame
		if(frame >= (float) (cache[cache.Count-2] as AMPropertyAction).endFrame) {
			if(fieldInfo != null) {
				fieldInfo.SetValue(component,(cache[cache.Count-2] as AMPropertyAction).getEndValue());
				refreshTransform();
			} else if(propertyInfo != null) {
				propertyInfo.SetValue(component,(cache[cache.Count-2] as AMPropertyAction).getEndValue(),null);
				refreshTransform();
			} else if(methodInfo != null) {
				string[] channelNames = getMorphNames();
				morph_count = channelNames.Length;
				previewMorph((cache[cache.Count-2] as AMPropertyAction).end_morph,morph_count);
			}
			return;
		}
		// if lies on property action
		foreach(AMPropertyAction action in cache) {
			if((frame<(float)action.startFrame)||(frame>(float)action.endFrame)) continue;
			if(quickPreview && !action.targetsAreEqual()) return;	// quick preview; if action will execute then skip
			// if on startFrame
			if(frame == (float)action.startFrame) {
				if(fieldInfo != null) {
					fieldInfo.SetValue(component,action.getStartValue());
					refreshTransform();
				} else if(propertyInfo != null) {
					propertyInfo.SetValue(component,action.getStartValue(),null);
					refreshTransform();
				} else if(methodInfo != null) {
					string[] channelNames = getMorphNames();
					morph_count = channelNames.Length;
					previewMorph(action.start_morph,morph_count);
				}
				return;	
			}
			// if on endFrame
			if(frame == (float)action.endFrame) {
				if(fieldInfo != null) {
					fieldInfo.SetValue(component,action.getEndValue());
					refreshTransform();
				} else if(propertyInfo != null) {
					propertyInfo.SetValue(component,action.getEndValue(),null);
					refreshTransform();
				} else if(methodInfo != null) {
					string[] channelNames = getMorphNames();
					morph_count = channelNames.Length;
					previewMorph(action.end_morph,morph_count);
				}
				return;	
			}
			// else find value using easing function

			AMTween.EasingFunction ease;
			AnimationCurve curve = null;
			
			if(action.hasCustomEase()) {
				ease = AMTween.customEase;
				curve = action.easeCurve;
			} else {
				ease = AMTween.GetEasingFunction((AMTween.EaseType)action.easeType);
			}
			
			float framePositionInAction = frame-(float)action.startFrame;
			if (framePositionInAction<0f) framePositionInAction = 0f;
			float percentage = framePositionInAction/action.getNumberOfFrames();
			

			
			//qCurrent.x = ease(qStart.x,qEnd.x,percentage);
			if(action.valueType == (int)ValueType.MorphChannels) {
				string[] channelNames = getMorphNames();
				morph_count = channelNames.Length;
				List<float> morphCurrent = new List<float>();
				//bool isMorphValid = true;
				for(int p=0;p<morph_count;p++) {
					if(action.start_morph.Count<=p || action.end_morph.Count<=p) {
						//isMorphValid = false;
						//Debug.Log("p = "+p+"; invalid morph! start count: "+action.start_morph.Count+", end count: "+action.end_morph.Count);
						break;
					}
					morphCurrent.Add(0f);
					morphCurrent[p] = ease(action.start_morph[p],action.end_morph[p],percentage,curve);
				}
				/*if(isMorphValid)*/ previewMorph(morphCurrent,morph_count);
			} else if(action.valueType == (int)ValueType.Integer) {
				float vStartInteger = Convert.ToSingle(action.start_val);
				float vEndInteger = Convert.ToSingle(action.end_val);
				int vCurrentInteger = (int)ease(vStartInteger,vEndInteger,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentInteger);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentInteger,null);
				refreshTransform();
			} else if(action.valueType == (int)ValueType.Long) {
				float vStartLong = Convert.ToSingle(action.start_val);
				float vEndLong = Convert.ToSingle(action.end_val);
				long vCurrentLong = (long)ease(vStartLong,vEndLong,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentLong);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentLong,null);
				refreshTransform();
			} else if(action.valueType == (int)ValueType.Float) {
				float vStartFloat = Convert.ToSingle(action.start_val);
				float vEndLong = Convert.ToSingle(action.end_val);
				float vCurrentFloat = (float)ease(vStartFloat,vEndLong,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentFloat);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentFloat,null);
				refreshTransform();
			} else if(action.valueType == (int)ValueType.Double) {
				float vStartDouble = Convert.ToSingle(action.start_val);
				float vEndDouble = Convert.ToSingle(action.end_val);
				double vCurrentDouble = (double)ease(vStartDouble,vEndDouble,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentDouble);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentDouble,null);
				refreshTransform();
			}else if(action.valueType == (int)ValueType.Vector2) {
				Vector2 vStartVector2 = action.start_vect2;
				Vector2 vEndVector2 = action.end_vect2;
				Vector2 vCurrentVector2 = new Vector2();
				vCurrentVector2.x = ease(vStartVector2.x,vEndVector2.x,percentage,curve);
				vCurrentVector2.y = ease(vStartVector2.y,vEndVector2.y,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentVector2);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentVector2,null);
				refreshTransform();
			} else if(action.valueType == (int)ValueType.Vector3) {
				Vector3 vStartVector3 = action.start_vect3;
				Vector3 vEndVector3 = action.end_vect3;
				Vector3 vCurrentVector3 = new Vector3();
				vCurrentVector3.x = ease(vStartVector3.x,vEndVector3.x,percentage,curve);
				vCurrentVector3.y = ease(vStartVector3.y,vEndVector3.y,percentage,curve);
				vCurrentVector3.z = ease(vStartVector3.z,vEndVector3.z,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentVector3);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentVector3,null);
				refreshTransform();
			} else if(action.valueType == (int)ValueType.Color) {
				Color vStartColor = action.start_color;
				Color vEndColor = action.end_color;
				Color vCurrentColor = new Color();
				vCurrentColor.r = ease(vStartColor.r,vEndColor.r,percentage,curve);
				vCurrentColor.g = ease(vStartColor.g,vEndColor.g,percentage,curve);
				vCurrentColor.b = ease(vStartColor.b,vEndColor.b,percentage,curve);
				vCurrentColor.a = ease(vStartColor.a,vEndColor.a,percentage,curve);
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentColor);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentColor,null);
				refreshTransform();
			}else if(action.valueType == (int)ValueType.Rect) {
				Rect vStartRect = action.start_rect;
				Rect vEndRect = action.end_rect;
				Rect vCurrentRect = new Rect();
				vCurrentRect.x = ease(vStartRect.x,vEndRect.x,percentage,curve);
				vCurrentRect.y = ease(vStartRect.y,vEndRect.y,percentage,curve);
				vCurrentRect.width = ease(vStartRect.width,vEndRect.width,percentage,curve);
				vCurrentRect.height = ease(vStartRect.height,vEndRect.height,percentage,curve);	
				if(fieldInfo != null) fieldInfo.SetValue(component,vCurrentRect);
				else if(propertyInfo != null) propertyInfo.SetValue(component,vCurrentRect,null);
				refreshTransform();
			} else {
				Debug.LogError("Animator: Invalid ValueType "+valueType.ToString());	
			}

			return;
		}
	}
	public void refreshTransform() {
		if(Application.isPlaying || !obj) return;
		obj.transform.position = new Vector3(obj.transform.position.x,obj.transform.position.y,obj.transform.position.z);	
	}
	public void previewMorph(List<float> morph,int count) {
		if(valueType == (int)ValueType.MorphChannels) {
			/*for(int i=0;i<count;i++) {
				if(i >= morph.Count) break;
				if(morph[i] == 0f) continue;
				methodInfo.Invoke(component, new object[]{i,morph[i]});
			}*/
			parentTake.addMorph(obj,methodInfo,component,morph);
		}
		// update transform to refresh scene
		//if(Application.isEditor) obj.transform.position = new Vector3(obj.transform.position.x,obj.transform.position.y,obj.transform.position.z);
	}
	/*public void previewMorph(List<float> morph,int count) {
		if(valueType == (int)ValueType.MorphChannels) {
			for(int i=0;i<count;i++) {
				if(i >= morph.Count) break;
				//if(morph[i] == 0f) continue;	// test
				methodInfo.Invoke(component, new object[]{i,morph[i]});
			}
		}
		// update transform to refresh scene
		//if(Application.isEditor) obj.transform.position = new Vector3(obj.transform.position.x,obj.transform.position.y,obj.transform.position.z);
	}*/
	public string getComponentName() {
		if(fieldInfo != null) return fieldInfo.DeclaringType.Name;
		if(propertyInfo != null) return propertyInfo.DeclaringType.Name;
		if(methodInfo != null) {
			if(valueType == (int)ValueType.MorphChannels) return "Morph";	
		}
		return "Unknown";
	}
	public string getValueInitialization(int codeLanguage, string varName) {
		string s = "";
		if(valueType == (int)ValueType.MorphChannels) {
			s += "AMTween.SetMorph("+varName+", "+varName+"Property, "+(cache[0] as AMPropertyAction).getFloatArrayString(codeLanguage, (cache[0] as AMPropertyAction).start_morph)+");";
			return s;
		}
		s += varName+"Property.";
		if(methodInfo != null) {
			s += "Invoke("+varName+", ";
			if(codeLanguage == 0) s += "new object[]{";
			else s += "[";
		} else {
			s+= "SetValue("+varName+", ";	
		}
		/*if(valueType == (int)ValueType.MorphChannels) {
			s += (cache[0] as AMPropertyAction).getFloatArrayString(codeLanguage, (cache[0] as AMPropertyAction).start_morph);
		}else */if(valueType == (int)ValueType.Integer) {
			float vStartInteger = Convert.ToSingle((cache[0] as AMPropertyAction).start_val);
			s += vStartInteger;
		} else if(valueType == (int)ValueType.Long) {
			float vStartLong = Convert.ToSingle((cache[0] as AMPropertyAction).start_val);
			s += vStartLong;
		} else if(valueType == (int)ValueType.Float) {
			float vStartFloat = Convert.ToSingle((cache[0] as AMPropertyAction).start_val);
			s += vStartFloat;
			if(codeLanguage == 0) s+= "f";
		} else if(valueType == (int)ValueType.Double) {
			float vStartDouble = Convert.ToSingle((cache[0] as AMPropertyAction).start_val);
			s += vStartDouble;
			if(codeLanguage == 0) s+= "f";
		}else if(valueType == (int)ValueType.Vector2) {
			Vector2 vStartVector2 = (cache[0] as AMPropertyAction).start_vect2;
			if(codeLanguage == 0) s+= "new Vector2("+vStartVector2.x+"f, "+vStartVector2.y+"f)";
			else s+= "Vector2("+vStartVector2.x+", "+vStartVector2.y+")";
		} else if(valueType == (int)ValueType.Vector3) {
			Vector3 vStartVector3 = (cache[0] as AMPropertyAction).start_vect3;
			if(codeLanguage == 0) s+= "new Vector3("+vStartVector3.x+"f, "+vStartVector3.y+"f, "+vStartVector3.z+"f)";
			else s+= "Vector3("+vStartVector3.x+", "+vStartVector3.y+", "+vStartVector3.z+")";
		} else if(valueType == (int)ValueType.Color) {
			Color vStartColor = (cache[0] as AMPropertyAction).start_color;
			if(codeLanguage == 0) s+= "new Color("+vStartColor.r+"f, "+vStartColor.g+"f, "+vStartColor.b+"f, "+vStartColor.a+"f)";
			else s+= "Color("+vStartColor.r+", "+vStartColor.g+", "+vStartColor.b+", "+vStartColor.a+")";
		}else if(valueType == (int)ValueType.Rect) {
			Rect vStartRect = (cache[0] as AMPropertyAction).start_rect;
			if(codeLanguage == 0) s+= "new Rect("+vStartRect.x+"f, "+vStartRect.y+"f, "+vStartRect.width+"f, "+vStartRect.height+"f)";
			else s+= "Rect("+vStartRect.x+", "+vStartRect.y+", "+vStartRect.width+", "+vStartRect.height+")";
		}
		/*if(fieldInfo != null) s += ");";
		else */if(propertyInfo != null) s += ", null";
		else if(methodInfo != null) {
			if(codeLanguage == 0) s += "}";
			else s += "]";
			
		}
		s += ");";
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
	
	public string[] getMorphNames() {
		if(!component || methodInfoMorphNames == null) return new string[]{};
		return (string[])methodInfoMorphNames.Invoke(component,null);	
	}
	
	public override AnimatorTimeline.JSONInit getJSONInit ()
	{
		if(!obj || keys.Count <= 0 || cache.Count <= 0) return null;
		AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
		init.go = obj.gameObject.name;
		List<string> strings = new List<string>();
		strings.Add(component.GetType().Name);
		if(fieldInfo != null) {
			init.typeExtra = "fieldinfo";
			strings.Add(fieldInfo.Name);
		} else if(propertyInfo != null) {
			init.typeExtra = "propertyinfo";
			strings.Add(propertyInfo.Name);
		} else if(methodInfo != null) {
			init.typeExtra = "methodinfo";
			strings.Add(methodInfo.Name);
			// parameter types
			foreach(string s in methodParameterTypes) {
				strings.Add(s);	
			}
		}
		if(valueType == (int)ValueType.MorphChannels) {
			init.type = "propertymorph";
			init.floats = (cache[0] as AMPropertyAction).start_morph.ToArray();
		} else if(valueType == (int)ValueType.Integer) {
			init.type = "propertyint";
			init._int = Convert.ToInt32((cache[0] as AMPropertyAction).start_val);
		} else if(valueType == (int)ValueType.Long) {
			init.type = "propertylong";
			init._long = Convert.ToInt64((cache[0] as AMPropertyAction).start_val);
		} else if(valueType == (int)ValueType.Float) {
			init.type = "propertyfloat";
			init.floats = new float[]{Convert.ToSingle((cache[0] as AMPropertyAction).start_val)};
		} else if(valueType == (int)ValueType.Double) {
			init.type = "propertydouble";
			init._double = (cache[0] as AMPropertyAction).start_val;
		} else if(valueType == (int)ValueType.Vector2) {
			init.type = "propertyvect2";
			AnimatorTimeline.JSONVector2 v2 = new AnimatorTimeline.JSONVector2();
			v2.setValue((cache[0] as AMPropertyAction).start_vect2);
			init._vect2 = v2;
		} else if(valueType == (int)ValueType.Vector3) {
			init.type = "propertyvect3";
			AnimatorTimeline.JSONVector3 v3 = new AnimatorTimeline.JSONVector3();
			v3.setValue((cache[0] as AMPropertyAction).start_vect3);
			init.position = v3;
		} else if(valueType == (int)ValueType.Color) {
			init.type = "propertycolor";
			AnimatorTimeline.JSONColor c = new AnimatorTimeline.JSONColor();
			c.setValue((cache[0] as AMPropertyAction).start_color);
			init._color = c;
		} else if(valueType == (int)ValueType.Rect) {
			init.type = "propertyrect";
			AnimatorTimeline.JSONRect r = new AnimatorTimeline.JSONRect();
			r.setValue((cache[0] as AMPropertyAction).start_rect);
			init._rect = r;
		} else {
			Debug.LogWarning("Animator: Error exporting JSON, unknown Property ValueType "+valueType);
			return null;
		}
		init.strings = strings.ToArray();
		return init;
	}
	
	public override List<GameObject> getDependencies() {
		List<GameObject> ls = new List<GameObject>();
		if(obj) ls.Add(obj);
		return ls;
	}
	
	public override List<GameObject> updateDependencies (List<GameObject> newReferences, List<GameObject> oldReferences)
	{
		List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!obj) return lsFlagToKeep;
		for(int i=0;i<oldReferences.Count;i++) {
			if(oldReferences[i] == obj) {
				string componentName = component.GetType().Name;
				Component _component = newReferences[i].GetComponent(componentName);
				
				// missing component
				if(!_component) {
					Debug.LogWarning("Animator: Property Track component '"+componentName+"' not found on new reference for GameObject '"+obj.name+"'. Duplicate not replaced.");
					lsFlagToKeep.Add(oldReferences[i]);
					return lsFlagToKeep;
				}
				// missing property
				if(_component.GetType().GetProperty(propertyName) == null) {
					Debug.LogWarning("Animator: Property Track property '"+propertyName+"' not found on new reference for GameObject '"+obj.name+"'. Duplicate not replaced.");
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
}
