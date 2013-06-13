using UnityEngine;
using UnityEditor;

public struct AMEditorResource {
    public const string skinsPathFormat = "Assets/M8Animator/Skins/{0}.guiskin";

    public const string textureEditorPathFormat = "Assets/M8Animator/TexturesEditor/{0}.png";

    public const string texturePathFormat = "Assets/M8Animator/Textures/{0}.png";
    public const string textureMatPathFormat = "Assets/M8Animator/Textures/{0}.mat";
        
    public static GUISkin LoadSkin(string name) {
        return Resources.LoadAssetAtPath(string.Format(skinsPathFormat, name), typeof(GUISkin)) as GUISkin;
    }

    public static Texture LoadTexture(string name) {
        return Resources.LoadAssetAtPath(string.Format(texturePathFormat, name), typeof(Texture)) as Texture;
    }

    public static Material LoadMaterial(string name) {
        return Resources.LoadAssetAtPath(string.Format(textureMatPathFormat, name), typeof(Material)) as Material;
    }

    public static Texture LoadEditorTexture(string name) {
        return Resources.LoadAssetAtPath(string.Format(textureEditorPathFormat, name), typeof(Texture)) as Texture;
    }
}
