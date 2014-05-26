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

    public override Tweener buildTweener(AMITarget itarget, AMTrack track, UnityEngine.Object target, Sequence sequence, int frameRate) {
        sequence.InsertCallback(getWaitTime(frameRate, 0.0f), itarget.TargetGetTriggerCallback(),
            new AMTriggerData() { valueString=this.valueString, valueInt=this.valueInt, valueFloat=this.valueFloat });
        return null;
    }

    #endregion
}