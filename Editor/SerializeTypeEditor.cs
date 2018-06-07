using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace M8.Animator.Edit {
    public struct SerializeTypeEditor {
        
        private static string[] mTrackNames;

        public static string[] TrackNames {
            get {
                if(mTrackNames == null) {
                    var names = System.Enum.GetNames(typeof(SerializeType));

                    mTrackNames = new string[names.Length];

                    for(int i = 0; i < names.Length; i++) {
                        mTrackNames[i] = ObjectNames.NicifyVariableName(names[i]);
                    }
                }

                return mTrackNames;
            }
        }
    }
}