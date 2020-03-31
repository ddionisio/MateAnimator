using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class GOSetActiveKey : Key {
        public override SerializeType serializeType { get { return SerializeType.GOSetActive; } }

        public bool setActive;

        public int endFrame;

        public override void destroy() {
            base.destroy();
        }
        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            var a = key as GOSetActiveKey;

            a.setActive = setActive;
        }

        #region action
        public override int getNumberOfFrames(int frameRate) {
            return endFrame == -1 ? 1 : endFrame - frame;
        }

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            GameObject go = target as GameObject;

            if(go == null) return;

            var tween = DOTween.To(TweenPlugValueSet<bool>.Get(), () => go.activeSelf, (x) => go.SetActive(x), setActive, getTime(seq.take.frameRate));
            seq.Insert(this, tween);
        }
        #endregion
    }
}