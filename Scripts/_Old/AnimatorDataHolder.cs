using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace M8.Animator {
	//!DEBUG
	[ExecuteInEditMode]
	[AddComponentMenu("")]
	public class AnimatorDataHolder : MonoBehaviour {
	#if UNITY_EDITOR
	    bool showError = false;

	    void OnDestroy() {
	        if(showError) {
	            Debug.LogWarning(StackTraceUtility.ExtractStackTrace());
	        }

	        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
	    }

	    void Awake() {
	        EditorApplication.playModeStateChanged += OnPlayModeChanged;
	    }

	    void OnPlayModeChanged(PlayModeStateChange state) {
            if(state == PlayModeStateChange.EnteredPlayMode)
                showError = true;
            else if(state == PlayModeStateChange.EnteredEditMode)
                showError = false;
	    }
	#endif
	}

}