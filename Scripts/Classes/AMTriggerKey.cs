using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;

public struct AMTriggerData {
    public string valueString;
    public int valueInt;
    public float valueFloat;
}

public class AMTriggerKey : AMKey {

    public string valueString;
    public int valueInt;
    public float valueFloat;

    // copy properties from key
    public override void CopyTo(AMKey key) {

        AMTriggerKey a = key as AMTriggerKey;
        a.enabled = false;
        a.frame = frame;
        a.valueString = valueString;
        a.valueInt = valueInt;
        a.valueFloat = valueFloat;
    }

    #region action

    public override void build(AMSequence seq, AMTrack track, UnityEngine.Object obj) {
        seq.sequence.InsertCallback(getWaitTime(seq.take.frameRate, 0.0f), seq.triggerCallback,
            this,
            new AMTriggerData() { valueString=this.valueString, valueInt=this.valueInt, valueFloat=this.valueFloat });
    }

    #endregion
}