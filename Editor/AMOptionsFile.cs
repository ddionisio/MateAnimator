using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class AMOptionsFile : ScriptableObject {
	
	// options
	public string skin = "am_skin_dark";
	public bool time_numbering = false;
	public bool scrubby_zoom_cursor = true;
	public bool scrubby_zoom_slider = false;
	public bool ignoreMinSize = false;
	public bool showWarningForLostReferences = true;
	public bool disableTimelineActions = false;
	public bool disableTimelineActionsTooltip = false;
	public bool showFramesForCollapsedTracks = true;
	
	// quick add combos
	public List<List<int>> quickAdd_Combos = new List<List<int>>();
	public List<int> quickAdd_Combos_Flattened;
	
	private bool unflattened = false;
	
	// foldout
	public List<bool> foldout = new List<bool>();
	
	public bool setSkin(string _skin) {
		if(skin != _skin) {
			skin = _skin;
			return true;
		}
		return false;
	}
	
	public bool setIgnoreMinimumSizeWarning(bool _ignoreMinSize) {
		if(ignoreMinSize != _ignoreMinSize) {
			ignoreMinSize = _ignoreMinSize;
			return true;
		}
		return false;
	}
	
	public bool setTimeNumbering(bool _time_numbering) {
		if(time_numbering != _time_numbering) {
			time_numbering = _time_numbering;
			return true;
		}
		return false;
	}
	
	public bool setScrubbyZoomSlider(bool _scrubby_zoom_slider) {
		if(scrubby_zoom_slider != _scrubby_zoom_slider) {
			scrubby_zoom_slider = _scrubby_zoom_slider;
			return true;
		}
		return false;
	}
	public bool setScrubbyZoomCursor(bool _scrubby_zoom_cursor) {
		if(scrubby_zoom_cursor != _scrubby_zoom_cursor) {
			scrubby_zoom_cursor = _scrubby_zoom_cursor;
			return true;
		}
		return false;
	}
	
	public bool setShowWarningForLostReferences(bool showWarningForLostReferences) {
		if(this.showWarningForLostReferences != showWarningForLostReferences) {
			this.showWarningForLostReferences = showWarningForLostReferences;
			return true;
		}
		return false;
	}
	
	public bool setDisableTimelineActions(bool disableTimelineActions) {
		if(this.disableTimelineActions != disableTimelineActions) {
			this.disableTimelineActions = disableTimelineActions;
			return true;
		}
		return false;
	}
	
	public bool setDisableTimelineActionsTooltip(bool disableTimelineActionsTooltip) {
		if(this.disableTimelineActionsTooltip != disableTimelineActionsTooltip) {
			this.disableTimelineActionsTooltip = disableTimelineActionsTooltip;
			return true;
		}
		return false;
	}
	
	public bool setShowFramesForCollapsedTracks(bool showFramesForCollapsedTracks) {
		if(this.showFramesForCollapsedTracks != showFramesForCollapsedTracks) {
			this.showFramesForCollapsedTracks = showFramesForCollapsedTracks;
			return true;
		}
		return false;
	}
	
	public void checkForFoldout(int index) {
		while(foldout.Count-1 < index) {
			foldout.Add(true);	
		}
	}
	
	#region quickadd combinations
	public void flatten_quickAdd_Combos() {
		quickAdd_Combos_Flattened = new List<int>();
		foreach(List<int> combo in quickAdd_Combos) {
			foreach(int track in combo) {
				quickAdd_Combos_Flattened.Add(track);	
			}
			quickAdd_Combos_Flattened.Add(-1);
		}
	}
	
	private void unflatten_quickAdd_Combos(bool forceUnflatten = false) {
		if(unflattened && !forceUnflatten) return;
		quickAdd_Combos = new List<List<int>>();
		if(quickAdd_Combos_Flattened.Count <= 0) return;
		List<int> temp = new List<int>();
		foreach(int data in quickAdd_Combos_Flattened) {
			if(data == -1) {
				quickAdd_Combos.Add(temp);
				temp = new List<int>();
			} else {
				temp.Add(data);
			}
		}
		unflattened = true;
	}
	
	public bool setQuickAddCombo(int index1, int index2, int value) {
		if(quickAdd_Combos[index1][index2] != value) {
			quickAdd_Combos[index1][index2] = value;
			return true;
		}
		return false;
	}
	
	#endregion
	// load file
    public const string filePath = "Assets/-am_options.asset";
	
	public static AMOptionsFile loadFile() {
        AMOptionsFile load_file = (AMOptionsFile)Resources.LoadAssetAtPath(filePath, typeof(AMOptionsFile));
		if(load_file) {
			load_file.unflatten_quickAdd_Combos(true);
			return load_file;
		}

		AMOptionsFile a = ScriptableObject.CreateInstance<AMOptionsFile>();
		AssetDatabase.CreateAsset(a, filePath);
		a.quickAdd_Combos.Add(new List<int> {(int)AMTimeline.Track.Translation,(int)AMTimeline.Track.Orientation});
		a.quickAdd_Combos.Add(new List<int> {(int)AMTimeline.Track.Translation,(int)AMTimeline.Track.Rotation,(int)AMTimeline.Track.Animation});
		a.flatten_quickAdd_Combos();
		AssetDatabase.Refresh();
		return a;
	}
	
	public static void export() {
		string newPath = EditorUtility.SaveFilePanel("Export Options", Application.dataPath, "am_options_export", "unitypackage");
		if(newPath.Length == 0) return;
		AssetDatabase.ExportPackage(filePath, newPath,ExportPackageOptions.Interactive);
	}
	
	

}
 