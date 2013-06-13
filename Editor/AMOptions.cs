using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

public class AMOptions : EditorWindow {
	public static AMOptions window = null;
	
	public AMOptionsFile oData;
	public AnimatorData aData;
	
	string version = "1.53";
	Vector2 scrollView = new Vector2(0f, 0f);
	private int playOnStartIndex = 0;
	private int exportTakeIndex = 0;
	private bool exportAllTakes = false;
	public static int tabIndex = 0;
	
	private enum tabType {
		General = 0,
		QuickAdd = 1,
		ImportExport = 2,
		About = 3
	}
	
	string[] tabNames = new string[]{"General", "Quick Add", "Import / Export", "About"};
	
	// skin names
	private string[] skin_names = new string[]{"Dark", "Classic Blue"};
	private string[] skin_ids = new string[]{"am_skin_dark", "am_skin_blue"};
	private int skinIndex = 0;
	
	// skins
	private GUISkin skin = null;
	private string cachedSkinName = null;
	
	private float width_indent = 5f;
			
	void OnEnable() {
		window = this;
		this.title = "Options";
		this.minSize = new Vector2(545f,365f);
		this.maxSize = new Vector2(1000f,this.minSize.y);
		
		loadAnimatorData();
		oData = AMOptionsFile.loadFile();
		// setup skin popup
		skinIndex = 0;
		for(int i=1;i<skin_ids.Length;i++) {
			if(skin_ids[i] == oData.skin) {
				skinIndex = i;
				break;
			}
		}
		
		if(aData) exportTakeIndex = aData.getTakeIndex(aData.getCurrentTake());
	}
	void OnDisable() {
		window = null;
	}
	void OnHierarchyChange()
	{
		if(!aData) loadAnimatorData();
	}
	void OnGUI() {
		AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
		if(!aData) {
			AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.",AMTimeline.MessageBoxType.Warning);
			return;
		}
		if(!oData) oData = AMOptionsFile.loadFile();
		GUILayout.BeginHorizontal();
		#region tab selection
		//GUI.DrawTexture(new Rect(0f,0f,120f,position.height),GUI.skin.GetStyle("GroupElementBG")/*GUI.skin.GetStyle("GroupElementBG").onNormal.background*/);
		GUIStyle styleTabSelectionBG = new GUIStyle(GUI.skin.GetStyle("GroupElementBG"));
		styleTabSelectionBG.normal.background = EditorStyles.toolbar.normal.background;
		GUILayout.BeginVertical(/*GUI.skin.GetStyle("GroupElementBG")*/styleTabSelectionBG, GUILayout.Width(121f));
			EditorGUIUtility.LookLikeControls();
			GUIStyle styleTabButton = new GUIStyle(EditorStyles.toolbarButton);
			styleTabButton.fontSize = 12;
			styleTabButton.fixedHeight = 30;
			styleTabButton.onNormal.background = styleTabButton.onActive.background;
			styleTabButton.onFocused.background = null;
			styleTabButton.onHover.background = null;
			tabIndex = GUILayout.SelectionGrid(tabIndex, tabNames, 1, styleTabButton);
		GUILayout.EndVertical();
		#endregion
		#region options
		GUILayout.BeginVertical();
			EditorGUIUtility.LookLikeControls();
			
			GUIStyle styleArea = new GUIStyle(GUI.skin.textArea);
			scrollView = GUILayout.BeginScrollView(scrollView,styleArea);
			List<string> takeNames = getTakeNames();
			
			GUIStyle styleTitle = new GUIStyle(GUI.skin.label);
			styleTitle.fontSize = 20;
			styleTitle.fontStyle = FontStyle.Bold;
			
			// tab title
			GUILayout.BeginHorizontal();
				GUILayout.Space(width_indent);
				GUILayout.Label(tabNames[tabIndex], styleTitle);
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			
			#region general
			if(tabIndex == (int)tabType.General) {
				List<string> takeNamesWithNone = new List<string>(takeNames);
				takeNamesWithNone.Insert(0, "None");
				// play on start
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.Space(1f);
						GUILayout.Label ("Play On Start");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(setPlayOnStartIndex(EditorGUILayout.Popup(playOnStartIndex,takeNamesWithNone.ToArray(),GUILayout.Width(200f)))) {
						if(playOnStartIndex == 0) aData.playOnStart = null;
						else aData.playOnStart = aData.getTake(takeNames[playOnStartIndex-1]);
					}
				GUILayout.EndHorizontal();
				// gizmo size
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f),GUILayout.Width (80f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Gizmo size",GUILayout.Width (80f));
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(aData.setGizmoSize(GUILayout.HorizontalSlider(aData.gizmo_size,0f,0.1f,GUILayout.ExpandWidth(true)))) {
						GUIUtility.keyboardControl = 0;
						EditorUtility.SetDirty(aData);
					}
					GUILayout.BeginVertical(GUILayout.Height(26f),GUILayout.Width (75f));
						GUILayout.FlexibleSpace();
						if(aData.setGizmoSize(EditorGUILayout.FloatField(aData.gizmo_size,GUI.skin.textField,GUILayout.Width (75f)))) {
							EditorUtility.SetDirty(aData);
						}
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				// time instead of frame numbers
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Show time instead of frame numbers");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setTimeNumbering(GUILayout.Toggle(oData.time_numbering,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();
				// scrubby zoom cursor
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Scrubby zoom cursor");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setScrubbyZoomCursor(GUILayout.Toggle(oData.scrubby_zoom_cursor,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();
				// scrubby zoom slider
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Scrubby zoom slider");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setScrubbyZoomSlider(GUILayout.Toggle(oData.scrubby_zoom_slider,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();
				// show warning for lost references
				/*GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Show warning for lost references");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setShowWarningForLostReferences(GUILayout.Toggle(oData.showWarningForLostReferences,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();*/
				// ignore minimum window size warning
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Ignore minimum window size warning");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setIgnoreMinimumSizeWarning(GUILayout.Toggle(oData.ignoreMinSize,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();
				// show frames for collapsed tracks
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Show frames for collapsed tracks");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setShowFramesForCollapsedTracks(GUILayout.Toggle(oData.showFramesForCollapsedTracks,""))) {
						// save
						EditorUtility.SetDirty(oData);
					}
				GUILayout.EndHorizontal();
				// disable timeline actions
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Hide Timeline Actions (May increase editor performance)");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.setDisableTimelineActions(GUILayout.Toggle(oData.disableTimelineActions,""))) {
						// save
						EditorUtility.SetDirty(oData);
						AMTimeline.recalculateNumFramesToRender();
					}
				GUILayout.EndHorizontal();
				// disable timeline actions tooltip
				if(oData.disableTimelineActions) GUI.enabled = false;
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.FlexibleSpace();
						GUILayout.Label ("Enable Timeline Actions tooltip");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					if(oData.disableTimelineActions) {
						GUILayout.Toggle(false,"");
					} else {
						if(oData.setDisableTimelineActionsTooltip(!GUILayout.Toggle(!oData.disableTimelineActionsTooltip,""))) {
							// save
							EditorUtility.SetDirty(oData);
						}
					}
				GUILayout.EndHorizontal();
				GUI.enabled = true;
				// skin
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical(GUILayout.Height(26f));
						GUILayout.Space(1f);
						GUILayout.Label ("Skin");
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					skinIndex = EditorGUILayout.Popup(skinIndex,skin_names,GUILayout.Width(200f));
					if(oData.setSkin(skin_ids[skinIndex])) {
						//if(playOnStartIndex == 0) aData.playOnStart = null;
						//else aData.playOnStart = aData.getTake(takeNames[playOnStartIndex-1]);
					}
				GUILayout.EndHorizontal();
			}
			#endregion
			#region quick add
			else if(tabIndex == (int)tabType.QuickAdd) {
					EditorGUIUtility.LookLikeControls();
					GUILayout.Space(3f);
					GUILayout.BeginHorizontal();
						GUILayout.Space(width_indent);
						GUILayout.Label("Combinations");
					GUILayout.EndHorizontal();
					if(oData.quickAdd_Combos == null) oData.quickAdd_Combos = new List<List<int>>();
					for(int j=0;j<oData.quickAdd_Combos.Count;j++) {
						GUILayout.Space(3f);
						GUILayout.BeginHorizontal();
							GUILayout.Space(width_indent);
							for(int i=0; i<oData.quickAdd_Combos[j].Count; i++) {
								if(oData.setQuickAddCombo(j,i,EditorGUILayout.Popup(oData.quickAdd_Combos[j][i],AMTimeline.TrackNames,GUILayout.Width(80f)))) {
									oData.flatten_quickAdd_Combos();
									EditorUtility.SetDirty(oData);
								}
								if(i<oData.quickAdd_Combos[j].Count -1) GUILayout.Label("+");
							}
							GUILayout.FlexibleSpace();
							if(oData.quickAdd_Combos[j].Count > 0) 
								if(GUILayout.Button("-", GUILayout.Width(20f), GUILayout.Height(20f))) {
									oData.quickAdd_Combos[j].RemoveAt(oData.quickAdd_Combos[j].Count-1);
									if(oData.quickAdd_Combos[j].Count == 0) {
										oData.quickAdd_Combos.RemoveAt(j);
										j--;
									}
									oData.flatten_quickAdd_Combos();
									EditorUtility.SetDirty(oData);
								}
							if(GUILayout.Button("+", GUILayout.Width(20f), GUILayout.Height(20f))) {
								oData.quickAdd_Combos[j].Add ((int)AMTimeline.Track.Translation);
								oData.flatten_quickAdd_Combos();
								EditorUtility.SetDirty(oData);
							}
						GUILayout.EndHorizontal();
					}
					GUILayout.Space(3f);
					GUILayout.BeginHorizontal();
						if(oData.quickAdd_Combos.Count <= 0) {
							GUILayout.Space(width_indent);
							GUILayout.Label("Click '+' to add a new combination");
						}
						GUILayout.FlexibleSpace();
						// new combo
						if(GUILayout.Button("+", GUILayout.Width(20f), GUILayout.Height(20f))) {
							oData.quickAdd_Combos.Add(new List<int> {(int)AMTimeline.Track.Translation});
							oData.flatten_quickAdd_Combos();
							EditorUtility.SetDirty(oData);
						}
					GUILayout.EndHorizontal();
			}
			#endregion
			#region import / export
			else if(tabIndex == (int)tabType.ImportExport) {
				GUIStyle labelRight = new GUIStyle(GUI.skin.label);
				labelRight.alignment = TextAnchor.MiddleRight;
				GUILayout.Space(10f);
				GUILayout.BeginHorizontal(GUILayout.Width(300f));
					GUILayout.Space(width_indent);
					GUILayout.BeginVertical();
					GUILayout.Space(1f);
					GUILayout.Label("Take(s):",labelRight,GUILayout.Width(55f));
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					GUILayout.Space(1f);
					if(GUILayout.Button("Import",GUILayout.Width(60f))) {
						AMTimeline.registerUndo("Import Take(s)");
						string importTakesPath = EditorUtility.OpenFilePanel("Import Take(s)","Assets/","unity");
						if(importTakesPath != "") AMTakeImport.openAdditiveAndDeDupe(importTakesPath);
					}
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					GUILayout.Space(1f);
					if(GUILayout.Button("Export:",GUILayout.Width(60f))) {
						if(!exportAllTakes) AMTakeExport.take = aData.getTake(takeNames[exportTakeIndex]);
						else AMTakeExport.take = null;
						//AMTakeExport.aData = aData;
						//EditorWindow.GetWindow (typeof (AMTakeExport)).ShowUtility();
						EditorWindow windowExport = ScriptableObject.CreateInstance<AMTakeExport>();
						windowExport.ShowUtility();
					}
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					exportAllTakes = (GUILayout.Toggle(!exportAllTakes, "") ? false : exportAllTakes);
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					GUILayout.Space(4f);
					setExportTakeIndex(EditorGUILayout.Popup(exportTakeIndex,takeNames.ToArray(),GUILayout.Width(100f)));
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					exportAllTakes = (GUILayout.Toggle(exportAllTakes, "") ? true : exportAllTakes);
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					GUILayout.Space(2f);
					GUILayout.Label("All Takes");	
					GUILayout.EndVertical();
					
				GUILayout.EndHorizontal();
				GUILayout.Space(3f);
				GUILayout.BeginHorizontal();
					GUILayout.Space(width_indent);
					GUILayout.Label("Options:",labelRight,GUILayout.Width(55f));
					if(GUILayout.Button("Import",GUILayout.Width(60f))) {
						AMTimeline.registerUndo("Import Options");
						string importOptionsPath = EditorUtility.OpenFilePanel("Import Options","Assets/Animator","unitypackage");
						if(importOptionsPath != "") {
							AssetDatabase.ImportPackage(importOptionsPath,true);
							this.Close();
						}
					}
					if(GUILayout.Button("Export",GUILayout.Width(60f))) {
						AMOptionsFile.export();	
					}
				GUILayout.EndHorizontal();
			}
			#endregion
			#region about
			else if(tabIndex == (int)tabType.About) {
				GUILayout.Space(3f);
				
				string message = "Animator v"+version+", Created by Abdulla Ameen (c) 2012.\nAMTween is derived from Bob Berkebile's iTween which falls under the MIT license.\n\nPlease have a look at the documentation if you need help, or e-mail animatorunity@gmail.com for further assistance.";
				message += "\n\nAdditional code contributions by:\nQuick Fingers, Eric Haines";
				GUIStyle styleInfo = new GUIStyle(GUI.skin.label);
				GUILayout.BeginHorizontal();
					GUILayout.Space(5);
					styleInfo.wordWrap = true;
					GUILayout.Label(message,styleInfo);
				GUILayout.EndHorizontal();
			}
			#endregion
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			#endregion
		GUILayout.EndHorizontal();
	}
	
	List<string> getTakeNames() {
		List<string> takeNames = new List<string>();
		foreach(AMTake take in aData.takes) {
			takeNames.Add(take.name);
		}
		return takeNames;
	}
	bool setPlayOnStartIndex(int index) {
		if(playOnStartIndex != index) {
			playOnStartIndex = index;
			return true;
		}
		return false;
	}
	bool setExportTakeIndex(int index) {
		if(exportTakeIndex != index) {
			exportTakeIndex = index;
			return true;
		}
		return false;
	}

