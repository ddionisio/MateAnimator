using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace M8.Animator {
    [CustomPropertyDrawer(typeof(TakeSelectorAttribute))]
    public class TakeSelectorPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if(property.propertyType == SerializedPropertyType.String || property.propertyType == SerializedPropertyType.Integer) {
                //grab animator from obj via attribute's animatorField
                var attrib = this.attribute as TakeSelectorAttribute;
                var obj = property.serializedObject;
                var animatorField = obj.FindProperty(attrib.animatorField);

                if(animatorField != null && animatorField.objectReferenceValue is Animate) {
                    EditorGUI.BeginProperty(position, label, property);

                    var anim = (Animate)animatorField.objectReferenceValue;

                    //generate take names
                    var takeNameList = new List<string>();
                    takeNameList.Add("<None>");
                    for(int i = 0; i < anim.takeCount; i++)
                        takeNameList.Add(anim.GetTakeName(i));

                    int index = 0;

                    if(property.propertyType == SerializedPropertyType.String) {
                        var curTakeName = property.stringValue;

                        //get current take name list index                    
                        if(!string.IsNullOrEmpty(curTakeName)) {
                            for(int i = 1; i < takeNameList.Count; i++) {
                                if(takeNameList[i] == curTakeName) {
                                    index = i;
                                    break;
                                }
                            }
                        }
                    }
                    else if(property.propertyType == SerializedPropertyType.Integer) {
                        index = property.intValue + 1;

                        if(index >= takeNameList.Count)
                            index = 0;
                    }

                    //select
                    index = EditorGUI.Popup(position, label.text, index, takeNameList.ToArray());

                    switch(property.propertyType) {
                        case SerializedPropertyType.String:
                            if(index >= 1 && index < takeNameList.Count)
                                property.stringValue = takeNameList[index];
                            else
                                property.stringValue = "";
                            break;
                        case SerializedPropertyType.Integer:
                            property.intValue = index - 1;
                            break;
                    }

                    EditorGUI.EndProperty();
                }
                else
                    EditorGUI.PropertyField(position, property, label);
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }
}