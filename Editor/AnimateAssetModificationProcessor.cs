using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace M8.Animator.Edit {
    public class AnimateAssetModificationProcessor : UnityEditor.AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            //clear out editor caches that shouldn't be serialized (ex: material preview)
            if(TimelineWindow.window) {
                var dat = TimelineWindow.window.aData;
                if(dat != null)
                    dat.ClearEditCache();
            }

            return paths;
        }
    }
}