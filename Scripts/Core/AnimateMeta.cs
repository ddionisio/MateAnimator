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

        public bool isSerializing { get; private set; }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            isSerializing = true;

            serializeData = new SerializeData();
            serializeData.Serialize(takeData);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            serializeData.Deserialize(takeData);

            isSerializing = false;
        }
    }
}