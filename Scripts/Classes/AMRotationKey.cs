using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

namespace MateAnimator{
	[AddComponentMenu("")]
	public class AMRotationKey : AMKey {

	    //public int type = 0; // 0 = Rotate To, 1 = Look At
	    public Quaternion rotation;

	    public int endFrame;

	    // copy properties from key
	    public override void CopyTo(AMKey key) {
			AMRotationKey a = key as AMRotationKey;
	        a.enabled = false;
	        a.frame = frame;
	        //a.type = type;
	        a.rotation = rotation;
	        a.easeType = easeType;
	        a.customEase = new List<float>(customEase);
	    }

	    #region action
	    public override int getNumberOfFrames(int frameRate) {
	        if(!canTween || (endFrame == -1 || endFrame == frame))
	            return 1;
	        else if(endFrame == -1)
	            return -1;
	        return endFrame - frame;
	    }
	    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
	        int frameRate = seq.take.frameRate;

	        //allow tracks with just one key
	        if(track.keys.Count == 1)
	            interp = (int)Interpolation.None;

			if(!canTween) {
	            seq.Insert(new AMActionTransLocalRot(this, frameRate, obj as Transform, rotation));
			}
			else if(endFrame == -1) return;
	        else {
                Transform trans = obj as Transform;
	            Quaternion endRotation = (track.keys[index + 1] as AMRotationKey).rotation;

                var tween = DOTween.To(new AMPlugQuaternion(), () => trans.localRotation, (x) => trans.localRotation=x, endRotation, getTime(frameRate));

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
