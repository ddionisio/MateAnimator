using UnityEngine;
using UnityEditor;

using System.IO;

namespace M8.Animator {
	public struct AMEditorResource {
	    private static string skinsDir = null;
	    private static string textureEditorDir = null;
	    private static string textureDir = null;
	        
	    public static GUISkin LoadSkin(string name) {
	        if(skinsDir == null) skinsDir = GetDir(name + ".guiskin");
	        return AssetDatabase.LoadAssetAtPath(string.Format("{0}{1}.guiskin", skinsDir, name), typeof(GUISkin)) as GUISkin;
	    }

	    //public static Texture LoadTexture(string name) {
	        //return Resources.LoadAssetAtPath(string.Format(texturePathFormat, name), typeof(Texture)) as Texture;
	    //}

	    //public static Material LoadMaterial(string name) {
	        //return Resources.LoadAssetAtPath(string.Format(textureMatPathFormat, name), typeof(Material)) as Material;
	    //}

	    public static Texture LoadEditorTexture(string name) {
	        if(textureEditorDir == null) textureEditorDir = GetDir(name + ".png");
	        return AssetDatabase.LoadAssetAtPath(string.Format("{0}{1}.png", textureEditorDir, name), typeof(Texture)) as Texture;
	    }

	    public static Texture LoadTexture(string name) {
	        if(textureDir == null) textureDir = GetDir(name + ".png");
	        return AssetDatabase.LoadAssetAtPath(string.Format("{0}{1}.png", textureDir, name), typeof(Texture)) as Texture;
	    }

	    private static string GetDir(string filename) {
	        string ret = "";
	        string path = FindPath(filename, "Assets", true);
	        if(path != null) {
	            int lastInd = path.LastIndexOf('/');
	            if(lastInd != -1) {
	                ret = path.Substring(0, lastInd+1);
	            }
	        }

	        return ret;
	    }

	    public static string FindPath(string filename, string dir, bool recursive) {
	        string[] paths = Directory.GetFiles(dir, filename, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

	        return paths.Length > 0 ? paths[0].Replace('\\', '/') : null;
	    }
	}
}
