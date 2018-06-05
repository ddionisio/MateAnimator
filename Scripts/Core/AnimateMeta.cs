using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    public class AnimateMeta : ScriptableObject, ISerializationCallbackReceiver {
        [SerializeField]
        List<Take> takeData;

        [SerializeField]
        SerializeData serializeData;

        public List<Take> takes { get { return takeData; } }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            serializeData = new SerializeData();
            serializeData.Serialize(takeData);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            serializeData.Deserialize(takeData);
        }
    }
}