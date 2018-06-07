using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    [CreateAssetMenu(fileName = "animatorTriggerSignal", menuName = "Signals/Animator Trigger")]
    public class TriggerSignal : ScriptableObject {
        public event System.Action<TriggerData> callback;

        public void Invoke(TriggerData parm) {
            //Debug.Log(string.Format("Invoke: {0} {1} {2}", parm.valueString, parm.valueFloat, parm.valueInt));

            if(callback != null)
                callback(parm);
        }
    }
}