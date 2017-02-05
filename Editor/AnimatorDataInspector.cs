using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
	[CustomEditor(typeof(AnimatorData))]
	public class AnimatorDataInspector : Editor {
	    private enum MetaCommand {
	        None,
	        Save,
	        SaveAs,
	        Revert,
	        Instantiate
	    }
	    private string[] mTakeLabels;
	    private bool mMissingsFoldout = true;

	    private AnimatorDataEdit aData;

	    void OnEnable() {
	        aData = new AnimatorDataEdit(target as AnimatorData);
	        GenerateTakeLabels();

	        mMissingsFoldout = true;
	    }

	    void GenerateTakeLabels() {
	        if(mTakeLabels == null || aData.takes.Count + 1 != mTakeLabels.Length) {
	            mTakeLabels = new string[aData.takes.Count + 1];
	            mTakeLabels[0] = "None";
	        }

	        //match strings
	        for(int i = 0; i < aData.takes.Count; i++)
	            mTakeLabels[i+1] = aData.takes[i].name;
	    }

	    public override void OnInspectorGUI() {
	        AnimatorData anim = target as AnimatorData;

	        GUILayout.BeginVertical();

	        //meta
	        AnimatorMeta curMeta = aData.meta;
	        AnimatorMeta newMeta = EditorGUILayout.ObjectField(new GUIContent("Meta", "Use data from the reference AnimatorMeta. Note: All modifications to the animation will be saved to the Meta."), curMeta, typeof(AnimatorMeta), false) as AnimatorMeta;

	        if(curMeta != newMeta) {
	            bool doIt = true;
	            if(curMeta == null) {
	                doIt = EditorUtility.DisplayDialog("Set Meta", "Setting the Meta will replace the current animation data, proceed?", "Yes", "No");
	            }

	            if(doIt) {
	                aData.RegisterUndo("Set Meta", true);
	                aData.MetaSet(newMeta, false);
	            }
	        }

	        MetaCommand metaComm = MetaCommand.None;
	                
	        GUILayout.BeginHorizontal();
	        GUILayout.FlexibleSpace();

	        GUI.backgroundColor = Color.green;

	        GUI.enabled = PrefabUtility.GetPrefabType(aData.gameObject) != PrefabType.Prefab && aData.metaCanSavePrefabInstance;
	        if(GUILayout.Button("Save", GUILayout.Width(100f))) metaComm = MetaCommand.Save;

	        GUI.enabled = true;
	        if(GUILayout.Button("Save As...", GUILayout.Width(100f))) metaComm = MetaCommand.SaveAs;

	        GUILayout.FlexibleSpace();
	        GUILayout.EndHorizontal();
	                
	        GUILayout.BeginHorizontal();
	        GUILayout.FlexibleSpace();

	        GUI.backgroundColor = Color.red;

	        GUI.enabled = PrefabUtility.GetPrefabType(aData.gameObject) != PrefabType.Prefab && aData.metaCanSavePrefabInstance;
	        if(GUILayout.Button("Revert", GUILayout.Width(100f))) metaComm = MetaCommand.Revert;

	        GUI.backgroundColor = Color.white;

	        GUI.enabled = PrefabUtility.GetPrefabType(aData.gameObject) != PrefabType.Prefab && aData.meta;
	        if(GUILayout.Button(new GUIContent("Break", "This will copy all data from AnimatorMeta to this AnimatorData, and then removes the reference to AnimatorMeta."), GUILayout.Width(100f))) metaComm = MetaCommand.Instantiate;
	        GUI.enabled = true;

	        GUILayout.FlexibleSpace();
	        GUILayout.EndHorizontal();

	        AMEditorUtil.DrawSeparator();
	        //

	        List<AMTakeData> takes = aData.takes;
	        string playTakeName = aData.defaultTakeName;
	        int playTakeInd = 0;
	        if(!string.IsNullOrEmpty(playTakeName)) {
	            for(int i = 0;i < takes.Count;i++) {
	                if(takes[i].name == aData.defaultTakeName) {
	                    playTakeInd = i+1;
	                    break;
	                }
	            }
	        }

	        GenerateTakeLabels();
	        int newPlayTakeInd = EditorGUILayout.IntPopup("Play On Start", playTakeInd, mTakeLabels, null);
	        if(newPlayTakeInd != playTakeInd) {
	            aData.RegisterUndo("Set Play On Start", false);
	            aData.defaultTakeName = newPlayTakeInd <= 0 ? "" : takes[newPlayTakeInd - 1].name;
	        }

	        bool isglobal = GUILayout.Toggle(aData.isGlobal, "Global");
	        if(aData.isGlobal != isglobal) {
	            aData.RegisterUndo("Set Global", false);
	            aData.isGlobal = isglobal;
	            if(isglobal) {
	                //uncheck isGlobal to any other animator data on scene
	                AnimatorData[] anims = FindObjectsOfType<AnimatorData>();
	                for(int i = 0;i < anims.Length;i++) {
	                    if(!aData.IsDataMatch(anims[i]) && anims[i].isGlobal)
	                        anims[i].isGlobal = false;
	                }
	            }
	        }
	        
            var sequenceLoadAll = GUILayout.Toggle(anim.sequenceLoadAll, "Build All Sequence On Start");
            if(anim.sequenceLoadAll != sequenceLoadAll) {
                aData.RegisterUndo("Set Sequence Load All", false);
                anim.sequenceLoadAll = sequenceLoadAll;
            }

            var sequenceKillWhenDone = GUILayout.Toggle(anim.sequenceKillWhenDone, "Kill Sequence When Done");
            if(anim.sequenceKillWhenDone != sequenceKillWhenDone) {
                aData.RegisterUndo("Set Sequence Kill All When Done", false);
                anim.sequenceKillWhenDone = sequenceKillWhenDone;
            }

            var playOnEnable = GUILayout.Toggle(anim.playOnEnable, "Play On Enable");
            if(anim.playOnEnable != playOnEnable) {
                aData.RegisterUndo("Set Play On Enable", false);
                anim.playOnEnable = playOnEnable;
            }

            var onDisableAction = (AnimatorData.DisableAction)EditorGUILayout.EnumPopup("On Disable", anim.onDisableAction);
            if(anim.onDisableAction != onDisableAction) {
                aData.RegisterUndo("Set On Disable Action", false);
                anim.onDisableAction = onDisableAction;
            }

            var updateType = (DG.Tweening.UpdateType)EditorGUILayout.EnumPopup("Update", anim.updateType);
            if(anim.updateType != updateType) {
                aData.RegisterUndo("Set Update Type", false);
                anim.updateType = updateType;
            }

            var updateTimeIndependent = EditorGUILayout.Toggle("Time Independent", anim.updateTimeIndependent);
            if(anim.updateTimeIndependent != updateTimeIndependent) {
                aData.RegisterUndo("Set Time Independent", false);
                anim.updateTimeIndependent = updateTimeIndependent;
            }

	        if(PrefabUtility.GetPrefabType(aData.gameObject) != PrefabType.Prefab) {
	            AMTimeline timeline = AMTimeline.window;

	            if(timeline != null && aData == timeline.aData) {
	                if(GUILayout.Button("Deselect")) {
	                    timeline.aData = null;
	                    timeline.Repaint();

	                    if(Selection.activeGameObject == aData.gameObject) {
	                        Selection.activeGameObject = null;
	                    }
	                }
	            }
	            else {
	                if(GUILayout.Button("Edit Timeline")) {
	                    if(timeline != null) {
	                        timeline.aData = aData;
	                        timeline.Repaint();
	                    }
	                    else {
	                        EditorWindow.GetWindow(typeof(AMTimeline));
	                    }
	                }
	            }
	        }

	        //display missings
	        string[] missings = aData.target.GetMissingTargets();
	        if(missings != null && missings.Length > 0) {
	            AMEditorUtil.DrawSeparator();
	            mMissingsFoldout = EditorGUILayout.Foldout(mMissingsFoldout, string.Format("Missing Targets [{0}]", missings.Length));
	            if(mMissingsFoldout) {
	                for(int i = 0;i < missings.Length;i++)
	                    GUILayout.Label(missings[i]);
	            }

	            //fix missing targets
	            if(GUILayout.Button("Generate Missing Targets"))
	                aData.target.GenerateMissingTargets(missings);
	        }

	        GUILayout.EndVertical();

	        switch(metaComm) {
	            case MetaCommand.Save:
	                aData.MetaSaveInstantiate();
	                GUI.changed = true;
	                break;
	            case MetaCommand.SaveAs:
	                string path = EditorUtility.SaveFilePanelInProject("Save AnimatorMeta", aData.name + ".prefab", "prefab", "Please enter a file name to save the animator data to");
	                if(!string.IsNullOrEmpty(path)) {
	                    GameObject metago = new GameObject("_meta");
	                    metago.AddComponent<AnimatorMeta>();
	                    UnityEngine.Object pref = PrefabUtility.CreateEmptyPrefab(path);
	                    GameObject metagopref = PrefabUtility.ReplacePrefab(metago, pref);
	                    UnityEngine.Object.DestroyImmediate(metago);
	                    aData.MetaSet(metagopref.GetComponent<AnimatorMeta>(), true);
	                }
	                break;
	            case MetaCommand.Revert:
	                if(EditorUtility.DisplayDialog("Revert Animator Meta", "Are you sure?", "Yes", "No")) {
	                    aData.RegisterUndo("Revert Animator Meta", true);
	                    GameObject prefabGO = PrefabUtility.GetPrefabParent(aData.meta.gameObject) as GameObject;
	                    aData.MetaSet(prefabGO ? prefabGO.GetComponent<AnimatorMeta>() : null, false);
	                    GUI.changed = true;
	                }
	                break;
	            case MetaCommand.Instantiate:
	                //warning if there are missing targets
	                bool doIt = true;
	                if(missings != null && missings.Length > 0)
	                    doIt = EditorUtility.DisplayDialog("Break Animator Meta", "There are missing targets, some keys will be removed. Do you want to proceed?", "Yes", "No");
	                if(doIt) {
	                    aData.RegisterUndo("Break Animator Meta", false);
	                    aData.MetaSet(null, true);
	                    aData.currentTakeInd = 0;
	                    GUI.changed = true;
	                }
	                break;
	        }

	        if(GUI.changed) {
	            if(AMTimeline.window)
	                AMTimeline.window.Repaint();
	        }
	    }
	}
}
