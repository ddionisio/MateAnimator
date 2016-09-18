using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
	public class AMMaterialEditor : EditorWindow {
	    public static AMMaterialEditor window { get; private set; }

	    private AnimatorDataEdit mData;
	    private AMMaterialTrack mTrack;

	    // skins
	    private GUISkin skin = null;
	    private string cachedSkinName = null;

	    public AnimatorDataEdit aData {
	        get {
	            if(AMTimeline.window != null && AMTimeline.window.aData != mData) {
	                mTrack = null;
	                reloadAnimatorData();
	            }

	            return mData;
	        }
	    }

	    public static void Open(AMMaterialTrack track) {
	        window = EditorWindow.GetWindow(typeof(AMMaterialEditor)) as AMMaterialEditor;

	        window.mTrack = track;
	    }

	    public void reloadAnimatorData() {
	        mData = null;
	        loadAnimatorData();
	    }

	    void OnHierarchyChange() {
	        if(aData == null) reloadAnimatorData();
	    }

	    void OnEnable() {
	#if UNITY_5
	        titleContent = new GUIContent("Material");
	#else
	        title = "Material";
	#endif
	        //this.minSize = new Vector2(273f, 102f);
	        loadAnimatorData();
	        //scrollView = new Vector2(0f, 0f);
	        // define styles		
	    }

	    void OnDisable() {
	        window = null;
	    }

	    void OnGUI() {
	        AMTimeline.loadSkin(ref skin, ref cachedSkinName, position);
	        if(aData == null) {
	            AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.", AMTimeline.MessageBoxType.Warning);
	            return;
	        }

	        if(!mTrack)
	            return;

	        Renderer render = mTrack.GetTarget(aData.target) as Renderer;
	        if(!render) {
	            AMTimeline.MessageBox("Assign a Renderer to the track first.", AMTimeline.MessageBoxType.Warning);
	            return;
	        }

	        //select material
	        Material[] mats = render.sharedMaterials;

	        string[] matNames = new string[mats.Length];
	        int[] matInds = new int[mats.Length];
	        for(int i = 0; i < mats.Length; i++) {
	            matNames[i] = mats[i].name;
	            matInds[i] = i;
	        }
	                
	        //grab track info
	        int matInd = Mathf.Clamp(mTrack.materialIndex, 0, mats.Length - 1);
	        Material matOverride = mTrack.materialOverride;
	        string shaderProperty = mTrack.property;
	        
	        //material select
	        matInd = EditorGUILayout.IntPopup("Material", matInd, matNames, matInds);

	        //material override select
	        matOverride = EditorGUILayout.ObjectField("Material Override", matOverride, typeof(Material), false) as Material;

	        Material mat = matOverride ? matOverride : mats[matInd];

	        //grab material info
	        string[] shaderPropertyNames, shaderPropertyDetails;
	        AMMaterialTrack.ValueType[] shaderPropertyTypes;
	        int[] shaderPropertyInds;
	        GetPropertyInfos(mat, out shaderPropertyNames, out shaderPropertyDetails, out shaderPropertyTypes, out shaderPropertyInds);

	        int shaderPropertyInd = -1;
	        AMMaterialTrack.ValueType shaderPropertyType = mTrack.propertyType;

	        for(int i = 0; i < shaderPropertyNames.Length; i++) {
	            if(shaderProperty == shaderPropertyNames[i]) {
	                shaderPropertyInd = i;

	                //special case for texture offset and scale
	                if(shaderPropertyTypes[i] == AMMaterialTrack.ValueType.TexEnv && i + 2 < shaderPropertyNames.Length) {
	                    if(shaderPropertyType == shaderPropertyTypes[i+1])
	                        shaderPropertyInd += 1;
	                    else if(shaderPropertyType == shaderPropertyTypes[i+2])
	                        shaderPropertyInd += 2;
	                }
	                break;
	            }
	        }

	        if(shaderPropertyInd == -1)
	            shaderPropertyInd = 0;

	        AMEditorUtil.DrawSeparator();

	        //shader property select
	        shaderPropertyInd = EditorGUILayout.IntPopup("Property", shaderPropertyInd, shaderPropertyDetails, shaderPropertyInds);
	        
	        shaderProperty = shaderPropertyNames[shaderPropertyInd];
	        shaderPropertyType = shaderPropertyTypes[shaderPropertyInd];

	        //check for change
	        if(mTrack.materialIndex != matInd || mTrack.materialOverride != matOverride || mTrack.property != shaderProperty || mTrack.propertyType != shaderPropertyType) {
	            Undo.RecordObject(mTrack, "Material Track Property Change");

	            mTrack.materialIndex = matInd;
	            mTrack.materialOverride = matOverride;
	            mTrack.property = shaderProperty;
	            mTrack.propertyType = shaderPropertyType;

	            EditorUtility.SetDirty(mTrack);
	        }
	    }

	    void GetPropertyInfos(Material mat, out string[] names, out string[] details, out AMMaterialTrack.ValueType[] types, out int[] inds) {
	        Shader shader = mat.shader;
	        int count = ShaderUtil.GetPropertyCount(shader);

	        List<string> _names = new List<string>();
	        List<AMMaterialTrack.ValueType> _types = new List<AMMaterialTrack.ValueType>();
	        
	        for(int i = 0; i < count; i++) {
	            var name = ShaderUtil.GetPropertyName(shader, i);
	            var type = ShaderUtil.GetPropertyType(shader, i);

	            _names.Add(name);
	            _types.Add((AMMaterialTrack.ValueType)type);

	            if(type == ShaderUtil.ShaderPropertyType.TexEnv) {
	                _names.Add(name); _types.Add(AMMaterialTrack.ValueType.TexOfs);
	                _names.Add(name); _types.Add(AMMaterialTrack.ValueType.TexScale);
	            }
	        }

	        count = _names.Count;

	        names = new string[count];
	        details = new string[count];
	        types = new AMMaterialTrack.ValueType[count];
	        inds = new int[count];

	        for(int i = 0; i < count; i++) {
	            names[i] = _names[i];
	            types[i] = _types[i];

	            switch(types[i]) {
	                case AMMaterialTrack.ValueType.TexOfs:
	                    details[i] = string.Format("{0} Offset", names[i]);
	                    break;
	                case AMMaterialTrack.ValueType.TexScale:
	                    details[i] = string.Format("{0} Scale", names[i]);
	                    break;
	                default:
	                    details[i] = names[i];
	                    break;
	            }

	            inds[i] = i;
	        }
	    }

	    void loadAnimatorData() {
	        if(AMTimeline.window)
	            mData = AMTimeline.window.aData;
	        else
	            mData = null;
	    }
	}
}
