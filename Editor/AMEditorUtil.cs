using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace M8.Animator {
	public struct AMEditorUtil {
	    public static List<Sprite> GetSprites(UnityEngine.Object[] objs) {
	        List<Sprite> sprites = new List<Sprite>(objs.Length);

	        for(int i = 0;i < objs.Length;i++) {
	            if(objs[i] is Sprite) {
	                Sprite spr = objs[i] as Sprite;
	                if(sprites.IndexOf(spr) == -1)
	                    sprites.Add(spr);
	            }
	            else if(objs[i] is Texture2D) {
	                string path = AssetDatabase.GetAssetPath(objs[i]);
	                UnityEngine.Object[] sprs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
	                for(int s = 0;s < sprs.Length;s++) {
	                    if(sprs[s] is Sprite) {
	                        Sprite _spr = sprs[s] as Sprite;
	                        if(sprites.IndexOf(_spr) == -1)
	                            sprites.Add(_spr);
	                    }
	                }
	            }
	        }
	        return sprites;
	    }
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
        public static void ResetDisplayControls() {
            EditorGUIUtility.labelWidth = 0f;
            EditorGUIUtility.fieldWidth = 0f;
        }
	}
}
