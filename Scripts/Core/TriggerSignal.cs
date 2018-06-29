using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    /// <summary>
    /// Convenience signal that can be used for EventTrack, will work much like the old TriggerTrack.
    /// </summary>
    [CreateAssetMenu(fileName = "animatorTriggerSignal", menuName = "Signals/Animator Trigger")]
    public class TriggerSignal : ScriptableObject {
        public event System.Action<string, int, float> callback;

        public void Invoke(string valString, int valInt, float valFloat) {
            //Debug.Log(string.Format("Invoke: {0} {1} {2}", valString, valInt, valFloat));

            if(callback != null)
                callback(valString, valInt, valFloat);
        }
    }
}