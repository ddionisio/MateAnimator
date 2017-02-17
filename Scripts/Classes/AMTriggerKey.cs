using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
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
            base.CopyTo(key);

	        AMTriggerKey a = key as AMTriggerKey;
	                    
	        a.valueString = valueString;
	        a.valueInt = valueInt;
	        a.valueFloat = valueFloat;
	    }

	    #region action

	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
            AMTriggerData parm = new AMTriggerData() { valueString=this.valueString, valueInt=this.valueInt, valueFloat=this.valueFloat };
            var tween = DOTween.To(new AMPlugValueSetElapsed(), () => 0, (x) => seq.Trigger(this, parm), 0, 1.0f/seq.take.frameRate);
            tween.plugOptions.SetSequence(seq);
            seq.Insert(this, tween);
	    }

	    #endregion
	}
}