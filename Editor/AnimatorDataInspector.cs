using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

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

    private AnimatorDataEdit aData {
        get {
            return AMTimeline.AnimEdit(target as AnimatorData);
        }
    }

    void OnEnable() {
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
        AnimatorDataEdit dat = aData;
        AnimatorData anim = target as AnimatorData;

        GUILayout.BeginVertical();

        //meta
        AnimatorMeta curMeta = dat.meta;
        AnimatorMeta newMeta = EditorGUILayout.ObjectField(new GUIContent("Meta", "Use data from the reference AnimatorMeta. Note: All modifications to the animation will be saved to the Meta."), curMeta, typeof(AnimatorMeta), false) as AnimatorMeta;

        if(curMeta != newMeta) {
            bool doIt = true;
            if(curMeta == null) {
                doIt = EditorUtility.DisplayDialog("Set Meta", "Setting the Meta will replace the current animation data, proceed?", "Yes", "No");
            }

            if(doIt) {
                dat.RegisterUndo("Set Meta", true);
                dat.MetaSet(newMeta, false);
            }
        }

        MetaCommand metaComm = MetaCommand.None;
                
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = Color.green;

        GUI.enabled = PrefabUtility.GetPrefabType(dat.gameObject) != PrefabType.Prefab && dat.metaCanSavePrefabInstance;
        if(GUILayout.Button("Save", GUILayout.Width(100f))) metaComm = MetaCommand.Save;

        GUI.enabled = true;
        if(GUILayout.Button("Save As...", GUILayout.Width(100f))) metaComm = MetaCommand.SaveAs;

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
                
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = Color.red;

        GUI.enabled = PrefabUtility.GetPrefabType(dat.gameObject) != PrefabType.Prefab && dat.metaCanSavePrefabInstance;
        if(GUILayout.Button("Revert", GUILayout.Width(100f))) metaComm = MetaCommand.Revert;

        GUI.backgroundColor = Color.white;

        GUI.enabled = PrefabUtility.GetPrefabType(dat.gameObject) != PrefabType.Prefab && dat.meta;
        if(GUILayout.Button(new GUIContent("Break", "This will copy all data from AnimatorMeta to this AnimatorData, and then removes the reference to AnimatorMeta."), GUILayout.Width(100f))) metaComm = MetaCommand.Instantiate;
        GUI.enabled = true;

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        AMEditorUtil.DrawSeparator();
        //

        List<AMTakeData> takes = dat.takes;
        string playTakeName = dat.defaultTakeName;
        int playTakeInd = 0;
        if(!string.IsNullOrEmpty(playTakeName)) {
            for(int i = 0;i < takes.Count;i++) {
                if(takes[i].name == dat.defaultTakeName) {
                    playTakeInd = i+1;
                    break;
                }
            }
        }

        GenerateTakeLabels();
        int newPlayTakeInd = EditorGUILayout.IntPopup("Play On Start", playTakeInd, mTakeLabels, null);
        if(newPlayTakeInd != playTakeInd) {
            dat.RegisterUndo("Set Play On Start", false);
            dat.defaultTakeName = newPlayTakeInd <= 0 ? "" : takes[newPlayTakeInd - 1].name;
        }

        bool isglobal = GUILayout.Toggle(dat.isGlobal, "Global");
        if(dat.isGlobal != isglobal) {
            dat.RegisterUndo("Set Global", false);
            dat.isGlobal = isglobal;
            if(isglobal) {
                //uncheck isGlobal to any other animator data on scene
                AnimatorData[] anims = FindObjectsOfType<AnimatorData>();
                for(int i = 0;i < anims.Length;i++) {
                    if(!dat.IsDataMatch(anims[i]) && anims[i].isGlobal) {
                        anims[i].isGlobal = false;
                        EditorUtility.SetDirty(anims[i]);
                    }
                }
            }
        }
                
        anim.sequenceLoadAll = GUILayout.Toggle(anim.sequenceLoadAll, "Build All Sequence On Start");
        anim.sequenceKillWhenDone = GUILayout.Toggle(anim.sequenceKillWhenDone, "Kill Sequence When Done");
        anim.playOnEnable = GUILayout.Toggle(anim.playOnEnable, "Play On Enable");
        anim.onDisableAction = (AnimatorData.DisableAction)EditorGUILayout.EnumPopup("On Disable", anim.onDisableAction);
        anim.updateType = (Holoville.HOTween.UpdateType)EditorGUILayout.EnumPopup("Update", anim.updateType);

        if(PrefabUtility.GetPrefabType(dat.gameObject) != PrefabType.Prefab) {
            AMTimeline timeline = AMTimeline.window;

            if(timeline != null && dat == timeline.aData) {
                if(GUILayout.Button("Deselect")) {
                    timeline.aData = null;
                    timeline.Repaint();

                    if(Selection.activeGameObject == dat.gameObject) {
                        Selection.activeGameObject = null;
                    }
                }
            }
            else {
                if(GUILayout.Button("Edit Timeline")) {
                    if(timeline != null) {
                        timeline.aData = dat;
                        timeline.Repaint();
                    }
                    else {
                        EditorWindow.GetWindow(typeof(AMTimeline));
                    }
                }
            }
        }

        //display missings
        string[] missings = dat.target.GetMissingTargets();
        if(missings != null && missings.Length > 0) {
            AMEditorUtil.DrawSeparator();
            mMissingsFoldout = EditorGUILayout.Foldout(mMissingsFoldout, string.Format("Missing Targets [{0}]", missings.Length));
            if(mMissingsFoldout) {
                for(int i = 0;i < missings.Length;i++)
                    GUILayout.Label(missings[i]);
            }

            //fix missing targets
            if(GUILayout.Button("Generate Missing Targets"))
                dat.target.GenerateMissingTargets(missings);
        }

        GUILayout.EndVertical();

        switch(metaComm) {
            case MetaCommand.Save:
                dat.MetaSaveInstantiate();
                GUI.changed = true;
                break;
            case MetaCommand.SaveAs:
                string path = EditorUtility.SaveFilePanelInProject("Save AnimatorMeta", dat.name + ".prefab", "prefab", "Please enter a file name to save the animator data to");
                if(!string.IsNullOrEmpty(path)) {
                    GameObject metago = new GameObject("_meta");
                    metago.AddComponent<AnimatorMeta>();
                    UnityEngine.Object pref = PrefabUtility.CreateEmptyPrefab(path);
                    GameObject metagopref = PrefabUtility.ReplacePrefab(metago, pref);
                    UnityEngine.Object.DestroyImmediate(metago);
                    dat.MetaSet(metagopref.GetComponent<AnimatorMeta>(), true);
                }
                break;
            case MetaCommand.Revert:
                if(EditorUtility.DisplayDialog("Revert Animator Meta", "Are you sure?", "Yes", "No")) {
                    dat.RegisterUndo("Revert Animator Meta", true);
                    GameObject prefabGO = PrefabUtility.GetPrefabParent(dat.meta.gameObject) as GameObject;
                    dat.MetaSet(prefabGO ? prefabGO.GetComponent<AnimatorMeta>() : null, false);
                    GUI.changed = true;
                }
                break;
            case MetaCommand.Instantiate:
                //warning if there are missing targets
                bool doIt = true;
                if(missings != null && missings.Length > 0)
                    doIt = EditorUtility.DisplayDialog("Break Animator Meta", "There are missing targets, some keys will be removed. Do you want to proceed?", "Yes", "No");
                if(doIt) {
                    dat.RegisterUndo("Break Animator Meta", false);
                    dat.MetaSet(null, true);
                    aData.currentTakeInd = 0;
                    GUI.changed = true;
                }
                break;
        }

        if(GUI.changed) {
            if(AMTimeline.window)
                AMTimeline.window.Repaint();
            aData.SetDirty();
        }
    }
}
