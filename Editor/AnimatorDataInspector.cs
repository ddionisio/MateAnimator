using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(AnimatorData))]
public class AnimatorDataInspector : Editor {
	private string[] mTakeLabels;
	private bool mMissingsFoldout = true;

	void OnEnable() {
		AnimatorData dat = target as AnimatorData;

		mTakeLabels = new string[dat._takes.Count + 1];
		mTakeLabels[0] = "None";
		for(int i = 0; i < dat._takes.Count; i++) {
			mTakeLabels[i+1] = dat._takes[i].name;
		}

		mMissingsFoldout = true;
	}

    public override void OnInspectorGUI() {
        serializedObject.Update();

        AnimatorData dat = target as AnimatorData;

        GUILayout.BeginVertical();

		//meta
		AnimatorMeta curMeta = dat.e_meta;
		AnimatorMeta newMeta = EditorGUILayout.ObjectField("Meta", curMeta, typeof(AnimatorMeta), false) as AnimatorMeta;

		if(curMeta != newMeta) {
			bool doIt = true;
			if(curMeta == null) {
				doIt = EditorUtility.DisplayDialog("Set Meta", "Setting the Meta will replace the current animation data, proceed?", "Yes", "No");
			}

			if(doIt) {
				Undo.RecordObject(dat, "Set Meta");
				List<UnityEngine.Object> newObjs = dat.e_setMeta(newMeta, false);
				if(newObjs != null && newObjs.Count > 0) {
					foreach(UnityEngine.Object newObj in newObjs)
						Undo.RegisterCreatedObjectUndo(newObj, "Set Meta");
				}
			}
		}

		bool doMetaSave = false, doInstantiate = false;

		GUILayout.BeginHorizontal();

		GUI.backgroundColor = Color.green;
		doMetaSave = GUILayout.Button("Save As...", GUILayout.Width(100f));
		GUI.backgroundColor = Color.white;

		GUILayout.Space(8);

		GUI.enabled = dat.e_meta;
		doInstantiate = GUILayout.Button("Instantiate", GUILayout.Width(100f));
		GUI.enabled = true;

		GUILayout.EndHorizontal();

		AMEditorUtil.DrawSeparator();
		//

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

		//display missings
		string[] missings = dat.e_getMissingTargets();
		if(missings != null && missings.Length > 0) {
			AMEditorUtil.DrawSeparator();
			mMissingsFoldout = EditorGUILayout.Foldout(mMissingsFoldout, string.Format("Missing Targets [{0}]", missings.Length));
			if(mMissingsFoldout) {
				for(int i = 0; i < missings.Length; i++) {
					GUILayout.Label(missings[i]);
				}
			}
		}
		        
        GUILayout.EndVertical();

		if(doMetaSave) {
			string path = EditorUtility.SaveFilePanelInProject("Save AnimatorMeta", dat.name + ".prefab", "prefab", "Please enter a file name to save the animator data to");
			if(!string.IsNullOrEmpty(path)) {
				GameObject metago = new GameObject("_meta");
				metago.AddComponent<AnimatorMeta>();
				UnityEngine.Object pref = PrefabUtility.CreateEmptyPrefab(path);
				GameObject metagopref = PrefabUtility.ReplacePrefab(metago, pref);
				UnityEngine.Object.DestroyImmediate(metago);
				dat.e_setMeta(metagopref.GetComponent<AnimatorMeta>(), true);
			}
		}
		else if(doInstantiate) {
			dat.e_setMeta(null, true);
		}

		if(GUI.changed)
			EditorUtility.SetDirty(dat);

        serializedObject.ApplyModifiedProperties();
    }
}
