using UnityEngine;
using System.Collections;

//!DEBUG
[ExecuteInEditMode]
[AddComponentMenu("")]
public class AnimatorDataHolder : MonoBehaviour {
    void OnDestroy() {
        if(!Application.isPlaying) {
            Debug.LogWarning(StackTraceUtility.ExtractStackTrace());
            Debug.LogWarning("Animator Data Holder has been deleted.");
        }   
    }
}
