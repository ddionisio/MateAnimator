using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

namespace MateAnimator{
	public class AMOptions : EditorWindow {
	    public static AMOptions window = null;

	    public AMOptionsFile oData;

	    private AnimatorDataEdit __aData;

	    public AnimatorDataEdit aData {
	        get {
	            if(AMTimeline.window != null && __aData != AMTimeline.window.aData) {
	                reloadAnimatorData();
	            }

	            return __aData;
	        }
	    }

	    string version = "2.1";
	    Vector2 scrollView = new Vector2(0f, 0f);
	    private int exportTakeIndex = 0;
	    private bool exportAllTakes = false;
	    public static int tabIndex = 0;

	    private enum tabType {
	        General = 0,
	        QuickAdd = 1,
	        ImportExport = 2,
	        About = 3
	    }

	    string[] tabNames = new string[] { "General", "Quick Add", "Import / Export", "About" };

	    
	    // skins
	    private GUISkin skin = null;
	    private string cachedSkinName = null;

	    private float width_indent = 5f;

	    void OnEnable() {
	        window = this;
	#if UNITY_5
	        titleContent = new GUIContent("Options");
	#else
	        title = "Options";
	#endif
	        minSize = new Vector2(545f, 365f);
	        maxSize = new Vector2(1000f, this.minSize.y);

	        loadAnimatorData();
	        oData = AMOptionsFile.loadFile();
	        
	        //if(aData) exportTakeIndex = aData.GetTakeIndex(AMTimeline.window.currentTake);
	    }
	    void OnDisable() {
	        window = null;
	    }
	    void OnHierarchyChange() {
	        if(aData == null) loadAnimatorData();
	    }
	    void OnGUI() {
	        AMTimeline.loadSkin(ref skin,ref cachedSkinName, position);
	        if(aData == null) {
	            AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.", AMTimeline.MessageBoxType.Warning);
	            return;
	        }
	        if(!oData) oData = AMOptionsFile.loadFile();
	        GUILayout.BeginHorizontal();
	        #region tab selection
	        //GUI.DrawTexture(new Rect(0f,0f,120f,position.height),GUI.skin.GetStyle("GroupElementBG")/*GUI.skin.GetStyle("GroupElementBG").onNormal.background*/);
	        GUIStyle styleTabSelectionBG = new GUIStyle(GUI.skin.GetStyle("GroupElementBG"));
	        styleTabSelectionBG.normal.background = EditorStyles.toolbar.normal.background;
	        GUILayout.BeginVertical(/*GUI.skin.GetStyle("GroupElementBG")*/styleTabSelectionBG, GUILayout.Width(121f));
            AMEditorUtil.ResetDisplayControls();
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
	        AMEditorUtil.ResetDisplayControls();

	        GUIStyle styleArea = new GUIStyle(GUI.skin.textArea);
	        scrollView = GUILayout.BeginScrollView(scrollView, styleArea);
	        List<string> takeNames = getTakeNames();

	    	GUIStyle styleTitle =  new GUIStyle(GUI.skin.label);
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
	            
	            // gizmo size
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f), GUILayout.Width(80f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Gizmo size", GUILayout.Width(80f));
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();

	            float newGizmoSize = GUILayout.HorizontalSlider(oData.gizmo_size, 0f, 0.1f, GUILayout.ExpandWidth(true));
	            if(oData.gizmo_size != newGizmoSize) {
	                oData.gizmo_size = newGizmoSize;
	                AnimatorTimeline.e_gizmoSize = newGizmoSize;
	                GUIUtility.keyboardControl = 0;
	                EditorUtility.SetDirty(oData);
	            }

	            GUILayout.BeginVertical(GUILayout.Height(26f), GUILayout.Width(75f));
	            GUILayout.FlexibleSpace();
	            newGizmoSize = EditorGUILayout.FloatField(oData.gizmo_size, GUI.skin.textField, GUILayout.Width(75f));
	            if(oData.gizmo_size != newGizmoSize) {
	                oData.gizmo_size = newGizmoSize;
	                AnimatorTimeline.e_gizmoSize = newGizmoSize;
	                EditorUtility.SetDirty(oData);
	            }

	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            GUILayout.EndHorizontal();
	            // pixel/unit default
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            EditorGUIUtility.labelWidth = 250.0f;
	            float ppu = EditorGUILayout.FloatField("Pixel/Unit Default", oData.pixelPerUnitDefault);
	            if(ppu <= 0.001f) ppu = 0.001f;
	            if(oData.pixelPerUnitDefault != ppu) {
	                oData.pixelPerUnitDefault = ppu;
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            GUILayout.Space(4.0f);
	            // sprite drag/drop fps
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            EditorGUIUtility.labelWidth = 250.0f;
	            int nfps = EditorGUILayout.IntField("Sprite Insert Frame/Second", oData.spriteInsertFramePerSecond);
	            if(nfps <= 0) nfps = 1;
	            if(oData.spriteInsertFramePerSecond != nfps) {
	                oData.spriteInsertFramePerSecond = nfps;
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            // time instead of frame numbers
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Show time instead of frame numbers");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setTimeNumbering(GUILayout.Toggle(oData.time_numbering, ""))) {
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            // scrubby zoom cursor
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Scrubby zoom cursor");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setScrubbyZoomCursor(GUILayout.Toggle(oData.scrubby_zoom_cursor, ""))) {
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            // scrubby zoom slider
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Scrubby zoom slider");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setScrubbyZoomSlider(GUILayout.Toggle(oData.scrubby_zoom_slider, ""))) {
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
	            GUILayout.Label("Ignore minimum window size warning");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setIgnoreMinimumSizeWarning(GUILayout.Toggle(oData.ignoreMinSize, ""))) {
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            // show frames for collapsed tracks
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Show frames for collapsed tracks");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setShowFramesForCollapsedTracks(GUILayout.Toggle(oData.showFramesForCollapsedTracks, ""))) {
	                // save
	                EditorUtility.SetDirty(oData);
	            }
	            GUILayout.EndHorizontal();
	            // disable timeline actions
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.BeginVertical(GUILayout.Height(26f));
	            GUILayout.FlexibleSpace();
	            GUILayout.Label("Hide Timeline Actions (May increase editor performance)");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.setDisableTimelineActions(GUILayout.Toggle(oData.disableTimelineActions, ""))) {
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
	            GUILayout.Label("Enable Timeline Actions tooltip");
	            GUILayout.FlexibleSpace();
	            GUILayout.EndVertical();
	            if(oData.disableTimelineActions) {
	                GUILayout.Toggle(false, "");
	            }
	            else {
	                if(oData.setDisableTimelineActionsTooltip(!GUILayout.Toggle(!oData.disableTimelineActionsTooltip, ""))) {
	                    // save
	                    EditorUtility.SetDirty(oData);
	                }
	            }
	            GUILayout.EndHorizontal();
	        }
	        #endregion
	        #region quick add
	        else if(tabIndex == (int)tabType.QuickAdd) {
	            AMEditorUtil.ResetDisplayControls();
	            GUILayout.Space(3f);
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(width_indent);
	            GUILayout.Label("Combinations");
	            GUILayout.EndHorizontal();
	            if(oData.quickAdd_Combos == null) oData.quickAdd_Combos = new List<List<int>>();
	            for(int j = 0; j < oData.quickAdd_Combos.Count; j++) {
	                GUILayout.Space(3f);
	                GUILayout.BeginHorizontal();
	                GUILayout.Space(width_indent);
	                for(int i = 0; i < oData.quickAdd_Combos[j].Count; i++) {
	                    if(oData.setQuickAddCombo(j, i, EditorGUILayout.Popup(oData.quickAdd_Combos[j][i], AMTimeline.TrackNames, GUILayout.Width(80f)))) {
	                        oData.flatten_quickAdd_Combos();
	                        EditorUtility.SetDirty(oData);
	                    }
	                    if(i < oData.quickAdd_Combos[j].Count - 1) GUILayout.Label("+");
	                }
	                GUILayout.FlexibleSpace();
	                if(oData.quickAdd_Combos[j].Count > 0)
	                    if(GUILayout.Button("-", GUILayout.Width(20f), GUILayout.Height(20f))) {
	                        oData.quickAdd_Combos[j].RemoveAt(oData.quickAdd_Combos[j].Count - 1);
	                        if(oData.quickAdd_Combos[j].Count == 0) {
	                            oData.quickAdd_Combos.RemoveAt(j);
	                            j--;
	                        }
	                        oData.flatten_quickAdd_Combos();
	                        EditorUtility.SetDirty(oData);
	                    }
	                if(GUILayout.Button("+", GUILayout.Width(20f), GUILayout.Height(20f))) {
	                    oData.quickAdd_Combos[j].Add((int)AMTimeline.Track.Translation);
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
	                oData.quickAdd_Combos.Add(new List<int> { (int)AMTimeline.Track.Translation });
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
	            GUILayout.Label("Take(s):", labelRight, GUILayout.Width(55f));
	            GUILayout.EndVertical();
	            GUILayout.BeginVertical();
	            GUILayout.Space(1f);
	            if(GUILayout.Button("Import", GUILayout.Width(60f))) {
	                //TODO: undo "Import Take(s)"
	                string importTakesPath = EditorUtility.OpenFilePanel("Import Take(s)", "Assets/", "unity");
	                if(importTakesPath != "") AMTakeImport.openAdditiveAndDeDupe(importTakesPath);
	            }
	            GUILayout.EndVertical();
	            GUILayout.BeginVertical();
	            GUILayout.Space(1f);
	            if(GUILayout.Button("Export:", GUILayout.Width(60f))) {
	                if(!exportAllTakes) AMTakeExport.take = aData.GetTake(takeNames[exportTakeIndex]);
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
	            setExportTakeIndex(EditorGUILayout.Popup(exportTakeIndex, takeNames.ToArray(), GUILayout.Width(100f)));
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
	            GUILayout.Label("Options:", labelRight, GUILayout.Width(55f));
	            if(GUILayout.Button("Import", GUILayout.Width(60f))) {
	                //TODO: undo for "Import Options"
	                string importOptionsPath = EditorUtility.OpenFilePanel("Import Options", "Assets/Animator", "unitypackage");
	                if(importOptionsPath != "") {
	                    AssetDatabase.ImportPackage(importOptionsPath, true);
	                    this.Close();
	                }
	            }
	            if(GUILayout.Button("Export", GUILayout.Width(60f))) {
	                AMOptionsFile.export();
	            }
	            GUILayout.EndHorizontal();
	        }
	        #endregion
	        #region about
	        else if(tabIndex == (int)tabType.About) {
	            GUILayout.Space(3f);

	            string message = "Animator v" + version + ", Originally by Abdulla Ameen (c) 2012.  Modified by David Dionisio under the Creative Commons Attribution-NonCommercial 3.0 Unported License.\n\nPlease have a look at the documentation if you need help, or e-mail ddionisio@renegadeware.com for further assistance.";
	            message += "\n\nHOTween by Daniele Giardini\n\nAdditional code contributions by:\nQuick Fingers, Eric Haines";
	            GUIStyle styleInfo = new GUIStyle(GUI.skin.label);
	            GUILayout.BeginHorizontal();
	            GUILayout.Space(5);
	            styleInfo.wordWrap = true;
	            GUILayout.Label(message, styleInfo);
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
	        foreach(AMTakeData take in aData.takes) {
	            takeNames.Add(take.name);
	        }
	        return takeNames;
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
	        __aData = null;
	        loadAnimatorData();
	    }
	    void loadAnimatorData() {
	        if(AMTimeline.window != null) {
	            __aData = AMTimeline.window.aData;
	            if(__aData != null) {
	                exportTakeIndex = __aData.currentTakeInd;
	            }
	        }
	    }


	}
}