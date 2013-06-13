using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class AMTakeExport : EditorWindow {
	public static AMTakeExport window = null;
	
	List<GameObject> gameObjs;
	List<int> gameObjsDepth;
	List<bool?> gameObjsFoldout;
	List<bool?> gameObjsSelected;
	
	Vector2 scrollPos = new Vector2(0f,0f);
	
	public static AMTake take = null;
	
	private AMOptionsFile oData;
	private AnimatorData aData;
	
	List<GameObject> dependencies;
	
	// skins
	private GUISkin skin = null;
	private string cachedSkinName = null;
	
	private float width_toggle = 17f;
	private float width_indent = 15f;
	private float height_label_offset = 3f;
	
	private bool didLoad = false;
	private const int defaultWaitTime = 3;
	private int waitTime = defaultWaitTime;
	
	private const float height_gameobject = 24f;
	
	void OnEnable() {
		window = this;
		this.title = "Export Take" + (take == null ? "s" : "");
		this.minSize = new Vector2(190f,120f);
		
		oData = AMOptionsFile.loadFile();
	}
	void OnDisable() {
		window = null;	
	}
	void OnHierarchyChange() {
		if(!aData) loadAnimatorData();
		waitTime = defaultWaitTime;
		didLoad = false;
		this.Repaint();
	}
	public void reloadAnimatorData() {
		aData = null;
		loadAnimatorData();
	}
	void loadAnimatorData()
	{
		GameObject go = GameObject.Find ("AnimatorData");
		if(go) {
			aData = (AnimatorData) go.GetComponent ("AnimatorData");
			dependencies = aData.getDependencies(take);
		} else {
			this.Close();	
		}
	}
	bool isDependencyOrChild(GameObject go) {
		foreach(GameObject _go in dependencies) {
			if(_go == go || go.transform.IsChildOf(_go.transform)) return true;	
		}
		return false;
	}
	void Update() {
		if(!didLoad) {
			waitTime --;
			if(waitTime <= 0) {
				didLoad = true;
				loadAnimatorData();
				updateSelectedItems();
				this.Repaint();
			}
		}
	}
	void OnGUI() {
		
		AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
		GUIStyle padding = new GUIStyle();
		padding.padding = new RectOffset(3,3,3,3);
		GUILayout.BeginVertical(padding);
		// show list of items
		showItems();
		
		GUILayout.Space(3f);
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("All",GUILayout.Width(50f))) {
			for(int i=0;i<gameObjsSelected.Count;i++) if(gameObjsSelected[i] == false) gameObjsSelected[i] = true;
		}
		if(GUILayout.Button("None",GUILayout.Width(50f))) {
			for(int i=0;i<gameObjsSelected.Count;i++) if(gameObjsSelected[i] == true) gameObjsSelected[i] = false;
		}
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Export...",GUILayout.Width(80f))) {
			bool shouldExit = false;
			bool performExport = true;
			if(EditorApplication.currentScene == "") {
				if(EditorUtility.DisplayDialog("Save Current Scene", "The current scene must be saved before an export is performed.","Save Current Scene","Cancel")) {
					EditorApplication.SaveScene();
					EditorUtility.DisplayDialog("Performing Export...", "The current scene has been saved. Now performing export.","Continue");
				} else {
					performExport = false;	
				}
			}
			if(performExport) {
				if(saveSelectedItemsToScene()) shouldExit = true;
			}
			if(shouldExit) {
				this.Close();
				GUIUtility.ExitGUI();	
			}
			
		}
		
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
	
	bool saveSelectedItemsToScene()
	{
		string saveScenePath = EditorUtility.SaveFilePanel("Export Take","Assets/",(take != null ? take.name : "All_Takes"),"unity");
		if(saveScenePath == "") return false;
		// delete unselected GameObjects
		foreach(GameObject go in GameObject.FindObjectsOfType(typeof(GameObject))) {
			if(!go) continue;
			if(go.name == "AnimatorData") {
				if(take != null) (go.GetComponent("AnimatorData") as AnimatorData).deleteAllTakesExcept(take);
				continue;	
			}
			int index = gameObjs.IndexOf(go);
			if(index <= -1) continue;
			if(gameObjsSelected[index]!=null && (bool)!gameObjsSelected[index]) DestroyImmediate(go);
		}
		// save with changes
		EditorApplication.SaveScene(saveScenePath,true);
		// restore scene
		EditorApplication.OpenScene(saveScenePath);
		// refresh project directory
		AssetDatabase.Refresh();
		return true;
	}
	void showItems()
	{
		
		GUILayout.Label("Items to Export");
		GUILayout.Space(3f);
		GUIStyle styleScrollView = new GUIStyle(GUI.skin.scrollView);
		styleScrollView.normal.background = GUI.skin.GetStyle("GroupElementBG").onNormal.background;
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos,styleScrollView);
		if(didLoad) {
			if(gameObjs.Where(p => p != null && p.name == "AnimatorData").ToList().Count >= 1) {
				GUILayout.BeginHorizontal(GUILayout.Height(height_gameobject));
					GUI.enabled = false;
					GUILayout.Toggle(true,"",GUILayout.Width(width_toggle));
					GUILayout.Space(15f);
					GUILayout.BeginVertical();
						GUILayout.Space(height_label_offset);
						GUILayout.Label(new GUIContent("AnimatorData ("+(take == null ? "All Takes" : take.name)+")",EditorGUIUtility.ObjectContent(null, typeof(MonoBehaviour)).image));
					GUILayout.EndVertical();
					GUI.enabled = true;
				GUILayout.EndHorizontal();
			}
			bool? isOpen = null;
			int lastDepth = 0;
			List<int> gameObjectsToShow = new List<int>();
			for(int i=0;i<gameObjs.Count;i++) {
				if(gameObjs[i] == null) continue;
				if(gameObjs[i].name == "AnimatorData") continue;
				if(isOpen == false && lastDepth < gameObjsDepth[i]) continue;
				isOpen = gameObjsFoldout[i];
				lastDepth = gameObjsDepth[i];
				gameObjectsToShow.Add(i);
			}
			int maxGameObjects = Mathf.CeilToInt((position.height - 62f)/height_gameobject);
			maxGameObjects = Mathf.Clamp(maxGameObjects, 0, gameObjectsToShow.Count);
			int FirstIndex = Mathf.FloorToInt(scrollPos.y / height_gameobject)-1;
			FirstIndex = Mathf.Clamp(FirstIndex, 0, gameObjectsToShow.Count);
			int LastIndex = FirstIndex + maxGameObjects;
			LastIndex = Mathf.Clamp(LastIndex, 0, gameObjectsToShow.Count);
			if(LastIndex-FirstIndex < maxGameObjects) FirstIndex = Mathf.Clamp(LastIndex-maxGameObjects,0,gameObjectsToShow.Count);
			for(int i=0;i<gameObjectsToShow.Count;i++) {
				if(gameObjectsToShow.Count > maxGameObjects && (i < FirstIndex || i > LastIndex)) GUILayout.Space(height_gameobject);
				else showGameObject(gameObjectsToShow[i],gameObjsDepth[gameObjectsToShow[i]]);	
			}
		} else {
			GUILayout.Label("Loading...");
			GUI.enabled = false;
		}
		
		EditorGUILayout.EndScrollView();
	}
	
	void showGameObject(int i, int level) {
		GUILayout.BeginHorizontal(GUILayout.Height(height_gameobject));
		if(gameObjsSelected[i] == null) {
			GUI.enabled = false;
			GUILayout.Toggle(true,"",GUILayout.Width(width_toggle));			
		} else {
			gameObjsSelected[i] = GUILayout.Toggle((bool)gameObjsSelected[i], "",GUILayout.Width(width_toggle));
			
		}
		// indent
		GUILayout.Space(width_indent*level);
		
		if(gameObjsFoldout[i] != null) {
			GUI.enabled = true;
			// foldout
			GUILayout.BeginVertical();
				GUILayout.Space(height_label_offset-1f);
				if(GUILayout.Button("","label",GUILayout.Width(15f))) gameObjsFoldout[i] = !gameObjsFoldout[i];
				GUI.DrawTexture(GUILayoutUtility.GetLastRect(),((bool)gameObjsFoldout[i] ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
			GUILayout.EndVertical();
			// toggle children
			//if(gameObjsSelected[i] != null) {
			if(gameObjsSelected[i] == null) GUI.enabled = false;
				GUILayout.BeginVertical();
					GUILayout.Space(height_label_offset+1f);
					if(GUILayout.Button("", GUILayout.Width(13f),GUILayout.Height(15f))) {
						if(gameObjsSelected[i] != null) {
							gameObjsSelected[i] = !gameObjsSelected[i];
							setAllChildrenSelection((bool)gameObjsSelected[i], i, gameObjsDepth[i]);
						}
					}
				Rect rectSelectAllTexture = GUILayoutUtility.GetLastRect();
				GUILayout.EndVertical();
				
				rectSelectAllTexture.x += 3f;
				rectSelectAllTexture.y += 4f;
				rectSelectAllTexture.width = 7f;
				rectSelectAllTexture.height = 8f;
				if(!GUI.enabled) GUI.color = new Color(GUI.color.r,GUI.color.g,GUI.color.b,0.25f);
				GUI.DrawTexture(rectSelectAllTexture,AMTimeline.getSkinTextureStyleState("select_all").background);
				GUI.color = Color.white;
			//}
		} else GUILayout.Space(15f);
		
		GUILayout.BeginVertical();
			GUILayout.Space(height_label_offset);
			if(gameObjsFoldout[i] != null) GUILayout.Label(gameObjs[i].name);
			else GUILayout.Label(new GUIContent(gameObjs[i].name,EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image),GUILayout.Height(19f));
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		if(!GUI.enabled) GUI.enabled = true;
		GUILayout.EndHorizontal();
	}

	void setupAllGameObjects() 
	{
		List<GameObject> _gameObjs = ((GameObject[])GameObject.FindObjectsOfType(typeof(GameObject))).ToList();
		_gameObjs = _gameObjs.OrderBy(o=>o.name).ToList();
		
		gameObjs = new List<GameObject>();
		gameObjsDepth = new List<int>();
		gameObjsFoldout = new List<bool?>();
		
		setupGameObject(_gameObjs, -1, 0);
		
	}
	
	void setupGameObject(List<GameObject> _gameObjs, int parentIndex, int depth) {
		foreach(GameObject g in _gameObjs) {
			if(g.transform.parent == (parentIndex < 0 ? null : gameObjs[parentIndex].transform)) {
				if(parentIndex >= 0 && gameObjsFoldout[parentIndex] == null) gameObjsFoldout[parentIndex] = false;
				gameObjs.Add(g);
				gameObjsDepth.Add(depth);
				gameObjsFoldout.Add(null);
				// setup children
				setupGameObject(_gameObjs, gameObjs.Count-1, depth + 1);
			}
		}
	}
	
	void setAllChildrenSelection(bool selected, int index, int depth) {
		if(gameObjs.Count < 1) return;
		for(int i=index+1;i<gameObjs.Count;i++) {
			if(gameObjsDepth[i] <= depth) break;
			if(gameObjsSelected[i] != null) gameObjsSelected[i] = selected;
		}
	}
	void updateSelectedItems()
	{
		// save old list
		List <GameObject> _gameObjs;
		List <bool?> _gameObjsFoldout;
		if(gameObjs != null) {
			_gameObjs = new List<GameObject>(gameObjs);
			_gameObjsFoldout = new List<bool?>(gameObjsFoldout);
		} else { 
			_gameObjs = new List<GameObject>();
			_gameObjsFoldout = new List<bool?>();
		}
		setupAllGameObjects();
		
		List<bool?> _gameObjsSelected = new List<bool?>();
		for(int i=0;i<gameObjs.Count;i++) {
			if(isDependencyOrChild(gameObjs[i])) _gameObjsSelected.Add(null);
			else _gameObjsSelected.Add(false);
		}
		
		if(_gameObjs.Count > 0) {
			// keep old selection
			for(int i=0; i<gameObjs.Count;i++) {
				int index = _gameObjs.IndexOf(gameObjs[i]);
				if(index > -1) {
					_gameObjsSelected[i] = gameObjsSelected[index];
					if(index < _gameObjsFoldout.Count && gameObjsFoldout[i] != null) gameObjsFoldout[i] = _gameObjsFoldout[index];
				} else if(isDependencyOrChild(gameObjs[i])) {
					_gameObjsSelected[i] = null;
				}
			}
		}
		gameObjsSelected = _gameObjsSelected;
	}
}