//	bool showFoldout(int index, string text) {
//		oData.checkForFoldout(index);
//		GUIStyle styleLabelBold = new GUIStyle(GUI.skin.label);
//		styleLabelBold.fontStyle = FontStyle.Bold;
//		
//		bool justClicked = false;
//		GUILayout.BeginHorizontal();
//		if(GUILayout.Button(new GUIContent(oData.foldout[index] ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background),"label",GUILayout.Height(19f),GUILayout.Width(19f))) {
//			justClicked = true;	
//		}
//		GUILayout.Label(text,styleLabelBold);
//		if(GUI.Button(GUILayoutUtility.GetLastRect(),"","label")) {
//			justClicked = true;	
//		}
//		GUILayout.EndHorizontal();
//		if(justClicked) oData.foldout[index] = !oData.foldout[index];
//		return oData.foldout[index];
//	}
	public void reloadAnimatorData() {
		aData = null;
		loadAnimatorData();
	}
	void loadAnimatorData()
	{
		GameObject go = GameObject.Find ("AnimatorData");
		if(go) {
			aData = (AnimatorData) go.GetComponent ("AnimatorData");
			if(aData) {
				if(aData.playOnStart != null) playOnStartIndex = aData.getTakeIndex(aData.playOnStart) + 1;
				exportTakeIndex = aData.getTakeIndex(aData.getCurrentTake());
			}
		}
	}
	
	
}
