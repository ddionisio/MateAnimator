using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator.Edit {
    public class MaterialEditor : EditorWindow {
        public static MaterialEditor window { get; private set; }

        private AnimateEditControl mData;
        private MaterialTrack mTrack;

        // skins
        private GUISkin skin = null;
        private string cachedSkinName = null;

        public AnimateEditControl aData {
            get {
                if(TimelineWindow.window != null && TimelineWindow.window.aData != mData) {
                    mTrack = null;
                    reloadAnimate();
                }

                return mData;
            }
        }

        public static void Open(MaterialTrack track) {
            window = EditorWindow.GetWindow(typeof(MaterialEditor)) as MaterialEditor;

            window.mTrack = track;
        }

        public void reloadAnimate() {
            mData = null;
            loadAnimatorData();
        }

        void OnHierarchyChange() {
            if(aData == null) reloadAnimate();
        }

        void OnEnable() {
            titleContent = new GUIContent("Material");

            //this.minSize = new Vector2(273f, 102f);
            loadAnimatorData();
            //scrollView = new Vector2(0f, 0f);
            // define styles		
        }

        void OnDisable() {
            window = null;
        }

        void OnGUI() {
            TimelineWindow.loadSkin(ref skin, ref cachedSkinName, position);
            if(aData == null) {
                TimelineWindow.MessageBox("Animator requires an Animate component in your scene. Launch Animator to add the component.", TimelineWindow.MessageBoxType.Warning);
                return;
            }

            if(mTrack == null)
                return;

            Renderer render = mTrack.GetTarget(aData.target) as Renderer;
            if(!render) {
                TimelineWindow.MessageBox("Assign a Renderer to the track first.", TimelineWindow.MessageBoxType.Warning);
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
            MaterialTrack.ValueType[] shaderPropertyTypes;
            int[] shaderPropertyInds;
            GetPropertyInfos(mat, out shaderPropertyNames, out shaderPropertyDetails, out shaderPropertyTypes, out shaderPropertyInds);

            int shaderPropertyInd = -1;
            MaterialTrack.ValueType shaderPropertyType = mTrack.propertyType;

            for(int i = 0; i < shaderPropertyNames.Length; i++) {
                if(shaderProperty == shaderPropertyNames[i]) {
                    shaderPropertyInd = i;

                    //special case for texture offset and scale
                    if(shaderPropertyTypes[i] == MaterialTrack.ValueType.TexEnv && i + 2 < shaderPropertyNames.Length) {
                        if(shaderPropertyType == shaderPropertyTypes[i + 1])
                            shaderPropertyInd += 1;
                        else if(shaderPropertyType == shaderPropertyTypes[i + 2])
                            shaderPropertyInd += 2;
                    }
                    break;
                }
            }

            if(shaderPropertyInd == -1)
                shaderPropertyInd = 0;

            EditorUtility.DrawSeparator();

            //shader property select
            shaderPropertyInd = EditorGUILayout.IntPopup("Property", shaderPropertyInd, shaderPropertyDetails, shaderPropertyInds);

            shaderProperty = shaderPropertyNames[shaderPropertyInd];
            shaderPropertyType = shaderPropertyTypes[shaderPropertyInd];

            //check for change
            if(mTrack.materialIndex != matInd || mTrack.materialOverride != matOverride || mTrack.property != shaderProperty || mTrack.propertyType != shaderPropertyType) {
                aData.RegisterTakesUndo("Material Track Property Change");

                mTrack.materialIndex = matInd;
                mTrack.materialOverride = matOverride;
                mTrack.property = shaderProperty;
                mTrack.propertyType = shaderPropertyType;
            }
        }

        void GetPropertyInfos(Material mat, out string[] names, out string[] details, out MaterialTrack.ValueType[] types, out int[] inds) {
            Shader shader = mat.shader;
            int count = ShaderUtil.GetPropertyCount(shader);

            List<string> _names = new List<string>();
            List<MaterialTrack.ValueType> _types = new List<MaterialTrack.ValueType>();

            for(int i = 0; i < count; i++) {
                var name = ShaderUtil.GetPropertyName(shader, i);
                var type = ShaderUtil.GetPropertyType(shader, i);

                _names.Add(name);
                _types.Add((MaterialTrack.ValueType)type);

                if(type == ShaderUtil.ShaderPropertyType.TexEnv) {
                    _names.Add(name); _types.Add(MaterialTrack.ValueType.TexOfs);
                    _names.Add(name); _types.Add(MaterialTrack.ValueType.TexScale);
                }
            }

            count = _names.Count;

            names = new string[count];
            details = new string[count];
            types = new MaterialTrack.ValueType[count];
            inds = new int[count];

            for(int i = 0; i < count; i++) {
                names[i] = _names[i];
                types[i] = _types[i];

                switch(types[i]) {
                    case MaterialTrack.ValueType.TexOfs:
                        details[i] = string.Format("{0} Offset", names[i]);
                        break;
                    case MaterialTrack.ValueType.TexScale:
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
            if(TimelineWindow.window)
                mData = TimelineWindow.window.aData;
            else
                mData = null;
        }
    }
}
