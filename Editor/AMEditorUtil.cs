using UnityEngine;
using UnityEditor;

public struct AMEditorUtil {
	public static void DrawSeparator() {
		GUILayout.Space(12f);
		
		if(Event.current.type == EventType.Repaint) {
			Texture2D tex = EditorGUIUtility.whiteTexture;
			Rect rect = GUILayoutUtility.GetLastRect();
			GUI.color = new Color(0f, 0f, 0f, 0.25f);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
			GUI.color = Color.white;
		}
	}
	public static string GetSelectionFolder() {
		if(Selection.activeObject != null) {
			string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
			
			if(!string.IsNullOrEmpty(path)) {
				int dot = path.LastIndexOf('.');
				int slash = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
				if(slash > 0) return (dot > slash) ? path.Substring(0, slash + 1) : path + "/";
			}
		}
		return "Assets/";
	}
}
