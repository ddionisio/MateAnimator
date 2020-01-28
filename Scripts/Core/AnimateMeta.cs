using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    public class AnimateMeta : ScriptableObject, ISerializationCallbackReceiver {
        [SerializeField]
        List<Take> takeData = new List<Take>();

        [SerializeField]
        SerializeData serializeData;

        public List<Take> takes { get { return takeData; } }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
#if UNITY_2019_3_OR_NEWER
#else
            serializeData = new SerializeData();
            serializeData.Serialize(takeData);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
#if UNITY_2019_3_OR_NEWER
            if(serializeData != null && !serializeData.isEmpty) {
                serializeData.Deserialize(takeData);
                serializeData = null;
            }
#else
            serializeData.Deserialize(takeData);
#endif
        }
    }
}