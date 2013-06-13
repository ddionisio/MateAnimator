using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class AMEasePicker : EditorWindow {
	
	public static AMEasePicker window = null;
	
	public static bool justSet = false;
	public static AMKey key = null;
	public static AMTrack track = null;
	public AnimatorData aData = null;
	public static int selectedIndex;
	public static int selectedSpeedIndex = 0;
	public static int category = 0;
	private AMOptionsFile oData = null;
	private AnimationCurve curve = new AnimationCurve();
	private AnimationCurve selectedCurve = new AnimationCurve();
	private bool isCustomEase = false;
	// texture
	Texture tex_orb;
    bool texLoaded = false;
	
	// skins
	private GUISkin skin = null;
	private string cachedSkinName = null;
	
	// search categories
	private List<List<string>> easeTypesFiltered = new List<List<string>>();
	private string[] categories = new string[]{"All","Quad","Cubic","Quart","Quint","Sine","Expo","Circ","Bounce","Back","Elastic","Other"};
	private string[] speedNames = new string[]{"1x","2x","4x"};
	private float[] speedValues = new float[]{1f,2f,4f};
	
	public static void refreshValues() {
		if(window == null) return;
		setValues(key,track);
	}
	
	public static void setValues(AMKey _key, AMTrack _track) {
		justSet = true;
		key = _key;
		track = _track;
		//aData = _aData;
		selectedIndex = key.easeType;
	}
	
	public static float waitPercent = 0.3f;
	private float percent = waitPercent*-1f;
	private float x_pos = 20f;
	
	void OnEnable() {
        if(!texLoaded) {
            tex_orb = AMEditorResource.LoadEditorTexture("am_orb");
            texLoaded = true;
        }

		window = this;
		this.maxSize = new Vector2(715f,398f);
		this.minSize = this.maxSize;
		this.wantsMouseMove = true;
		loadAnimatorData();
		oData = AMOptionsFile.loadFile();
		setupFilteredCategories();
		selectedIndex = getCategoryIndexForEase(key.easeType);
		if(selectedIndex < 0) {
			selectedIndex = key.easeType;
			category = 0;
		}
		
		if(getSelectedEaseName(category,selectedIndex) == "Custom") {
			isCustomEase = true;
		}
		if(isCustomEase && key.customEase.Count > 0) {
			curve = key.getCustomEaseCurve();	
		} else {
			setEasingCurve();
		}
	}
	
	void OnDisable() {
		window = null;
		justSet = false;
		key = null;
		track = null;
		aData = null;
	}
	void OnHierarchyChange()
	{
		if(!aData) reloadAnimatorData();
	}
	public void reloadAnimatorData() {
		aData = null;
		loadAnimatorData();
		AMTake take = aData.getCurrentTake();
		// update references for track and key
		bool shouldClose = true;
		foreach(AMTrack _track in take.trackValues) {
			if(track ==	_track) {
				track = _track;
				foreach(AMKey _key in track.keys) {
					if(key == _key) {
						key = _key;
						shouldClose = false;
					}
				}
			}
		}
		if(shouldClose) this.Close();
	}
	void loadAnimatorData()
	{
		GameObject go = GameObject.Find ("AnimatorData");
		if(go) {
			aData = (AnimatorData) go.GetComponent ("AnimatorData");
		}
	}
	void Update() {
		percent += 0.003f* speedValues[selectedSpeedIndex];
		if(percent > 1f+waitPercent) percent = waitPercent*-1f;
		float x_pos_start = 50f;
		float x_pos_end = position.width-50f-80f-200f;
		
		if(percent <= 1f) {
			AMTween.EasingFunction ease;
			AnimationCurve _curve = null; 
			if(isCustomEase) {
				_curve = curve;
				ease = AMTween.customEase;
			} else {
				ease = AMTween.GetEasingFunction((AMTween.EaseType)getSelectedEaseIndex(category,selectedIndex));
			}
			x_pos = ease(x_pos_start,x_pos_end,(percent < 0f ? 0f : percent), _curve);
			
		}
		this.Repaint();
	}
	

	
	void OnGUI() {
		
		this.title = "Ease: "+(oData.time_numbering ? AMTimeline.frameToTime(key.frame,(float)aData.getCurrentTake().frameRate)+" s" : key.frame.ToString());
		AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
		bool updateEasingCurve = false;
		
		GUIStyle styleBox = new GUIStyle(GUI.skin.button);
		styleBox.normal = GUI.skin.button.active;
		styleBox.hover = styleBox.normal;
		styleBox.border = GUI.skin.button.border;
		GUILayout.BeginArea(new Rect(5f,5f,position.width-10f,position.height-10f));
			GUILayout.BeginHorizontal();
				if(GUILayout.Button("", styleBox, GUILayout.Width(500f), GUILayout.Height(100f))) {
					selectedSpeedIndex = (selectedSpeedIndex+1) % speedValues.Length;
					percent = waitPercent*-1f;	
				}
				
			GUILayout.EndHorizontal();
			
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
				int prevCategory = category;
				bool updatedSelectedIndex = false;
				if(setCategory(GUILayout.SelectionGrid(category,categories,(position.width >= 715f ? 12 : 6),GUILayout.Width(position.width-16f)))) {
					selectedIndex = getSelectedEaseIndex(prevCategory,selectedIndex);
					selectedIndex = getCategoryIndexForEase(selectedIndex);
					if(selectedIndex < 0) {
						selectedIndex = 0;
						percent = waitPercent*-1f;
						updatedSelectedIndex = true;
					}
				}
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);
			
			GUILayout.BeginVertical(GUILayout.Height(233f));
				if(updatedSelectedIndex || setSelectedIndex(GUILayout.SelectionGrid(selectedIndex,easeTypesFiltered[category].ToArray(),3))) {
					percent = waitPercent*-1f;
					updateEasingCurve = true;
					if(getSelectedEaseName(category,selectedIndex) == "Custom") {
						isCustomEase = true;
						if(key.customEase.Count > 0) {
							curve = key.getCustomEaseCurve();	
						} else {
							setEasingCurve();
						}
					} else isCustomEase = false;
				}
			GUILayout.EndVertical();
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
				if(GUILayout.Button("Apply")) {
					bool shouldUpdateCache = false;
					if(isCustomEase) {
						key.setCustomEase(curve);
						shouldUpdateCache = true;
					}
					if(key.setEaseType(getSelectedEaseIndex(category,selectedIndex))) {
						shouldUpdateCache = true;
					}
					if(shouldUpdateCache) {
						// update cache when modifying varaibles
						track.updateCache();
						AMCodeView.refresh();
						// preview new position
						aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
						// save data
						EditorUtility.SetDirty(aData);
					}
					this.Close();
				}
				if(GUILayout.Button("Cancel")) {
					this.Close();
				}
			GUILayout.EndHorizontal();
		GUILayout.EndArea();
	
		// orb texture
		GUI.DrawTexture(new Rect(x_pos,15f,80f,80f),tex_orb);
		// speed label
		GUIStyle styleLabelRight = new GUIStyle(GUI.skin.label);
		styleLabelRight.alignment = TextAnchor.MiddleRight;
		styleLabelRight.normal.textColor = Color.white;
		EditorGUI.DropShadowLabel(new Rect(475f,5f,25f,25f),speedNames[selectedSpeedIndex],styleLabelRight);
		// draw border
		GUI.color = GUI.skin.window.normal.textColor;
		GUI.DrawTexture(new Rect(0f,0f,7f,110f),EditorGUIUtility.whiteTexture);
		GUI.DrawTexture(new Rect(position.width-209f,0f,208f,110f),EditorGUIUtility.whiteTexture);
		GUI.color = Color.white;
		
		// curve field
		if(updateEasingCurve) setEasingCurve();
		else if(!isCustomEase && didChangeCurve()) {
			isCustomEase = true;
			selectedIndex = getCategoryIndexForEaseName("Custom");
			if(selectedIndex < 0) {
				category = 0;
				selectedIndex = getCategoryIndexForEaseName("Custom");
			}
		}
		curve = EditorGUI.CurveField(new Rect(500f, 5f, 208f, 100f),curve,Color.blue,new Rect(0f,-0.5f,1f,2.0f));
	}
	
	private void setupFilteredCategories() {
		if(easeTypesFiltered != null && easeTypesFiltered.Count > 0) return;
		easeTypesFiltered = new List<List<string>>();
		List<string> used = new List<string>();
		foreach(string category in categories) {
			List<string> temp = new List<string>();
			foreach(string ease in AMTimeline.easeTypeNames) {
				if(category == "All") {
					temp.Add(ease);
				} else if(category == "Other") {
					if(!used.Contains(ease))
						temp.Add(ease);
				} else if(ease.Contains(category)) {
					temp.Add(ease);
					used.Add(ease);
				}	
			}
			easeTypesFiltered.Add(temp);
		}
	}
	
	private int getSelectedEaseIndex(int _category, int _index) {
		if(_category == 0) return _index;
		return easeTypesFiltered[0].IndexOf(easeTypesFiltered[_category][_index]);
	}
	private string getSelectedEaseName(int _category, int _index) {
		return easeTypesFiltered[_category][_index];
	}
	private int getCategoryIndexForEase(int _index) {
		string ease = easeTypesFiltered[0][_index];
		return easeTypesFiltered[category].IndexOf(ease);
	}
	private int getCategoryIndexForEaseName(string _name) {
		return easeTypesFiltered[category].IndexOf(_name);
	}
	public static bool setCategory(int _category) {
		if(category != _category) {
			category = _category;
			return true;
		}
		return false;
	}
	
	public static bool setSelectedIndex(int _index) {
		if(selectedIndex != _index) {
			selectedIndex = _index;
			return true;
		}
		return false;
	}
	
	public void setEasingCurve() {
		if(getSelectedEaseName(category,selectedIndex) == "Custom") {
			if(curve.length <= 0) {
				curve = getCurve(AMTween.EaseType.linear);
				this.Repaint();
			}
			return;
		}
		curve = new AnimationCurve();
		curve = getCurve((AMTween.EaseType)getSelectedEaseIndex(category,selectedIndex));
		selectedCurve = getCurve((AMTween.EaseType)getSelectedEaseIndex(category,selectedIndex));
		this.Repaint();
	}
	
	public bool didChangeCurve() {
		if(curve == null && selectedCurve == null) return false;
		if(curve.length != selectedCurve.length) return true;
		for(int i=0;i<curve.length;i++) {
			if(curve[i].time != selectedCurve[i].time || curve[i].value != selectedCurve[i].value || curve[i].inTangent != selectedCurve[i].inTangent|| curve[i].outTangent != selectedCurve[i].outTangent)
				return true;
		}
		return false;
	}
	public AnimationCurve getCurve(AMTween.EaseType easeType) {
		AnimationCurve _curve = new AnimationCurve();
		switch(easeType)
		{
			case AMTween.EaseType.linear:
				_curve.AddKey(new Keyframe(0f,0f,1f,1f));
				_curve.AddKey(new Keyframe(1f,1f,1f,1f));
				break;
			case AMTween.EaseType.easeInQuad:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(1f,1f,2f,2f));
				break;
			case AMTween.EaseType.easeOutQuad:
				_curve.AddKey(new Keyframe(0f,0f,2f,2f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInOutQuad:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,2f,2f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInCubic:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(1f,1f,3f,3f));
				break;
			case AMTween.EaseType.easeOutCubic:
				_curve.AddKey(new Keyframe(0f,0f,3f,3f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInOutCubic:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,3f,3f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInQuart:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.5f,0.064f,0.5f,0.5f));
				_curve.AddKey(new Keyframe(1f,1f,4f,4f));
				break;
			case AMTween.EaseType.easeOutQuart:
				_curve.AddKey(new Keyframe(0f,0f,4f,4f));
				_curve.AddKey(new Keyframe(0.5f,0.936f,0.5f,0.5f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInOutQuart:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.25f,0.032f,0.5f,0.5f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,4f,4f));
				_curve.AddKey(new Keyframe(0.75f,0.968f,0.5f,0.5f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInQuint:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.2f,0f,0.033f,0.033f));
				_curve.AddKey(new Keyframe(0.6f,0.077f,0.65f,0.65f));
				_curve.AddKey(new Keyframe(1f,1f,5f,5f));
				break;
			case AMTween.EaseType.easeOutQuint:
				_curve.AddKey(new Keyframe(0f,0f,5f,5f));
				_curve.AddKey(new Keyframe(0.4f,0.92f,0.65f,0.65f));
				_curve.AddKey(new Keyframe(0.8f,1f,0.033f,0.033f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInOutQuint:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.1f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.3f,0.04f,0.65f,0.65f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,5f,5f));
				_curve.AddKey(new Keyframe(0.7f,0.96f,0.65f,0.65f));
				_curve.AddKey(new Keyframe(0.9f,1f,0f,0f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInSine:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.5f,0.292f,1.11f,1.11f));
				_curve.AddKey(new Keyframe(1f,1f,1.56f,1.56f));
				break;
			case AMTween.EaseType.easeOutSine:
				_curve.AddKey(new Keyframe(0f,0f,1.56f,1.56f));
				_curve.AddKey(new Keyframe(0.5f,0.708f,1.11f,1.11f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInOutSine:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.25f,0.145f,1.1f,1.1f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,1.6f,1.6f));
				_curve.AddKey(new Keyframe(0.75f,0.853f,1.1f,1.1f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInExpo:
				_curve.AddKey(new Keyframe(0f,0f,0.031f,0.031f));
				_curve.AddKey(new Keyframe(0.5f,0.031f,0.214f,0.214f));
				_curve.AddKey(new Keyframe(0.8f,0.249f,1.682f,1.682f));
				_curve.AddKey(new Keyframe(1f,1f,6.8f,6.8f));
				break;
			case AMTween.EaseType.easeOutExpo:
				_curve.AddKey(new Keyframe(0f,0f,6.8f,6.8f));
				_curve.AddKey(new Keyframe(0.2f,0.751f,1.682f,1.682f));
				_curve.AddKey(new Keyframe(0.5f,0.969f,0.214f,0.214f));
				_curve.AddKey(new Keyframe(1f,1f,0.031f,0.031f));
				break;
			case AMTween.EaseType.easeInOutExpo:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.25f,0.015f,0.181f,0.181f));
				_curve.AddKey(new Keyframe(0.4f,0.125f,1.58f,1.58f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,6.8f,6.8f));
				_curve.AddKey(new Keyframe(0.6f,0.873f,1.682f,1.682f));
				_curve.AddKey(new Keyframe(0.75f,0.982f,0.21f,0.21f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInCirc:
				_curve.AddKey(new Keyframe(0f,0f,0.04f,0.04f));
				_curve.AddKey(new Keyframe(0.6f,0.2f,0.76f,0.76f));
				_curve.AddKey(new Keyframe(0.9f,0.562f,1.92f,1.92f));
				_curve.AddKey(new Keyframe(0.975f,0.78f,4.2f,4.2f));
				_curve.AddKey(new Keyframe(1f,1f,17.3f,17.3f));
				break;
			case AMTween.EaseType.easeOutCirc:
				_curve.AddKey(new Keyframe(0f,0f,17.3f,17.3f));
				_curve.AddKey(new Keyframe(0.025f,0.22f,4.2f,4.2f));
				_curve.AddKey(new Keyframe(0.1f,0.438f,1.92f,1.92f));
				_curve.AddKey(new Keyframe(0.4f,0.8f,0.76f,0.76f));
				_curve.AddKey(new Keyframe(1f,1f,0.04f,0.04f));
				break;
			case AMTween.EaseType.easeInOutCirc:
				_curve.AddKey(new Keyframe(0f,0f,0f,0f));
				_curve.AddKey(new Keyframe(0.3f,0.098f,0.75f,0.75f));
				_curve.AddKey(new Keyframe(0.45f,0.281f,1.96f,1.96f));
				_curve.AddKey(new Keyframe(0.4875f,0.392f,4.4f,4.4f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,8.14f,8.14f));
				_curve.AddKey(new Keyframe(0.5125f,0.607f,4.3f,4.3f));
				_curve.AddKey(new Keyframe(0.55f,0.717f,1.94f,1.94f));
				_curve.AddKey(new Keyframe(0.7f,0.899f,0.74f,0.74f));
				_curve.AddKey(new Keyframe(1f,1f,0f,0f));
				break;
			case AMTween.EaseType.easeInBounce:
				_curve.AddKey(new Keyframe(0f,0f,0.715f,0.715f));
				_curve.AddKey(new Keyframe(0.091f,0f,-0.677f,1.365f));
				_curve.AddKey(new Keyframe(0.272f,0f,-1.453f,2.716f));
				_curve.AddKey(new Keyframe(0.636f,0f,-2.775f,5.517f));
				_curve.AddKey(new Keyframe(1f,1f,-0.0023f,-0.0023f));
				break;
			case AMTween.EaseType.easeOutBounce:
				_curve.AddKey(new Keyframe(0f,0f,-0.042f,-0.042f));
				_curve.AddKey(new Keyframe(0.364f,1f,5.414f,-2.758f));
				_curve.AddKey(new Keyframe(0.727f,1f,2.773f,-1.295f));
				_curve.AddKey(new Keyframe(0.909f,1f,1.435f,-0.675f));
				_curve.AddKey(new Keyframe(1f,1f,0.735f,0.735f));
				break;
			case AMTween.EaseType.easeInOutBounce:
				_curve.AddKey(new Keyframe(0f,0f,0.682f,0.682f));
				_curve.AddKey(new Keyframe(0.046f,0f,-0.732f,1.316f));
				_curve.AddKey(new Keyframe(0.136f,0f,-1.568f,2.608f));
				_curve.AddKey(new Keyframe(0.317f,0f,-2.908f,5.346f));
				_curve.AddKey(new Keyframe(0.5f,0.5f,-0.061f,0.007f));
				_curve.AddKey(new Keyframe(0.682f,1f,5.463f,-2.861f));
				_curve.AddKey(new Keyframe(0.864f,1f,2.633f,-1.258f));
				_curve.AddKey(new Keyframe(0.955f,1f,1.488f,-0.634f));
				_curve.AddKey(new Keyframe(1f,1f,0.804f,0.804f));
				break;
			case AMTween.EaseType.easeInBack:
				_curve.AddKey(new Keyframe(0.00f,0.00f,0.00f,0.00f));
				_curve.AddKey(new Keyframe(1.00f,1.00f,4.71f,4.71f));
				break;
			case AMTween.EaseType.easeOutBack:
				_curve.AddKey(new Keyframe(0.00f,0.00f,4.71f,4.71f));
				_curve.AddKey(new Keyframe(1.00f,1.00f,0.00f,0.00f));
				break;
			case AMTween.EaseType.easeInOutBack:
				_curve.AddKey(new Keyframe(0.00f,0.00f,0.00f,0.00f));
				_curve.AddKey(new Keyframe(0.50f,0.50f,5.61f,5.61f));
				_curve.AddKey(new Keyframe(1.00f,1.00f,0.00f,0.00f));
				break;
			case AMTween.EaseType.easeInElastic:
				_curve.AddKey(new Keyframe(0.00f,0.00f,0.00f,0.00f));
				_curve.AddKey(new Keyframe(0.15f,0.00f,-0.04f,-0.04f));
				_curve.AddKey(new Keyframe(0.30f,-0.005f,0.04f,0.04f));
				_curve.AddKey(new Keyframe(0.42f,0.02f,-0.07f,-0.07f));
				_curve.AddKey(new Keyframe(0.58f,-0.04f,0.15f,0.15f));
				_curve.AddKey(new Keyframe(0.72f,0.13f,0.20f,0.20f));
				_curve.AddKey(new Keyframe(0.80f,-0.13f,-5.33f,-5.33f));
				_curve.AddKey(new Keyframe(0.868f,-0.375f,0.14f,0.14f));
				_curve.AddKey(new Keyframe(0.92f,-0.05f,11.32f,11.32f));
				_curve.AddKey(new Keyframe(1.00f,1.00f,7.50f,7.50f));
				break;
			case AMTween.EaseType.easeOutElastic:
				_curve.AddKey(new Keyframe(0.000f,0.00f,6.56f,6.56f));
				_curve.AddKey(new Keyframe(0.079f,1.06f,11.22f,11.22f));
				_curve.AddKey(new Keyframe(0.134f,1.38f,0.03f,0.03f));
				_curve.AddKey(new Keyframe(0.204f,1.10f,-5.24f,-5.24f));
				_curve.AddKey(new Keyframe(0.289f,0.87f,0.65f,0.65f));
				_curve.AddKey(new Keyframe(0.424f,1.05f,0.13f,0.13f));
				_curve.AddKey(new Keyframe(0.589f,0.98f,0.12f,0.12f));
				_curve.AddKey(new Keyframe(0.696f,1.00f,0.07f,0.07f));
				_curve.AddKey(new Keyframe(0.898f,1.00f,0.00f,0.00f));
				_curve.AddKey(new Keyframe(1.000f,1.00f,0.00f,0.00f));
				break;
		case AMTween.EaseType.easeInOutElastic:
				_curve.AddKey(new Keyframe(0.000f,0.00f,0.00f,0.00f));
				_curve.AddKey(new Keyframe(0.093f,0.00f,-0.05f,-0.05f));
				_curve.AddKey(new Keyframe(0.149f,0.00f,0.06f,0.06f));
				_curve.AddKey(new Keyframe(0.210f,0.01f,-0.04f,-0.04f));
				_curve.AddKey(new Keyframe(0.295f,-0.02f,0.31f,0.31f));
				_curve.AddKey(new Keyframe(0.356f,0.07f,0.11f,0.11f));
				_curve.AddKey(new Keyframe(0.400f,-0.06f,-5.12f,-5.12f));
				_curve.AddKey(new Keyframe(0.435f,-0.19f,0.18f,0.18f));
				_curve.AddKey(new Keyframe(0.463f,0.02f,12.44f,12.44f));
				_curve.AddKey(new Keyframe(0.500f,0.50f,8.33f,8.33f));
				_curve.AddKey(new Keyframe(0.540f,1.03f,12.05f,12.05f));
				_curve.AddKey(new Keyframe(0.568f,1.18f,0.31f,0.31f));
				_curve.AddKey(new Keyframe(0.604f,1.04f,-5.03f,-5.03f));
				_curve.AddKey(new Keyframe(0.645f,0.93f,0.36f,0.36f));
				_curve.AddKey(new Keyframe(0.705f,1.02f,0.39f,0.39f));
				_curve.AddKey(new Keyframe(0.786f,0.99f,-0.04f,-0.04f));
				_curve.AddKey(new Keyframe(0.848f,1.00f,0.04f,0.04f));
				_curve.AddKey(new Keyframe(0.900f,1.00f,-0.01f,-0.01f));
				_curve.AddKey(new Keyframe(1.000f,1.00f,0.00f,0.00f));
			break;
		case AMTween.EaseType.spring:
				_curve.AddKey(new Keyframe(0.000f,0.00f,3.51f,3.51f));
				_curve.AddKey(new Keyframe(0.367f,0.88f,1.79f,1.79f));
				_curve.AddKey(new Keyframe(0.615f,1.08f,-0.28f,-0.28f));
				_curve.AddKey(new Keyframe(0.795f,0.97f,0.03f,0.03f));
				_curve.AddKey(new Keyframe(0.901f,1.01f,0.20f,0.20f));
				_curve.AddKey(new Keyframe(1.000f,1.00f,0.00f,0.00f));
			break;
		}
		return _curve;
	}
}
