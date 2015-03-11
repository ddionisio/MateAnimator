using UnityEngine;
using UnityEditor;
using System.Collections;

using Holoville.HOTween;

public class AMSettings : EditorWindow {
    public static AMSettings window = null;

    public AMOptionsFile oData;

    private AnimatorDataEdit __aData;

    public AnimatorDataEdit aData {
        get {
            if(AMTimeline.window.aData != __aData) {
                reloadAnimatorData();
            }

            return __aData;
        }
    }

    private int numFrames;
    private int frameRate;
    private int loopCount = 1;
    private LoopType loopMode = LoopType.Restart;
    private int loopBackFrame = -1;
    private bool saveChanges = false;
    // skins
    private GUISkin skin = null;
    private string cachedSkinName = null;

    void OnEnable() {
        window = this;
        this.title = "Settings";
        this.minSize = new Vector2(280f, 190f);
        //this.maxSize = this.minSize;

        oData = AMOptionsFile.loadFile();
        loadAnimatorData();

    }
    void OnDisable() {
        window = null;
        if(aData != null && saveChanges) {
            AMTakeData take = aData.currentTake;
            bool saveNumFrames = true;
			if((numFrames < take.numFrames) && (take.hasKeyAfter(numFrames))) {
                if(!EditorUtility.DisplayDialog("Data Will Be Lost", "You will lose some keys beyond frame " + numFrames + " if you continue.", "Continue Anway", "Cancel")) {
                    saveNumFrames = false;
                }
            }

			string label = take.name+": Modify Settings";
			AMTimeline.RegisterTakesUndo(aData, label, true);
            take = aData.currentTake;
            
            if(saveNumFrames) {
				Undo.RegisterCompleteObjectUndo(AnimatorDataEdit.GetKeysAndTracks(take), label);

                // save numFrames
				take.numFrames = numFrames;
				AMKey[]dkeys = take.removeKeysAfter(aData.target, numFrames);
				foreach(AMKey dkey in dkeys)
					Undo.DestroyObjectImmediate(dkey);

                // save data
				foreach(AMTrack track in take.trackValues) {
					foreach(AMKey key in track.keys)
						EditorUtility.SetDirty(key);
                    EditorUtility.SetDirty(track);
                }
            }
            // save frameRate
			take.frameRate = frameRate;

            //save other data
			take.numLoop = loopCount;
			take.loopMode = loopMode;
			take.loopBackToFrame = Mathf.Clamp(loopBackFrame, -1, numFrames);

            // save data
            aData.SetDirtyTakes();

            EditorWindow.GetWindow(typeof(AMTimeline)).Repaint();
        }
    }
    void OnGUI() {
        AMTimeline.loadSkin(ref skin, ref cachedSkinName, position);
        if(aData == null) {
            AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.", AMTimeline.MessageBoxType.Warning);
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
        EditorGUIUtility.LookLikeControls(50.0f, 100.0f);
        loopCount = EditorGUILayout.IntField("Count", loopCount);
        if(loopCount < 0) loopCount = -1;
        loopMode = (LoopType)EditorGUILayout.EnumPopup("Mode", loopMode);
        GUILayout.EndHorizontal();

        EditorGUIUtility.LookLikeControls();

        GUILayout.Space(2f);
        //pausePreviousTake = EditorGUILayout.Toggle("Pause Prev. Take", pausePreviousTake);
                
        GUILayout.Space(4f);
        GUI.enabled = loopCount <= 0;
        loopBackFrame = EditorGUILayout.IntSlider("Loop Back To Frame", loopBackFrame, -1, numFrames);
        GUI.enabled = true;
        GUILayout.Space(6f);
        GUILayout.Label("Number of Frames");
        GUILayout.Space(2f);
        numFrames = EditorGUILayout.IntField(numFrames, GUI.skin.textField, GUILayout.Width(position.width - 10f - 12f));
        if(numFrames <= 0) numFrames = 1;
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
    public void reloadAnimatorData() {
        __aData = null;
        loadAnimatorData();
    }
    void loadAnimatorData() {
        if(AMTimeline.window) {
            __aData = AMTimeline.window.aData;
            AMTakeData take = aData.currentTake;
            numFrames = take.numFrames;
            frameRate = take.frameRate;
            loopCount = take.numLoop;
            loopMode = take.loopMode;
            loopBackFrame = take.loopBackToFrame;
        }
    }
}
