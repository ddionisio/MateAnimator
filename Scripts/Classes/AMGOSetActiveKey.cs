using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace MateAnimator{
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

	        //active won't really be set, it's just a filler along with ease
	        seq.Insert(new AMActionGOActive(this, seq.take.frameRate, go, setActive));
	    }
	    #endregion
	}
}
