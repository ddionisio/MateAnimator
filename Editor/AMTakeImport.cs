using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AMTakeImport : EditorWindow {
	
	public static List<GameObject> newReference = new List<GameObject>();
	public static List<GameObject> oldReference = new List<GameObject>();
	List<int> actions;
	
	private AMOptionsFile oData;
	
	Vector2 scrollPos = new Vector2(0f,0f);
	float width_button_action = 64f;
	float width_object_field = 120f;
	float width_toggle_label = 41f;
	float height_label_offset = 3f;
	
	// skins
	private GUISkin skin = null;
	private string cachedSkinName = null;
	
	private const float height_gameobject = 22f;

	void OnEnable() {
		this.title = "Resolve Duplicates";
		this.minSize = new Vector2(590f,120f);
		actions = new List<int>();	
		for(int i=0;i<newReference.Count;i++) actions.Add(0);
		
		oData = AMOptionsFile.loadFile();
	}
	// Use this for initialization
	void OnGUI() {
		AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
		GUIStyle padding = new GUIStyle();
		padding.padding = new RectOffset(4,4,4,4);
		GUILayout.BeginVertical(padding);
		EditorGUIUtility.LookLikeControls();
		GUILayout.BeginHorizontal();
			GUILayout.Label(newReference.Count+" possible duplicate"+(newReference.Count > 1 ? "s" : "")+" found:");
			GUILayout.FlexibleSpace();
			GUILayout.Label("Keep: ");
			if(GUILayout.Button("All",GUILayout.Width(width_button_action))) {
				for(int i=0;i<actions.Count;i++) actions[i] = 0;	
			}
			if(GUILayout.Button("New",GUILayout.Width(width_button_action))) {
				for(int i=0;i<actions.Count;i++) actions[i] = 1;	
			}
			if(GUILayout.Button("Previous",GUILayout.Width(width_button_action))) {
				for(int i=0;i<actions.Count;i++) actions[i] = 2;	
			}
			GUILayout.Space(18f);
		GUILayout.EndHorizontal();
		GUILayout.Space(4f);
		GUIStyle styleScrollView = new GUIStyle(GUI.skin.scrollView);
		styleScrollView.normal.background = GUI.skin.GetStyle("GroupElementBG").onNormal.background;
		styleScrollView.padding = new RectOffset(0,0,4,4);
		scrollPos = GUILayout.BeginScrollView(scrollPos,false,true,GUI.skin.horizontalScrollbar,GUI.skin.verticalScrollbar,styleScrollView);
		
			int maxGameObjects = Mathf.CeilToInt((position.height - 62f)/height_gameobject);
			maxGameObjects = Mathf.Clamp(maxGameObjects, 0, newReference.Count);
			int FirstIndex = Mathf.FloorToInt(scrollPos.y / height_gameobject)-1;
			FirstIndex = Mathf.Clamp(FirstIndex, 0, newReference.Count);
			int LastIndex = FirstIndex + maxGameObjects;
			LastIndex = Mathf.Clamp(LastIndex, 0, newReference.Count);
			if(LastIndex-FirstIndex < maxGameObjects) FirstIndex = Mathf.Clamp(LastIndex-maxGameObjects,0,newReference.Count);
		
			for(int i=0;i<newReference.Count;i++) {
				if(newReference.Count > maxGameObjects && (i < FirstIndex || i > LastIndex)) {
					GUILayout.Space(height_gameobject);
					continue;
				}
				GUILayout.BeginHorizontal(GUILayout.Height(height_gameobject));
					GUILayout.Label("New: ",GUILayout.Width(40f));	
					GUI.enabled = false;
					EditorGUILayout.ObjectField(newReference[i],typeof(GameObject),true,GUILayout.Width(width_object_field));
					GUI.enabled = true;
					GUILayout.Label("Prev: ",GUILayout.Width(40f));
					// do not allow null values
					GameObject go = (GameObject) EditorGUILayout.ObjectField(oldReference[i],typeof(GameObject),true,GUILayout.Width(width_object_field));
					if(go) oldReference[i] = go;
					GUILayout.FlexibleSpace();
					bool keepBoth = actions[i] == 0;
					keepBoth = GUILayout.Toggle(keepBoth,"");
					GUILayout.BeginVertical();
						GUILayout.Space(height_label_offset);
						GUILayout.Label("Both",GUILayout.Width(width_toggle_label));
					GUILayout.EndVertical();
					bool keepFirst = actions[i] == 1;
					keepFirst = GUILayout.Toggle(keepFirst,"");
					GUILayout.BeginVertical();
						GUILayout.Space(height_label_offset);
						GUILayout.Label("New",GUILayout.Width(width_toggle_label));
					GUILayout.EndVertical();
					bool keepSecond = actions[i] == 2;
					keepSecond = GUILayout.Toggle(keepSecond,"");
					GUILayout.BeginVertical();
						GUILayout.Space(height_label_offset);
						GUILayout.Label("Prev.",GUILayout.Width(width_toggle_label));
					GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				// set action
				if(keepBoth && actions[i] != 0) actions[i] = 0;
				else if(keepFirst && actions[i] != 1) actions[i] = 1;
				else if(keepSecond && actions[i] != 2) actions[i] = 2;
			}
		GUILayout.EndScrollView();
		GUILayout.Space(4f);
		GUILayout.BeginHorizontal();
			if(GUILayout.Button("Apply")) saveChanges();
			if(GUILayout.Button ("Cancel")) this.Close();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
	
	void saveChanges() {
		GameObject go = GameObject.Find ("AnimatorData");
		if(!go) return;
		AnimatorData aData = (AnimatorData) go.GetComponent ("AnimatorData");
		if(!aData) return;
		
		List<GameObject> keepReferences = new List<GameObject>();
		List<GameObject> replaceReferences = new List<GameObject>();
		
		for(int i=0; i<newReference.Count;i++) {
			if(actions[i] == 0) continue;
			if(newReference[i] == oldReference[i]) continue;
			if(!newReference[i] || !oldReference[i]) continue;	// skip null values
			else if(actions[i] == 1) {
				keepReferences.Add(newReference[i]);
				replaceReferences.Add(oldReference[i]);
			} else if(actions[i] == 2) {
				keepReferences.Add(oldReference[i]);
				replaceReferences.Add(newReference[i]);	
			}
		}
		
		if(keepReferences.Count <= 0) {
			this.Close();
			return;	// return if no changes made
		}
		AMTimeline.registerUndo("Resolve Duplicates");
		// update references
		List<GameObject> lsFlagToKeep = aData.updateDependencies(keepReferences, replaceReferences);
		// reset event track method info
		AMTimeline.resetIndexMethodInfo();
		AMTimeline.shouldCheckDependencies = false;
		//aData.shouldCheckDependencies = false;
		// delete replaced references
		int count = 0;
		for(int i=0;i<replaceReferences.Count;i++) {
			if(lsFlagToKeep.Contains(replaceReferences[i])) continue;
			DestroyImmediate(replaceReferences[i]);
			replaceReferences.RemoveAt(i);
			count++;
			i--;
		}
		replaceReferences = new List<GameObject>();
		
		Debug.Log ("Animator: Resolved Duplicate"+(count > 1 ? "s" : "")+". Deleted "+count+" GameObject"+(count > 1 ? "s" : "")+".");
		this.Close();
		
	}
	
	bool setAction(int actionIndex, int toggleAction, bool toggleValue) {
		if(!toggleValue) return toggleValue;
		if(actions[actionIndex] != toggleAction) actions[actionIndex] = toggleAction;
		return toggleValue;
	}
	public static void openAdditiveAndDeDupe(string scenePath)
	{
		List<GameObject> origGOs = ((GameObject[])GameObject.FindObjectsOfType(typeof(GameObject))).ToList();
		EditorApplication.OpenSceneAdditive(scenePath);
		List<GameObject> updGOs = ((GameObject[])GameObject.FindObjectsOfType(typeof(GameObject))).ToList();
		List<GameObject> newGOs = updGOs.Except(origGOs).ToList();
		
		// merge AnimatorData
		List<AnimatorData> origAnimatorData = new List<AnimatorData>();
		List<AnimatorData> newAnimatorData = new List<AnimatorData>();
		// get animator data
		foreach(GameObject go in origGOs) {
			AnimatorData _temp = getAnimatorData(go);
			if(_temp != null) origAnimatorData.Add(_temp);
		}
		foreach(GameObject go in newGOs) {
			AnimatorData __temp = getAnimatorData(go);
			if(__temp != null) newAnimatorData.Add(__temp);
		}
		int numTakes = 0;
		foreach (AnimatorData _a in newAnimatorData) {
			numTakes += _a.takes.Count;	
		}
		int numGOs = (newGOs.Count-newAnimatorData.Count);
		Debug.Log ("Animator: Imported "+numTakes+" Take"+(numTakes > 1 ? "s" : "")+". Added "+numGOs+" GameObject"+(numGOs > 1 ? "s" : "")+".");
		// merge new animator data together
		for(int i=1;i<newAnimatorData.Count;i++) {
			newAnimatorData[0].mergeWith(newAnimatorData[i]);
			newGOs.Remove(newAnimatorData[i].gameObject);
			DestroyImmediate(newAnimatorData[i].gameObject);
			newAnimatorData.RemoveAt(i);
			i--;
		}
		// merge old animator data together
		for(int i=1;i<origAnimatorData.Count;i++) {
			origAnimatorData[0].mergeWith(origAnimatorData[i]);
			origGOs.Remove(origAnimatorData[i].gameObject);
			DestroyImmediate(origAnimatorData[i].gameObject);
			origAnimatorData.RemoveAt(i);
			i--;
		}
		
		// merge old with new
		if(origAnimatorData.Count >= 1 && newAnimatorData.Count >= 1) {
			origAnimatorData[0].mergeWith(newAnimatorData[0]);
			newGOs.Remove(newAnimatorData[0].gameObject);
			DestroyImmediate(newAnimatorData[0].gameObject);
			newAnimatorData.RemoveAt(0);
		}
		// get references for new takes
		newReference = new List<GameObject>();
		oldReference = new List<GameObject>();
		// check for dupes
		foreach(GameObject ngo in newGOs) {
			if(ngo == null) continue;
			foreach(GameObject ogo in origGOs) {
				if(ogo == null) continue;
				if(ogo.name == ngo.name) {
					newReference.Add(ngo);
					oldReference.Add(ogo);
					break;
				}
			}
		}
		// open de-duper
		if(newReference.Count > 0) {
			EditorWindow windowImport = ScriptableObject.CreateInstance<AMTakeImport>();
			windowImport.ShowUtility();
		}
	}
	
	public static AnimatorData getAnimatorData(GameObject go) {
		if(go.name != "AnimatorData") return null;
		AnimatorData __aData = (AnimatorData)go.GetComponent("AnimatorData");
		return __aData;
	}
}
