using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(AnimatorData))]
public class AnimatorDataInspector : Editor {
    public override void OnInspectorGUI() {
        AnimatorData dat = target as AnimatorData;

        GUILayout.BeginVertical();

        GUILayout.Label("Play On Start: " + (dat.playOnStart != null ? dat.playOnStart.name : "none"));

        if(GUILayout.Button("Edit Timeline")) {
            AMTimeline timeline = AMTimeline.window;

            if(timeline != null) {
                timeline.aData = dat;
                timeline.Repaint();
            }
            else {
                EditorWindow.GetWindow(typeof(AMTimeline));
            }
        }

        GUILayout.EndVertical();
    }
}
