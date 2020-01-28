using UnityEngine;
using UnityEditor;
using System.Collections;

using DG.Tweening;

namespace M8.Animator.Edit {
    public class TakeSettings : EditorWindow {
        public static TakeSettings window = null;

        private AnimateEditControl __aData;

        public AnimateEditControl aData {
            get {
                if(TimelineWindow.window == null) return null;

                if(TimelineWindow.window.aData != __aData) {
                    reloadAnimate();
                }

                return __aData;
            }
        }

        private int endFramePadding;
        private int frameRate;
        private int loopCount = 1;
        private LoopType loopMode = LoopType.Restart;
        private int loopBackFrame = 0;
        private bool loopBackFrameCheck;
        private bool saveChanges = false;
        // skins
        private GUISkin skin = null;
        private string cachedSkinName = null;

        private int totalFrames;

        void OnEnable() {
            window = this;

            titleContent = new GUIContent("Settings");

            minSize = new Vector2(280f, 190f);
            //maxSize = this.minSize;

            loadAnimatorData();

        }
        void OnDisable() {
            window = null;
            if(aData != null && saveChanges) {
                Take take = aData.currentTake;
                bool saveNumFrames = true;

                string label = take.name + ": Modify Settings";
                aData.RegisterTakesUndo(label);
                take = aData.currentTake;

                if(saveNumFrames) {
                    // save end frame padding
                    take.endFramePadding = endFramePadding;
                }
                // save frameRate
                take.frameRate = frameRate;

                //save other data
                take.numLoop = loopCount;
                take.loopMode = loopMode;
                take.loopBackToFrame = loopBackFrameCheck ? Mathf.Clamp(loopBackFrame, 1, totalFrames) : 0;

                GetWindow(typeof(TimelineWindow)).Repaint();
            }
        }
        void OnGUI() {
            TimelineWindow.loadSkin(ref skin, ref cachedSkinName, position);
            if(aData == null) {
                TimelineWindow.MessageBox("Animator requires an Animate component in your scene. Launch Animator to add the component.", TimelineWindow.MessageBoxType.Warning);
                return;
            }

            //AMTake curTake = aData.getCurrentTake();

            GUIStyle styleArea = new GUIStyle(GUI.skin.scrollView);
            styleArea.padding = new RectOffset(4, 4, 4, 4);
            GUILayout.BeginArea(new Rect(0f, 0f, position.width, position.height), styleArea);
            //GUILayout.Label("Take: " + curTake.name + " of "+aData.gameObject.name + " "+curTake.GetHashCode());
            GUILayout.Label("Loop");
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUIUtility.labelWidth = 50f; EditorGUIUtility.fieldWidth = 100f;
            loopCount = EditorGUILayout.IntField("Count", loopCount);
            if(loopCount < 0) loopCount = -1;
            loopMode = (LoopType)EditorGUILayout.EnumPopup("Mode", loopMode);
            GUILayout.EndHorizontal();

            EditorUtility.ResetDisplayControls();

            GUILayout.Space(2f);
            //pausePreviousTake = EditorGUILayout.Toggle("Pause Prev. Take", pausePreviousTake);

            GUILayout.Space(4f);
            bool loopBackFrameEnabled = loopCount < 0;
            GUI.enabled = loopBackFrameEnabled;
            GUILayout.BeginHorizontal();
            loopBackFrameCheck = EditorGUILayout.Toggle(loopBackFrameCheck, GUILayout.Width(12f));
            GUI.enabled = loopBackFrameEnabled && loopBackFrameCheck;
            loopBackFrame = EditorGUILayout.IntSlider("Loop Back To Frame", loopBackFrame, 1, totalFrames);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(6f);
            GUILayout.Label("End Frame Padding");
            GUILayout.Space(2f);
            GUI.enabled = !loopBackFrameEnabled || !loopBackFrameCheck || loopBackFrame < 0;
            endFramePadding = EditorGUILayout.IntField(endFramePadding, GUI.skin.textField, GUILayout.Width(position.width - 10f - 12f));
            if(endFramePadding < 0) endFramePadding = 0;
            GUI.enabled = true;
            GUILayout.Space(2f);
            GUILayout.Label("Frame Rate (Fps)");
            GUILayout.Space(2f);
            frameRate = EditorGUILayout.IntField(frameRate, GUI.skin.textField, GUILayout.Width(position.width - 10f - 12f));
            if(frameRate <= 0) frameRate = 1;
            GUILayout.Space(7f);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Apply")) {
                saveChanges = true;
                this.Close();
            }
            if(GUILayout.Button("Cancel")) {
                saveChanges = false;
                this.Close();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        void OnHierarchyChange() {
            if(aData == null) loadAnimatorData();
        }
        public void reloadAnimate() {
            __aData = null;
            loadAnimatorData();
        }
        void loadAnimatorData() {
            if(TimelineWindow.window) {
                __aData = TimelineWindow.window.aData;

                if(aData == null) return;
                if(aData.currentTake == null) return;

                Take take = aData.currentTake;
                endFramePadding = take.endFramePadding;
                frameRate = take.frameRate;
                loopCount = take.numLoop;
                loopMode = take.loopMode;
                loopBackFrame = take.loopBackToFrame;
                loopBackFrameCheck = loopBackFrame > 0;
                totalFrames = take.totalFrames;
            }
        }
    }
}
