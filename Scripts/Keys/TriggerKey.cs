using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace M8.Animator {
    public struct TriggerParam {
        public string valueString;
        public int valueInt;
        public float valueFloat;
    }

    [System.Serializable]
    public class TriggerKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Trigger; } }

        public string valueString;
        public int valueInt;
        public float valueFloat;

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as TriggerKey;

            a.valueString = valueString;
            a.valueInt = valueInt;
            a.valueFloat = valueFloat;
        }

        #region action

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object obj) {
            var parm = new TriggerParam() { valueString = this.valueString, valueInt = this.valueInt, valueFloat = this.valueFloat };
            var tween = DOTween.To(new TweenPlugValueSetElapsed(), () => 0, (x) => seq.Trigger(this, parm), 0, 1.0f / seq.take.frameRate);
            tween.plugOptions.SetSequence(seq);
            seq.Insert(this, tween);
        }

        #endregion
    }
}