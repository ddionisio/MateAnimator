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

        private enum PlayAtMode {
            Frame,
            Time
        }

        private string[] mTakeLabels;
        private bool mMissingsFoldout = true;

        private AnimateEditControl aData;
        private Animate mAnim;

        private int mDebugCurPlayingTakeInd;

        private int mDebugPlayTakeInd;

        private PlayAtMode mDebugPlayAtMode;
        private int mDebugPlayAtFrame;
        private float mDebugPlayAtTime;
        private bool mDebugPlayIsLoop;

        private PlayAtMode mDebugGotoAtMode;
        private int mDebugGotoFrame;
        private float mDebugGotoTime;

        void OnEnable() {
            mAnim = target as Animate;
            aData = new AnimateEditControl(mAnim);
            GenerateTakeLabels();

            mMissingsFoldout = true;

            mDebugCurPlayingTakeInd = -1;
            mDebugPlayTakeInd = 0;

            mDebugPlayAtMode = PlayAtMode.Frame;
            mDebugPlayAtFrame = 0;
            mDebugPlayAtTime = 0f;
            mDebugPlayIsLoop = false;

            mDebugGotoAtMode = PlayAtMode.Frame;
            mDebugGotoFrame = 0;
            mDebugGotoTime = 0f;
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
            
            if(!Application.isPlaying && aData.gameObject.scene.IsValid()) {
                TimelineWindow timeline = TimelineWindow.window;

                if(timeline != null && timeline.aData != null && aData.target == timeline.aData.target) {
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

            if(Application.isPlaying) {
                EditorUtility.DrawSeparator();

                var lastEnabled = GUI.enabled;
                                
                var curPlayingTakeInd = mAnim.currentPlayingTakeIndex;
                                
                if(mDebugCurPlayingTakeInd != curPlayingTakeInd) {
                    mDebugCurPlayingTakeInd = curPlayingTakeInd;
                    Repaint();
                }

                var isCurTakeValid = mDebugCurPlayingTakeInd != -1;

                GUILayout.Label(string.Format("Current Take: {0}", isCurTakeValid ? mAnim.currentPlayingTakeName : "None"));

                mDebugPlayTakeInd = EditorGUILayout.IntPopup(mDebugPlayTakeInd, mTakeLabels, null);

                GUI.enabled = mDebugPlayTakeInd > 0;

                //play control
                GUILayout.BeginVertical(GUI.skin.box);
                                
                GUILayout.BeginHorizontal();

                GUILayout.Label("Start At");

                mDebugPlayAtMode = (PlayAtMode)EditorGUILayout.EnumPopup(mDebugPlayAtMode);

                switch(mDebugPlayAtMode) {
                    case PlayAtMode.Frame:
                        mDebugPlayAtFrame = EditorGUILayout.IntField(mDebugPlayAtFrame);
                        break;
                    case PlayAtMode.Time:
                        mDebugPlayAtTime = EditorGUILayout.FloatField(mDebugPlayAtTime);
                        break;
                }

                GUILayout.EndHorizontal();

                mDebugPlayIsLoop = EditorGUILayout.Toggle("Loop", mDebugPlayIsLoop);

                if(GUILayout.Button("Play")) {
                    mAnim.Stop();

                    var takeInd = mDebugPlayTakeInd - 1;

                    switch(mDebugPlayAtMode) {
                        case PlayAtMode.Frame:
                            mAnim.PlayAtFrame(takeInd, mDebugPlayAtFrame, mDebugPlayIsLoop);
                            break;
                        case PlayAtMode.Time:
                            mAnim.PlayAtTime(takeInd, mDebugPlayAtTime, mDebugPlayIsLoop);
                            break;
                    }
                }

                GUILayout.EndVertical();

                //Goto control
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                mDebugGotoAtMode = (PlayAtMode)EditorGUILayout.EnumPopup(mDebugGotoAtMode);

                switch(mDebugGotoAtMode) {
                    case PlayAtMode.Frame:
                        mDebugGotoFrame = EditorGUILayout.IntField(mDebugGotoFrame);
                        break;
                    case PlayAtMode.Time:
                        mDebugGotoTime = EditorGUILayout.FloatField(mDebugGotoTime);
                        break;
                }

                GUILayout.EndHorizontal();

                if(GUILayout.Button("Goto")) {
                    var takeInd = mDebugPlayTakeInd - 1;

                    switch(mDebugGotoAtMode) {
                        case PlayAtMode.Frame:
                            mAnim.GotoFrame(takeInd, mDebugGotoFrame);
                            break;
                        case PlayAtMode.Time:
                            mAnim.Goto(takeInd, mDebugGotoTime);
                            break;
                    }
                }

                GUILayout.EndVertical();

                GUI.enabled = isCurTakeValid;

                GUILayout.BeginVertical(GUI.skin.box);

                //resume/pause control
                if(mAnim.isPlaying) {
                    if(GUILayout.Button("Pause"))
                        mAnim.Pause();
                }
                else {
                    if(GUILayout.Button("Resume"))
                        mAnim.Resume();
                }

                //stop control
                if(GUILayout.Button("Stop"))
                    mAnim.Stop();

                GUILayout.EndVertical();

                GUI.enabled = lastEnabled;
            }
        }
    }
}
