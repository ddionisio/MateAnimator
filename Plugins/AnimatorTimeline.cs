using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AnimatorTimeline {
	private static AnimatorData _aData;
    public static AnimatorData aData {
		get 
		{
			if(_aData != null) return _aData;
			GameObject go = GameObject.Find ("AnimatorData");
            if(go) _aData = go.GetComponent < AnimatorData>();
			if(!_aData) Debug.LogWarning("Animator: Could not find AnimatorData component.");
			return _aData;
		}
	}
	
	#region JSON parser
	public static void ParseJSON(string textAssetName) {
		TextAsset t = (TextAsset) Resources.Load(textAssetName);
		if(!t) Debug.LogError("Animator: Could not find TextAsset '"+textAssetName+".txt', make sure it's placed in a Resources folder!");
		ParseJSON(t);
	}
	
	private static void ParseJSON(TextAsset json) {
		ParseJSONString(json.ToString());	
	}
	
	public static void ParseJSONString(string json) {
        fastJSON.JSON.Instance.Parameters.UseExtensions = false;
        JSONTake j = fastJSON.JSON.Instance.ToObject < JSONTake >(json);
		if(!parseJSONTake(j)) Debug.LogWarning("Animator: Error parsing JSON");
	}
	
	private static Dictionary<string,GameObject> dictGameObjects;
	private static Dictionary<string,Component> dictComponents;
	private static Dictionary<string,MethodInfo> dictMethodInfos;
	private static Dictionary<string,FieldInfo> dictFieldInfos;
	private static Dictionary<string,PropertyInfo> dictPropertyInfos;
	private static Camera[] allCameras;
	//private static Texture[] allTextures;
	
	public class JSONTake {
		public string takeName;
		public JSONInit[] inits;
		public JSONAction[] actions;
	}
	
	public class JSONInit {
		public string type;
		public string typeExtra;
		public string go;
		public JSONVector3 position;
		public JSONQuaternion rotation;
		public float[] floats;
		public string[] strings;
		public string[] stringsExtra;
		public int _int;
		public long _long;
		public double _double;
		public JSONVector2 _vect2;
		public JSONColor _color;
		public JSONRect _rect;
	}
	
	public class JSONAction {
		public string method;
		public string go;
		public float delay;
		public float time;
		public int easeType;
		public float[] customEase;
		public string[] strings;
		public string[] stringsExtra;
		public float[] floats;
		public float[] floatsExtra;
		public bool[] bools;
		public int[] ints;
		public long[] longs;
		public double[] doubles;
		// parameters
		public JSONVector3[] path;	// path, translation: use position if length <= 1, rotation: stores eulerAngles
		public JSONVector2[] vect2s;
		public JSONColor[] colors;
		public JSONRect[] rects;
		public JSONEventParameter[] eventParams;
		
		public void setPath(Vector3[] _path) {
			path = getJSONVector3Array(_path);
		}
		
		public Vector3[] getVector3Path() {
			List<Vector3> ls = new List<Vector3>();
			for(int i=0;i<path.Length;i++) {
				ls.Add(path[i].toVector3());
			}
			return ls.ToArray();
		}
	}
	
	public class JSONEventParameter {
		
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
		Boolean = 13
	}
		
		public int valueType;
		public bool val_bool;
		public int val_int;
		public float val_float;
		public JSONVector2 val_vect2;
		public JSONVector3 val_vect3;
		public JSONVector4 val_vect4;
		public JSONColor val_color;
		public JSONRect val_rect;
		public string val_string;
		public string val_obj;
		public string val_obj_extra;
		//public UnityEngine.Object val_obj;
		public JSONEventParameter[] array;
		
		public object toObject() {
		if(valueType == (int) ValueType.Boolean) return (val_bool/* as object*/);
		if(valueType == (int) ValueType.String) return (val_string/* as object*/);
		if(valueType == (int) ValueType.Char) {
			if(val_string == null || val_string.Length<=0) return '\0';
			return (val_string[0]/* as object*/);
		}
		if(valueType == (int) ValueType.Integer || valueType == (int) ValueType.Long) return (val_int/* as object*/);
		if(valueType == (int) ValueType.Float || valueType == (int) ValueType.Double) return (val_float/* as object*/);
		if(valueType == (int) ValueType.Vector2) return (val_vect2/* as object*/);
		if(valueType == (int) ValueType.Vector3) return (val_vect3/* as object*/);
		if(valueType == (int) ValueType.Vector4) return (val_vect4/* as object*/);
		if(valueType == (int) ValueType.Color) return (val_color/* as object*/);
		if(valueType == (int) ValueType.Rect) return (val_rect/* as object*/);
		if(valueType == (int) ValueType.Object) {
			if(val_obj_extra == null) return (UnityEngine.Object)getGO(val_obj);
			else return (UnityEngine.Object)getCMP(val_obj_extra, val_obj);
		}
		if(valueType == (int) ValueType.Array) {
			if(array == null || array.Length <= 0) {
				Debug.LogError("Animator: Expected array parameters in JSON");
				return null;
			}
			if(array[0].valueType == (int) ValueType.Boolean) {
				bool[] arrObj = new bool[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_bool;
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.String) {
				string[] arrObj = new string[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_string;
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Char) {
				char[] arrObj = new char[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_string[0];
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Integer || array[0].valueType == (int) ValueType.Long) {
				int[] arrObj = new int[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_int;
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Float || array[0].valueType == (int) ValueType.Double) {
				float[] arrObj = new float[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_float;
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Vector2) {
				Vector2[] arrObj = new Vector2[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_vect2.toVector2();
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Vector3) {
				Vector3[] arrObj = new Vector3[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_vect3.toVector3();
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Vector4) {
				Vector4[] arrObj = new Vector4[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_vect4.toVector4();
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Color) {
				Color[] arrObj = new Color[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_color.toColor();
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Rect) {
				Rect[] arrObj = new Rect[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = array[i].val_rect.toRect();
				}
				return arrObj;
			}
			if(array[0].valueType == (int) ValueType.Object) {
				UnityEngine.Object[] arrObj = new UnityEngine.Object[array.Length];
				for(int i=0;i<array.Length;i++){
					arrObj[i] = (UnityEngine.Object)getGO(array[i].val_obj);
				}
				return arrObj;
			}
			
		}
		Debug.LogError("Animator: Type not found for Event Parameter valueType "+valueType);
		return null;
	}
	}
	
	public class JSONVector4 {
		public float x;
		public float y;
		public float z;
		public float w;
		
		public void setValue(Vector4 v) {
			x = v.x;
			y = v.y;
			z = v.z;
			w = v.w;
		}
		
		public Vector4 toVector4() {
			return new Vector4(x,y,z,w);
		}
	}
	
	public class JSONVector3 {
		public float x;
		public float y;
		public float z;
		
		public void setValue(Vector3 v) {
			x = v.x;
			y = v.y;
			z = v.z;
		}
		
		public Vector3 toVector3() {
			return new Vector3(x,y,z);
		}
	}
	
	public class JSONVector2 {
		public float x;
		public float y;
		
		public void setValue(Vector2 v) {
			x = v.x;
			y = v.y;
		}
		
		public Vector2 toVector2() {
			return new Vector2(x,y);
		}
	}
	
	public class JSONQuaternion {
		public float x;
		public float y;
		public float z;
		public float w;
		
		public void setValue(Vector4 q) {
			x = q.x;
			y = q.y;
			z = q.z;
			w = q.w;
		}
		
		public Quaternion toQuaternion() {
			return new Quaternion(x,y,z,w);
		}
	}
	
	public class JSONColor {
		public float r;
		public float g;
		public float b;
		public float a;
		
		public void setValue(Color c) {
			r = c.r;
			g = c.g;
			b = c.b;
			a = c.a;
		}
		
		public Color toColor() {
			return new Color(r,g,b,a);
		}
	}
	
	public class JSONRect {
		public float x;
		public float y;
		public float width;
		public float height;
		
		public void setValue(Rect r) {
			x = r.x;
			y = r.y;
			width = r.width;
			height = r.height;
		}
		
		public Rect toRect() {
			return new Rect(x,y,width,height);
		}
	}
	
	private static JSONVector3[] getJSONVector3Array(Vector3[] v) {
		List<JSONVector3> ls = new List<JSONVector3>();
		foreach(Vector3 _v in v) {
			JSONVector3 jv = new JSONVector3();
			jv.setValue(_v);
			ls.Add(jv);
		}
		return ls.ToArray();
	}
	
	private static bool setInitialValue(JSONInit init) {
		switch(init.type) {
		case "position":
			if(init.position == null || init.go == null) return false;
			getGO(init.go).transform.position = init.position.toVector3();
			break;
		case "rotation":
			if(init.rotation == null || init.go == null) return false;
			getGO(init.go).transform.rotation = init.rotation.toQuaternion();
			break;
		case "orientation":
			// track4OBJ.transform.LookAt (new Vector3(0f, 1f, -5f)); // Set Initial Orientation
			if(init.position == null || init.go == null) return false;	
			getGO(init.go).transform.LookAt(init.position.toVector3());
			break;
		case "propertymorph":
			if(init.go == null || init.floats == null) return false;
			AMTween.SetMorph(getCMP(init.go, "MegaMorph"), getMethodInfo(init.go,"MegaMorph","SetPercent",new string[]{"System.Int32","System.Single"}), init.floats);
			break;
		case "propertyint":
			if(!setInitialValueForProperty(init,init._int)) return false;
			break;
		case "propertylong":
			if(!setInitialValueForProperty(init,init._long)) return false;
			break;
		case "propertyfloat":
			if(init.floats == null || init.floats.Length <= 0) return false;
			if(!setInitialValueForProperty(init,init.floats[0])) return false;
			break;
		case "propertydouble":
			if(!setInitialValueForProperty(init,init._double)) return false;
			break;
		case "propertyvect2":
			if(init._vect2 == null) return false;
			if(!setInitialValueForProperty(init,init._vect2.toVector2())) return false;
			break;
		case "propertyvect3":
			if(init.position == null) return false;
			if(!setInitialValueForProperty(init,init.position.toVector3())) return false;
			break;
		case "propertycolor":
			if(init._color == null) return false;
			if(!setInitialValueForProperty(init,init._color.toColor())) return false;
			break;
		case "propertyrect":
			if(init._rect == null) return false;
			if(!setInitialValueForProperty(init,init._rect.toRect())) return false;
			break;
		case "cameraswitcher":
			// setup all cameras
			if(init.strings != null && init.strings.Length > 0) {
				allCameras = new Camera[init.strings.Length];
				for(int i=0;i<init.strings.Length;i++) {
					allCameras[i] = getGO(init.strings[i]).camera;
				}
			}
			// setup all textures
			/*if(init.stringsExtra != null && init.stringsExtra.Length > 0) {
				allTextures = new Texture[init.stringsExtra.Length];
				for(int i=0;i<init.stringsExtra.Length;i++) {
					allTextures[i] = AMTween.LoadTexture2D(init.stringsExtra[i]);
				}
			}*/
			// set top camera
			if(init.typeExtra == "camera") {
				if(init.go == null) return false;
				AMTween.SetTopCamera(getGO(init.go).camera,allCameras);
			} else {
				if(init._color == null) return false;
				AMTween.ShowColor(init._color.toColor());
			}
			break;
		default:
			Debug.LogWarning("Animator: Error parsing initial value type '"+init.type+"'");
			return false;
		}
		return true;
	}
	
	private static bool setInitialValueForProperty(JSONInit init, object value) {
		if(init.go == null || value == null || init.strings == null || init.strings.Length < 2 || init.typeExtra == null) return false;
		if(init.typeExtra == "fieldInfo") getFieldInfo(init.go, init.strings[0],init.strings[1]).SetValue(getCMP(init.go,init.strings[0]),value);
		else getPropertyInfo(init.go, init.strings[0],init.strings[1]).SetValue(getCMP(init.go,init.strings[0]),value,null);
		return true;
	}
	
	// returns false if there were problems parsing the take
	private static bool  parseJSONTake(JSONTake j) {
		dictGameObjects = new Dictionary<string, GameObject>();
		dictComponents = new Dictionary<string, Component>();
		dictMethodInfos = new Dictionary<string, MethodInfo>();
		dictFieldInfos = new Dictionary<string, FieldInfo>();
		dictPropertyInfos = new Dictionary<string, PropertyInfo>();
		allCameras = null;
		//allTextures = null;
		bool errorOccured = false;
		// set initial values
		if(j.inits != null) {
			foreach(JSONInit i in j.inits) {
				if(!setInitialValue(i)) errorOccured = true;
			}
		}
		// execute actions
		if(j.actions != null) {
			foreach(JSONAction a in j.actions) {
				switch(a.method) {
				// translation
				case "moveto":
					if(!parseMoveTo(a)) errorOccured = true;
					break;
				case "rotateto":
					if(!parseRotateTo(a)) errorOccured = true;
					break;
				case "lookfollow":
					if(!parseLookFollow(a)) errorOccured = true;
					break;
				case "looktofollow":
					if(!parseLookToFollow(a)) errorOccured = true;
					break;
				case "playanimation":
					if(!parsePlayAnimation(a)) errorOccured = true;
					break;
				case "playaudio":
					if(!parsePlayAudio(a)) errorOccured = true;
					break;
				case "propertyto":
					if(!parsePropertyTo(a)) errorOccured = true;
					break;
				case "sendmessage":
					if(!parseSendMessage(a)) errorOccured = true;
					break;
				case "invokemethod":
					if(!parseInvokeMethod(a)) errorOccured = true;
					break;
				case "camerafade":
					if(!parseCameraFade(a)) errorOccured = true;
					break;
				default:
					Debug.LogWarning("Animator: Error parsing method '"+a.method+"'");
					errorOccured = true;
					break;
				}
			}
		}
		AMTween.StartDisabled();	// enable all tweens at once
		return !errorOccured;
	}
	
	private static bool parseCameraFade(JSONAction a) {
		if(a.ints == null || a.ints.Length < 3) {
			Debug.LogWarning("Animator: CameraFade missing fade type, start or end targets.");
			return false;
		}
		if(a.strings == null || a.strings.Length < 2 || a.colors == null || a.colors.Length < 2) {
			Debug.LogWarning("Animator: CameraFade missing start or end targets.");
			return false;
		}
		Hashtable hash = new Hashtable();
		hash.Add("time",a.time);
		hash.Add("delay",a.delay);
		setupHashEase(hash, a);
		if(a.bools != null && a.bools.Length > 0) hash.Add("reversed",a.bools[0]);
		if(a.ints[1] == 0 || a.ints[2] == 0) hash.Add("allcameras",allCameras);
		if(a.stringsExtra != null && a.stringsExtra.Length > 0) hash.Add("texture",AMTween.LoadTexture2D(a.stringsExtra[0]));
		if(a.ints[1] == 0) hash.Add("camera1",getGO(a.strings[0]).camera);
		else hash.Add("color1",a.colors[0].toColor());
		if(a.ints[2] == 0) hash.Add("camera2",getGO(a.strings[1]).camera);
		else hash.Add("color2",a.colors[1].toColor());
		
		float[] parameters = a.floats;
		if(parameters == null) parameters = new float[]{};
		
		AMTween.CameraFade(a.ints[0],(a.bools == null || a.bools.Length < 2 ? false : a.bools[1]),parameters,hash);
		return true;
		
	}
	
	private static bool parseInvokeMethod(JSONAction a) {
		if(a.strings == null || a.strings.Length < 2) {
			Debug.LogWarning("Animator: SendMessage missing Component or MethodInfo Name.");
			return false;
		}
		//AMTween.InvokeMethod(component,AMTween.Hash ("delay",getWaitTime(frameRate),"methodinfo",methodInfo,"parameters",arrParams));
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("methodinfo",getMethodInfo(a.go,a.strings[0],a.strings[1],null));
		if(a.eventParams != null && a.eventParams.Length > 0) {
			object[] arrParams = new object[a.eventParams.Length];
			for(int i=0;i<a.eventParams.Length;i++) {
				arrParams[i] = a.eventParams[i].toObject();
			}
			if(arrParams.Length<=0) arrParams = null;
			hash.Add("parameters",arrParams);
		}
		AMTween.InvokeMethod(getCMP(a.go,a.strings[0]),hash);
		return true;
	}
	
	private static bool parseSendMessage(JSONAction a) {
		if(a.strings == null || a.strings.Length < 1) {
			Debug.LogWarning("Animator: SendMessage missing Method Name.");
			return false;
		}
		//AMTween.SendMessage(component.gameObject, AMTween.Hash ("delay", getWaitTime(frameRate), "methodname", methodName, "parameter", parameters[0].toObject()));
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("methodname",a.strings[0]);
		if(a.eventParams != null && a.eventParams.Length > 0) {
			hash.Add("parameter",a.eventParams[0].toObject());
		}
		AMTween.SendMessage(getGO(a.go),hash);
		return true;
	}
	
	private static bool parsePropertyTo(JSONAction a) {
		//AMTween.PlayAudio(audioSource, AMTween.Hash ("delay", getWaitTime(frameRate), "audioclip", audioClip, "loop", loop));
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("time",a.time);
		setupHashEase(hash, a);
		
		if(a.strings == null || a.strings.Length < 2) {
			Debug.LogWarning("Animator: PropertyTo missing Component or property type.");
			return false;
		}
		
		string componentName = a.strings[0];
		bool missingTargets = false;
		switch(a.strings[1]) {
		case "morph":
			if(a.floats == null || a.floatsExtra == null) missingTargets = true;
			hash.Add("methodtype","morph");
			hash.Add("methodinfo",getMethodInfo(a.go,"MegaMorph","SetPercent",new string[]{"System.Int32","System.Single"}));
			hash.Add("from",a.floats);
			hash.Add("to",a.floatsExtra);
			break;
		case "integer":
			if(a.ints == null || a.ints.Length < 2) missingTargets = true;
			hash.Add("from",a.ints[0]);
			hash.Add("to",a.ints[1]);
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "long":
			if(a.longs == null || a.longs.Length < 2) missingTargets = true;
			hash.Add("from",a.longs[0]);
			hash.Add("to",a.longs[1]);
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "float":
			if(a.floats == null || a.floats.Length < 2) missingTargets = true;
			hash.Add("from",a.floats[0]);
			hash.Add("to",a.floats[1]);
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "double":
			if(a.doubles == null || a.doubles.Length < 2) missingTargets = true;
			hash.Add("from",a.doubles[0]);
			hash.Add("to",a.doubles[1]);
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "vector2":
			if(a.vect2s == null || a.vect2s.Length < 2) missingTargets = true;
			hash.Add("from",a.vect2s[0].toVector2());
			hash.Add("to",a.vect2s[1].toVector2());
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "vector3":
			if(a.path == null || a.path.Length < 2) missingTargets = true;
			hash.Add("from",a.path[0].toVector3());
			hash.Add("to",a.path[1].toVector3());
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "color":
			if(a.colors == null || a.colors.Length < 2) missingTargets = true;
			hash.Add("from",a.colors[0].toColor());
			hash.Add("to",a.colors[1].toColor());
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		case "rect":
			if(a.rects == null || a.rects.Length < 2) missingTargets = true;
			hash.Add("from",a.rects[0].toRect());
			hash.Add("to",a.rects[1].toRect());
			setupHashFieldOrPropertyInfo(hash,a);
			break;
		default:
			Debug.LogWarning("Animator: PropertyTo unknown property type '"+a.strings[1]+"'.");
			return false;
		}
		
		if(missingTargets) {
			Debug.LogWarning("Animator: PropertyTo missing 'to' or 'from' targets.");
			return false;
		}
		AMTween.PropertyTo(getCMP(a.go,componentName),hash);
		//AMTween.PlayAudio((AudioSource)getCMP(a.go,"AudioSource"), hash);
		return true;
	}
	
	private static void setupHashFieldOrPropertyInfo(Hashtable hash, JSONAction a) {
		if(a.strings.Length < 4) {
			Debug.LogWarning("Animator: PropertyTo missing Component or property type.");
			return;
		}
		if(a.strings[2] == "fieldinfo") hash.Add("fieldinfo",getFieldInfo(a.go,a.strings[0],a.strings[3]));
		else hash.Add("propertyinfo",getPropertyInfo(a.go,a.strings[0],a.strings[3]));
	}
	
	private static bool parsePlayAudio(JSONAction a) {
		//AMTween.PlayAudio(audioSource, AMTween.Hash ("delay", getWaitTime(frameRate), "audioclip", audioClip, "loop", loop));
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		if(a.strings.Length >= 1) {
			AudioClip audioClip = (AudioClip) Resources.Load(a.strings[0]);
			if(audioClip == null) {
				Debug.LogWarning("Animator: Could not find AudioClip '"+a.strings[0]+"'. Make sure the audio file is placed in a Resources folder!");
				return false;	
			}
			hash.Add("audioclip",audioClip);
		}
		else {
			Debug.LogWarning("Animator: PlayAudio missing 'audioclip' clip name.");
			return false;	
		}
		if(a.bools.Length >= 1) {
			hash.Add("loop",a.bools[0]);	
		} else {
			Debug.LogWarning("Animator: PlayAudio missing 'loop'.");
			return false;	
		}
		AMTween.PlayAudio((AudioSource)getCMP(a.go,"AudioSource"), hash);
		return true;
	}
	
	private static bool parsePlayAnimation(JSONAction a) {
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		if(a.strings.Length >= 1) hash.Add("animation",a.strings[0]);
		else {
			Debug.LogWarning("Animator: PlayAnimation missing 'animation' clip name.");
			return false;	
		}
		if(a.floats.Length >= 2) {
			hash.Add("wrapmode",(WrapMode)a.floats[0]);
			hash.Add("fadeLength",a.floats[1]);
		} else {
			Debug.LogWarning("Animator: PlayAnimation missing 'wrapmode' or 'fadeLength'.");
			return false;	
		}
		if(a.bools.Length >= 1) {
			hash.Add("crossfade",a.bools[0]);	
		} else {
			Debug.LogWarning("Animator: PlayAnimation missing 'crossfade'.");
			return false;	
		}
		AMTween.PlayAnimation(getGO(a.go), hash);
		return true;
	}
	
	private static bool parseLookToFollow(JSONAction a) {
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("time",a.time);
		setupHashEase(hash, a);
		if(a.strings.Length >= 1) hash.Add("looktarget",getGO(a.strings[0]).transform); // move to position
		else {
			Debug.LogWarning("Animator: LookFollow missing 'looktarget'.");
			return false;	
		}
		if(a.path != null && a.path.Length >=1) {
			hash.Add("endposition",a.path[0].toVector3());
		}
		AMTween.LookToFollow(getGO(a.go), hash);
		return true;
	}
	
	private static bool parseLookFollow(JSONAction a) {
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("time",a.time);
		setupHashEase(hash, a);
		if(a.strings.Length >= 1) hash.Add("looktarget",getGO(a.strings[0]).transform); // move to position
		else {
			Debug.LogWarning("Animator: LookFollow missing 'looktarget'.");
			return false;	
		}
		AMTween.LookFollow(getGO(a.go), hash);
		return true;
	}
	
	private static bool parseMoveTo(JSONAction a) {
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("time",a.time);
		setupHashEase(hash, a);
		if(a.path.Length > 1) hash.Add("path",a.getVector3Path()); // move to position
		else if(a.path.Length == 1) hash.Add("position", a.path[0].toVector3()); // move with path
		else {
			Debug.LogWarning("Animator: MoveTo missing 'position' or 'path'.");
			return false;	
		}
		AMTween.MoveTo(getGO(a.go), hash);
		return true;
	}
	
	private static bool parseRotateTo(JSONAction a) {
		Hashtable hash = new Hashtable();
		hash.Add("disable",true);
		hash.Add("delay",a.delay);
		hash.Add("time",a.time);
		setupHashEase(hash, a);
		if(a.path.Length >= 1) hash.Add("rotation",a.path[0].toVector3()); // rotate to eulerAngle
		else {
			Debug.LogWarning("Animator: RotateTo missing 'rotation'.");
			return false;	
		}
		AMTween.RotateTo(getGO(a.go), hash);
		return true;
	}
	
	private static void setupHashEase(Hashtable hashTable, AnimatorTimeline.JSONAction a) {
		if(a.customEase.Length > 0) {
			AnimationCurve easeCurve = AMTween.GenerateCurve(a.customEase);
			hashTable.Add("easecurve",easeCurve);
		} else {
			hashTable.Add("easetype",(AMTween.EaseType)a.easeType);	
		}
	}
	
	private static GameObject getGO(string name) {
		if(name == null) {
			Debug.LogWarning("Animator: Error parsing GameObject with null value");
			return null;	
		}
		if(dictGameObjects.ContainsKey(name)) return dictGameObjects[name];
		GameObject go = GameObject.Find(name);
		if(!go) Debug.LogWarning("Animator: Error parsing JSON; Could not find GameObject '"+name+"'.");
		dictGameObjects.Add(name,go);
		return go;
	}
	
	private static Component getCMP(string goName, string componentName) {
		if(goName == null || componentName == null) {
			Debug.LogWarning("Animator: Error parsing GameObject or Component with null value: "+goName+", "+componentName);
			return null;	
		}
		string key = goName + "." + componentName;
		if(dictComponents.ContainsKey(key)) return dictComponents[key];
		GameObject go = getGO(goName);
		Component c = go.GetComponent(componentName);
		if(!c) Debug.LogWarning("Animator: Error parsing JSON; Could not find Component '"+componentName+"' on GameObject '"+goName+"'.");
		dictComponents.Add(key, c);
		return c;
	}
	
	private static MethodInfo getMethodInfo(string goName, string componentName, string methodName, string[] typeNames) {
		if(methodName == null || componentName == null) {
			Debug.LogWarning("Animator: Error parsing MethodInfo or Component with null value");
			return null;	
		}
		string key = componentName +"."+ methodName;
		List<Type> types = new List<Type>();
		if(typeNames != null) {
			for(int i=0;i<typeNames.Length;i++) {
				key += "."+typeNames[i];	
			}
			// setup type list
			foreach(string s in typeNames) types.Add(Type.GetType(s));
		}
		if(dictMethodInfos.ContainsKey(key)) return dictMethodInfos[key];
		
		//dictMethodInfos.Add(key, Type.GetType(componentName).GetMethod(methodName,types.ToArray()));
		MethodInfo m;
		if(typeNames == null) m = getCMP(goName,componentName).GetType().GetMethod(methodName);
		else m = getCMP(goName,componentName).GetType().GetMethod(methodName,types.ToArray());
		dictMethodInfos.Add(key, m);
		//dictMethodInfos.Add(key, getCMP(goName,componentName).GetType().GetMethod(methodName,types.ToArray()));
		return dictMethodInfos[key];
	}
	
	private static FieldInfo getFieldInfo(string goName, string componentName, string fieldName) {
		if(fieldName == null || componentName == null) {
			Debug.LogWarning("Animator: Error parsing FieldInfo or Component with null value");
			return null;	
		}
		string key = componentName +"."+ fieldName;
		if(dictFieldInfos.ContainsKey(key)) return dictFieldInfos[key];
		//dictFieldInfos.Add(key, Type.GetType(componentName).GetField(fieldName));
		dictFieldInfos.Add(key, getCMP(goName,componentName).GetType().GetField(fieldName));
		return dictFieldInfos[key];
	}
	
	private static PropertyInfo getPropertyInfo(string goName, string componentName, string propertyName) {
		if(propertyName == null || componentName == null) {
			Debug.LogWarning("Animator: Error parsing PropertyInfo or Component with null value");
			return null;	
		}
		string key = componentName +"."+ propertyName;
		if(dictPropertyInfos.ContainsKey(key)) return dictPropertyInfos[key];
		//Debug.Log(Type.GetType("UnityEngine.AudioSource"));
		//dictPropertyInfos.Add(key, Type.GetType(componentName).GetProperty(propertyName));
		dictPropertyInfos.Add(key, getCMP(goName,componentName).GetType().GetProperty(propertyName));
		return dictPropertyInfos[key];
	}
	#endregion
}
