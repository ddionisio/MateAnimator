using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(AnimatorData))]
public class AnimatorDataInspector : Editor {
	private string[] mTakeLabels;

	void OnEnable() {
		AnimatorData dat = target as AnimatorData;

		mTakeLabels = new string[dat._takes.Count + 1];
		mTakeLabels[0] = "None";
		for(int i = 0; i < dat._takes.Count; i++) {
			mTakeLabels[i+1] = dat._takes[i].name;
		}
	}

    public override void OnInspectorGUI() {
        serializedObject.Update();

        AnimatorData dat = target as AnimatorData;

        GUILayout.BeginVertical();

        /*string[] takeNames = dat.GenerateTakeNames();
        int[] takeInds = new int[takeNames.Length];
        for(int i = 0; i < takeNames.Length; i++)
            takeInds[i] = i;

        int playOnStartInd = dat.playOnStart != null ? System.Array.IndexOf(takeNames, dat.playOnStart.name) : 0;
        if(playOnStartInd == -1)
            playOnStartInd = 0;

        playOnStartInd = EditorGUILayout.IntPopup("Play On Start", playOnStartInd, takeNames, takeInds);

        if(playOnStartInd == 0)
            dat.playOnStart = null;
        else
            dat.playOnStart = dat.takes[playOnStartInd - 1];*/
		List<AMTakeData> takes = dat._takes;
		string playTakeName = dat.defaultTakeName;
		int playTakeInd = 0;
		if(!string.IsNullOrEmpty(playTakeName)) {
			for(int i = 0; i < takes.Count; i++) {
				if(takes[i].name == dat.defaultTakeName) {
					playTakeInd = i+1;
					break;
				}
			}
		}

		int newPlayTakeInd = EditorGUILayout.IntPopup("Play On Start", playTakeInd, mTakeLabels, null);
		if(newPlayTakeInd != playTakeInd) {
			Undo.RecordObject(dat, "Set Play On Start");
			dat.defaultTakeName = newPlayTakeInd <= 0 ? "" : takes[newPlayTakeInd - 1].name;
		}

		bool isglobal = GUILayout.Toggle(dat.isGlobal, "Global");
		if(dat.isGlobal != isglobal) {
			dat.isGlobal = isglobal;
			if(isglobal) {
				//uncheck isGlobal to any other animator data on scene
				AnimatorData[] anims = FindObjectsOfType<AnimatorData>();
				for(int i = 0; i < anims.Length; i++) {
					if(dat != anims[i] && anims[i].isGlobal) {
						anims[i].isGlobal = false;
						EditorUtility.SetDirty(anims[i]);
					}
				}
			}
		}

        /*GUILayout.Label("Take Count: "+dat.takes.Count);
        if(AMTimeline.window && AMTimeline.window.aData) {
            if(dat.takes.Count != AMTimeline.window.aData.takes.Count) {
                AMTimeline.window.aData = null;
            }
            //GUILayout.Label("Take Count e: " + AMTimeline.window.aData.takes.Count);
        }*/

        dat.sequenceLoadAll = GUILayout.Toggle(dat.sequenceLoadAll, "Build All Sequence On Start");
        dat.sequenceKillWhenDone = GUILayout.Toggle(dat.sequenceKillWhenDone, "Kill Sequence When Done");
        dat.playOnEnable = GUILayout.Toggle(dat.playOnEnable, "Play On Enable");
        dat.onDisableAction = (AnimatorData.DisableAction)EditorGUILayout.EnumPopup("On Disable", dat.onDisableAction);
        dat.updateType = (Holoville.HOTween.UpdateType)EditorGUILayout.EnumPopup("Update", dat.updateType);

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

        if(GUI.changed)
            EditorUtility.SetDirty(dat);

        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
