using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.CustomPlugins;

namespace M8.Animator {
	[AddComponentMenu("")]
	public class AMRotationKey : AMKey {

	    //public int type = 0; // 0 = Rotate To, 1 = Look At
	    public Quaternion rotation;

	    public int endFrame;

	    // copy properties from key
	    public override void CopyTo(AMKey key) {
            base.CopyTo(key);

            AMRotationKey a = key as AMRotationKey;
	        
            //a.type = type;
            a.rotation = rotation;
	    }

	    #region action
	    public override int getNumberOfFrames(int frameRate) {
	        if(!canTween && (endFrame == -1 || endFrame == frame))
	            return 1;
	        else if(endFrame == -1)
	            return -1;
	        return endFrame - frame;
	    }
	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
            Transform trans = obj as Transform;
	        int frameRate = seq.take.frameRate;

	        //allow tracks with just one key
	        if(track.keys.Count == 1)
	            interp = (int)Interpolation.None;

			if(!canTween) {
                var tween = DOTween.To(new AMPlugValueSet<Quaternion>(), () => rotation, (x) => trans.localRotation=x, rotation, getTime(frameRate));
                tween.plugOptions = new AMPlugValueSetOptions(seq.sequence);

                seq.Insert(this, tween);
			}
			else if(endFrame == -1) return;
	        else {
	            Quaternion endRotation = (track.keys[index + 1] as AMRotationKey).rotation;

                var tween = DOTween.To(new PureQuaternionPlugin(), () => trans.localRotation, (x) => trans.localRotation=x, endRotation, getTime(frameRate));

                if(hasCustomEase())
                    tween.SetEase(easeCurve);
                else
                    tween.SetEase((Ease)easeType, amplitude, period);

                seq.Insert(this, tween);
	        }
	    }
	    #endregion

	}
}
