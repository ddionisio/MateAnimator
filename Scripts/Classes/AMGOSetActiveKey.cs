using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
	[AddComponentMenu("")]
	public class AMGOSetActiveKey : AMKey {
	    public bool setActive;

	    public int endFrame;

	    public override void destroy() {
	        base.destroy();
	    }
	    // copy properties from key
	    public override void CopyTo(AMKey key) {
			AMGOSetActiveKey a = key as AMGOSetActiveKey;
	        a.enabled = false;
	        a.frame = frame;
	        a.setActive = setActive;
	    }

	    #region action
	    public override int getNumberOfFrames(int frameRate) {
	        return endFrame == -1 ? 1 : endFrame - frame;
	    }

	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
			GameObject go = target as GameObject;

	        if(go == null) return;

            var tween = DOTween.To(new AMPlugValueSet<bool>(), () => setActive, (x) => go.SetActive(x), setActive, getTime(seq.take.frameRate));
            tween.plugOptions = new AMPlugValueSetOptions(seq.sequence);
	        seq.Insert(this, tween);
	    }
	    #endregion
	}
}
