using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AMPropertySelect : EditorWindow {
	public AMOptionsFile oData;
	public AnimatorData aData;
	
	public static AMPropertySelect window = null;
	public static AMPropertyTrack track = null;
	
	private int selectionIndex = -1;
	private Component[] arrComponents;
	private GameObject _go;
	private Vector2 scrollView;	
	//private Vector2 scrollViewComponent;
	private string[] ignoreProperties = {"rolloffFactor","minVolume","maxVolume"};
	const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
	// skins
	private GUISkin skin = null;
	private string cachedSkinName = null;
	
	void OnEnable() {
		window = this;
		this.title = "Property";
		this.minSize = new Vector2(273f,102f);
		this.wantsMouseMove = true;
		oData = AMOptionsFile.loadFile();
		loadAnimatorData();
		scrollView = new Vector2(0f,0f);
		// define styles		
	}
	void OnDisable() {
		window = null;	
	}
	void OnHierarchyChange()
	{
		if(!aData) reloadAnimatorData();
	}
	public void reloadAnimatorData() {
		aData = null;
		loadAnimatorData();
	}
	void loadAnimatorData()
	{
		GameObject go = GameObject.Find ("AnimatorData");
		if(go) {
			aData = (AnimatorData) go.GetComponent ("AnimatorData");
			if(track) {
				_go = track.obj;
				// refresh	
				updateComponentArray();
			}
		}
	}
	void Update() {
		if(EditorWindow.mouseOverWindow==this) this.Repaint();
	}
	void OnGUI() {
		
		AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
		if(!aData) {
			AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.",AMTimeline.MessageBoxType.Warning);
			return;
		}
		if(!track) {
			return;
		}
		if(!(track).obj) {
			AMTimeline.MessageBox("Assign a GameObject to the track first.",AMTimeline.MessageBoxType.Warning);
			return;	
		}
		GUILayout.Label ("Select a property to add to track '"+track.name+"'"/*, styleLabel*/);
		scrollView = GUILayout.BeginScrollView(scrollView);
			if(arrComponents != null && arrComponents.Length > 0) {
				for(int i=0;i<arrComponents.Length;i++) {
						// skip behaviours because they may repeat properties
						// if script is missing (unlikely but it happens in error) then catch and skip
						try {
							if(arrComponents[i].GetType() == typeof(Behaviour)) continue;
						} catch {
							continue;
						}	
						Component myComponent = _go.GetComponent(arrComponents[i].GetType());
						if(myComponent == null) continue;

						// component button
						GUILayout.BeginHorizontal(GUILayout.Width(position.width-5f));
							string componentName = myComponent.GetType().Name;
							if(GUILayout.Button(componentName/*,buttonStyle*/)) {
								if(selectionIndex != i) selectionIndex = i;
								else selectionIndex = -1;
							}
							string lblToggle;
							if(selectionIndex != i) lblToggle = "+";
							else lblToggle = "-";
						
							GUILayout.Label(lblToggle,GUILayout.Width (15f));
							
						GUILayout.EndHorizontal();
						
						if(selectionIndex == i) {
						//scrollViewComponent = GUILayout.BeginScrollView(scrollViewComponent);
							int numberOfProperties = 0;
							// check for special properties
							if(componentName == "MegaMorph") {
								processMegaMorph(myComponent);
								numberOfProperties++;
							}
							FieldInfo[] fields = myComponent.GetType().GetFields();
							// loop through all fields sfields
							foreach (FieldInfo fieldInfo in fields)
							{
								if(!AMPropertyTrack.isValidType(fieldInfo.FieldType)) {
									// invalid type
									continue;
								}
								// fields
								GUILayout.BeginHorizontal();
									// field button
		    						if(GUILayout.Button (fieldInfo.Name,GUILayout.Width(150f))) {
										// select the field
										processSelectProperty(myComponent,fieldInfo,null);
										
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
									try{
										propertyValue = propertyInfo.GetValue(myComponent,null);
									} catch {
										continue;
									}
									GUILayout.BeginHorizontal();
										if(GUILayout.Button(propertyInfo.Name,GUILayout.Width(150f))) {
											// select the property
											processSelectProperty(myComponent,null,propertyInfo);
										}
										
										GUILayout.Label(propertyValue.ToString());
									GUILayout.EndHorizontal();
									numberOfProperties++;
								}
							}
							if(numberOfProperties<=0) {
								GUILayout.Label ("No usable properties found");
							}
						//GUILayout.EndScrollView();
						}
					}	
			}
		GUILayout.EndScrollView();
	}
	void processMegaMorph(Component myComponent) {
		// special property
		GUILayout.BeginHorizontal();
			// property button
			Type typeMegaMorph = myComponent.GetType();
			try {
				MethodInfo methodGetChannelNames = typeMegaMorph.GetMethod("GetChannelNames");
				MethodInfo methodGetPercentAtIndex = typeMegaMorph.GetMethod("GetPercent",new Type[]{typeof(int)});	// takes index
				MethodInfo methodSetPercentAtIndex = typeMegaMorph.GetMethod("SetPercent",new Type[]{typeof(int),typeof(float)});	// takes index and float
			//Type.GetType("System.Int32") +" "+ 
				//Debug.Log(Type.GetType("System.Single"));
				string[] arrChannelNames = (string[])methodGetChannelNames.Invoke(myComponent, null);
			
				if(GUILayout.Button ("Morph Channels",GUILayout.Width(150f))) {
						// send over component
						// send over method get channel names and set / get percent at index
						processSelectProperty(myComponent, null, null,methodSetPercentAtIndex);
						// old
						// select the field
						//aData.fieldInfo = fieldInfo;
						//aData.propertyInfo = null;
						//aData.propertyComponent = myComponent;
						// exit by setting aData to null
						//aData.didSelectProperty = true;
				}
				string channelNames = "";
				for(int j=0;j<arrChannelNames.Length;j++) {
					channelNames += arrChannelNames[j]+": ";
					channelNames += (float)methodGetPercentAtIndex.Invoke(myComponent, new object[]{j});
					if(j<arrChannelNames.Length-1) channelNames += ", ";
					// temp set value, this is how to set the percent
					//methodSetPercentAtIndex.Invoke(myComponent, new object[]{j,0});
				}
				GUILayout.Label(channelNames);
			} catch {
				GUILayout.Label("Error: There was a problem loading morphs.");
			}
		GUILayout.EndHorizontal();	
	}
	bool shouldIgnoreProperty(string propertyName) {
		foreach (string s in ignoreProperties) {
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
	
	void processSelectProperty(Component propertyComponent, FieldInfo fieldInfo, PropertyInfo propertyInfo, MethodInfo methodMegaMorphSetPercent=null) {
		
		if(!aData || !track) this.Close();
		
		bool changePropertyValue = true;
		if((track.keys.Count > 0)&&(!EditorUtility.DisplayDialog("Data Will Be Lost","You will lose all of the keyframes on track '"+track.name+"' if you continue.", "Continue Anway","Cancel"))) {
			changePropertyValue = false;
		}
		if(changePropertyValue) {
			
			AMTimeline.registerUndo("Set Property");
			// delete keys
			if(track.keys.Count > 0) {
				track.deleteAllKeys();
				AMCodeView.refresh();
			}
			// set fieldinfo or propertyinfo
			if(fieldInfo != null) 
				track.setFieldInfo(fieldInfo);
			else if (propertyInfo != null) 
				track.setPropertyInfo(propertyInfo);
			// MegaMorph
			else if(methodMegaMorphSetPercent != null) 
				track.setMethodInfo(methodMegaMorphSetPercent, new string[]{"System.Int32","System.Single"}, AMPropertyTrack.ValueType.MorphChannels);
			// set component
			track.setComponent(propertyComponent);
			track.updateCache();
		}
		this.Close();
	}
}
