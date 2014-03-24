using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AMPropertySelect : EditorWindow {
    public AMOptionsFile oData;

    private AnimatorData __aData;

    public AnimatorData aData {
        get {
            if(AMTimeline.window != null && AMTimeline.window.aData != __aData) {
                track = null;
                reloadAnimatorData();
            }

            return __aData;
        }
    }

    public static AMPropertySelect window = null;
    private static AMPropertyTrack track = null;

    private int selectionIndex = -1;
    private Component[] arrComponents;
    private GameObject _go;
    private Vector2 scrollView;
    //private Vector2 scrollViewComponent;
    private string[] ignoreProperties = { "rolloffFactor", "minVolume", "maxVolume" };
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
    // skins
    private GUISkin skin = null;
    private string cachedSkinName = null;

    void OnEnable() {
        window = this;
        this.title = "Property";
        this.minSize = new Vector2(273f, 102f);
        this.wantsMouseMove = true;
        oData = AMOptionsFile.loadFile();
        loadAnimatorData();
        scrollView = new Vector2(0f, 0f);
        // define styles		
    }
    void OnDisable() {
        window = null;
        track = null;
    }
    void OnHierarchyChange() {
        if(!aData) reloadAnimatorData();
    }
    public void reloadAnimatorData() {
        __aData = null;
        loadAnimatorData();
    }
    void loadAnimatorData() {
        if(AMTimeline.window) {
            __aData = AMTimeline.window.aData;
            if(track) {
                _go = track.obj;
                // refresh	
                updateComponentArray();
            }
        }
    }
    void Update() {
        if(EditorWindow.mouseOverWindow == this) this.Repaint();
    }
    void OnGUI() {

        AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
        if(!aData) {
            AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.", AMTimeline.MessageBoxType.Warning);
            return;
        }
        if(!track) {
            return;
        }
        if(!(track).obj) {
            AMTimeline.MessageBox("Assign a GameObject to the track first.", AMTimeline.MessageBoxType.Warning);
            return;
        }
        GUILayout.Label("Select a property to add to track '" + track.name + "'"/*, styleLabel*/);
        scrollView = GUILayout.BeginScrollView(scrollView);
        if(arrComponents != null && arrComponents.Length > 0) {
            for(int i = 0; i < arrComponents.Length; i++) {
                // skip behaviours because they may repeat properties
                // if script is missing (unlikely but it happens in error) then catch and skip
                try {
                    if(arrComponents[i].GetType() == typeof(Behaviour)) continue;
                }
                catch {
                    continue;
                }
                Component myComponent = _go.GetComponent(arrComponents[i].GetType());
                if(myComponent == null) continue;

                // component button
                GUILayout.BeginHorizontal(GUILayout.Width(position.width - 5f));
                string componentName = myComponent.GetType().Name;
                if(GUILayout.Button(componentName/*,buttonStyle*/)) {
                    if(selectionIndex != i) selectionIndex = i;
                    else selectionIndex = -1;
                }
                string lblToggle;
                if(selectionIndex != i) lblToggle = "+";
                else lblToggle = "-";

                GUILayout.Label(lblToggle, GUILayout.Width(15f));

                GUILayout.EndHorizontal();

                if(selectionIndex == i) {
                    //scrollViewComponent = GUILayout.BeginScrollView(scrollViewComponent);
                    int numberOfProperties = 0;
                    FieldInfo[] fields = myComponent.GetType().GetFields();
                    // loop through all fields sfields
                    foreach(FieldInfo fieldInfo in fields) {
                        if(!AMPropertyTrack.isValidType(fieldInfo.FieldType)) {
                            // invalid type
                            continue;
                        }
                        // fields
                        GUILayout.BeginHorizontal();
                        // field button
                        if(GUILayout.Button(fieldInfo.Name, GUILayout.Width(150f))) {
                            // select the field
                            processSelectProperty(myComponent, fieldInfo, null);

                        }
                        GUILayout.Label(fieldInfo.GetValue(myComponent).ToString());
                        GUILayout.EndHorizontal();
                        numberOfProperties++;

                    }
                    PropertyInfo[] properties = myComponent.GetType().GetProperties();
                    // properties
                    foreach(PropertyInfo propertyInfo in properties) {
                        if(propertyInfo.PropertyType == typeof(HideFlags)) {
                            continue;
                        }
                        if(shouldIgnoreProperty(propertyInfo.Name)) continue;
                        if(propertyInfo.CanWrite && AMPropertyTrack.isValidType(propertyInfo.PropertyType)) {

                            object propertyValue;
                            try {
                                propertyValue = propertyInfo.GetValue(myComponent, null);
                            }
                            catch {
                                continue;
                            }
                            GUILayout.BeginHorizontal();
                            if(GUILayout.Button(propertyInfo.Name, GUILayout.Width(150f))) {
                                // select the property
                                processSelectProperty(myComponent, null, propertyInfo);
                            }

                            GUILayout.Label(propertyValue.ToString());
                            GUILayout.EndHorizontal();
                            numberOfProperties++;
                        }
                    }
                    if(numberOfProperties <= 0) {
                        GUILayout.Label("No usable properties found");
                    }
                    //GUILayout.EndScrollView();
                }
            }
        }
        GUILayout.EndScrollView();
    }
    bool shouldIgnoreProperty(string propertyName) {
        foreach(string s in ignoreProperties) {
            if(propertyName == s) return true;
        }
        return false;
    }
    void updateComponentArray() {
        if(!_go) {
            arrComponents = null;
            return;
        }
        arrComponents = _go.GetComponents(typeof(Component));
    }

    public static void setValues(AMPropertyTrack _track) {
        track = _track;
    }

    void processSelectProperty(Component propertyComponent, FieldInfo fieldInfo, PropertyInfo propertyInfo, MethodInfo methodMegaMorphSetPercent = null) {

        if(!aData || !track) this.Close();

        bool changePropertyValue = true;
        if((track.keys.Count > 0) && (!EditorUtility.DisplayDialog("Data Will Be Lost", "You will lose all of the keyframes on track '" + track.name + "' if you continue.", "Continue Anway", "Cancel"))) {
            changePropertyValue = false;
        }
        if(changePropertyValue) {

			Undo.RegisterCompleteObjectUndo(track, "Set Property");
            // delete keys
            if(track.keys.Count > 0) {
				foreach(AMKey key in track.keys)
					Undo.DestroyObjectImmediate(key);
				track.keys = new List<AMKey>();
                AMCodeView.refresh();
            }
            // set fieldinfo or propertyinfo
            if(fieldInfo != null)
                track.setFieldInfo(fieldInfo);
            else if(propertyInfo != null)
                track.setPropertyInfo(propertyInfo);
            // set component
            track.setComponent(propertyComponent);
            track.updateCache();
        }
        this.Close();
    }
}
