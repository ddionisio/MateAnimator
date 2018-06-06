using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator.Edit {
    [CustomEditor(typeof(Animate))]
    public class AnimateInspector : Editor {
        private enum MetaCommand {
            None,
            SaveAs,
            Instantiate
        }
        private string[] mTakeLabels;
        private bool mMissingsFoldout = true;

        private AnimateEditControl aData;

        void OnEnable() {
            aData = new AnimateEditControl(target as Animate);
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
                mTakeLabels[i + 1] = aData.takes[i].name;
        }

        public override void OnInspectorGUI() {
            Animate anim = target as Animate;

            GUILayout.BeginVertical();

            //meta
            AnimateMeta curMeta = aData.meta;
            AnimateMeta newMeta = EditorGUILayout.ObjectField(new GUIContent("Meta", "Use data from the reference AnimateMeta. Note: All modifications to the animation will be saved to the Meta."), curMeta, typeof(AnimateMeta), false) as AnimateMeta;

            if(curMeta != newMeta) {
                bool doIt = true;
                if(curMeta == null) {
                    doIt = UnityEditor.EditorUtility.DisplayDialog("Set Meta", "Setting the Meta will replace the current animation data, proceed?", "Yes", "No");
                }

                if(doIt) {
                    aData.MetaSet(newMeta, false);

                    curMeta = aData.meta;
                }
            }

            MetaCommand metaComm = MetaCommand.None;
                        
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            //Save As
            GUI.backgroundColor = Color.green;
            
            if(GUILayout.Button("Save As...", GUILayout.Width(100f))) metaComm = MetaCommand.SaveAs;
            //

            //Break
            GUI.enabled = curMeta != null;

            GUI.backgroundColor = Color.white;
                        
            if(GUILayout.Button(new GUIContent("Break", "This will copy all data from AnimateMeta to this Animate, and then removes the reference to AnimateMeta."), GUILayout.Width(100f))) metaComm = MetaCommand.Instantiate;

            GUI.enabled = true;
            //

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
                        
            EditorUtility.DrawSeparator();
            //

            List<Take> takes = aData.takes;
            string playTakeName = aData.defaultTakeName;
            int playTakeInd = 0;
            if(!string.IsNullOrEmpty(playTakeName)) {
                for(int i = 0; i < takes.Count; i++) {
                    if(takes[i].name == aData.defaultTakeName) {
                        playTakeInd = i + 1;
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
                    Animate[] anims = FindObjectsOfType<Animate>();
                    for(int i = 0; i < anims.Length; i++) {
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

            var onDisableAction = (Animate.DisableAction)EditorGUILayout.EnumPopup("On Disable", anim.onDisableAction);
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
                TimelineWindow timeline = TimelineWindow.window;

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
                            EditorWindow.GetWindow(typeof(TimelineWindow));
                        }
                    }
                }
            }

            //display missings
            string[] missings = aData.target.GetMissingTargets();
            if(missings != null && missings.Length > 0) {
                EditorUtility.DrawSeparator();
                mMissingsFoldout = EditorGUILayout.Foldout(mMissingsFoldout, string.Format("Missing Targets [{0}]", missings.Length));
                if(mMissingsFoldout) {
                    for(int i = 0; i < missings.Length; i++)
                        GUILayout.Label(missings[i]);
                }

                //fix missing targets
                if(GUILayout.Button("Generate Missing Targets"))
                    aData.target.GenerateMissingTargets(missings);
            }

            GUILayout.EndVertical();

            switch(metaComm) {
                case MetaCommand.SaveAs:
                    string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save AnimateMeta", aData.name, "asset", "Please enter a file name to save the animator data to");
                    if(!string.IsNullOrEmpty(path)) {
                        bool canCreate = true;

                        //check if path is the same as current
                        if(aData.meta) {
                            string curPath = AssetDatabase.GetAssetPath(aData.meta);
                            if(path == curPath)
                                canCreate = false; //don't need to save this since it's already being used.
                        }
                                                
                        if(canCreate) {
                            //check if it already exists
                            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) {
                                //load the meta and overwrite its data
                                var loadedMeta = AssetDatabase.LoadAssetAtPath<AnimateMeta>(path);
                                aData.MetaSet(loadedMeta, true);
                            }                            
                            else {
                                //create new meta
                                var createdMeta = ScriptableObject.CreateInstance<AnimateMeta>();
                                AssetDatabase.CreateAsset(createdMeta, path);
                                AssetDatabase.SaveAssets();

                                aData.MetaSet(createdMeta, true);
                            }
                        }
                    }
                    break;
                case MetaCommand.Instantiate:
                    //warning if there are missing targets
                    bool doIt = true;
                    if(missings != null && missings.Length > 0)
                        doIt = UnityEditor.EditorUtility.DisplayDialog("Break Animator Meta", "There are missing targets, some keys will be removed. Do you want to proceed?", "Yes", "No");
                    if(doIt) {
                        aData.MetaSet(null, true);
                        aData.currentTakeInd = 0;
                        GUI.changed = true;
                    }
                    break;
            }

            if(GUI.changed) {
                if(TimelineWindow.window) {
                    if(metaComm != MetaCommand.None)
                        TimelineWindow.window.Reload();
                    else
                        TimelineWindow.window.Repaint();
                }
            }
        }
    }
}
