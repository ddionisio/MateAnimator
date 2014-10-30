using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

//!DEBUG
[ExecuteInEditMode]
[AddComponentMenu("")]
public class AnimatorDataHolder : MonoBehaviour {
#if UNITY_EDITOR
    bool showError = true;

    void OnDestroy() {
        if(showError) {
            Debug.LogWarning(StackTraceUtility.ExtractStackTrace());
        }

        EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
    }

    void Awake() {
        EditorApplication.playmodeStateChanged += OnPlayModeChanged;
    }

    void OnPlayModeChanged() {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
            showError = false;
    }
#endif
}
