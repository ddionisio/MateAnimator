using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

namespace MateAnimator{
	[AddComponentMenu("")]
	public class AMRotationEulerKey : AMKey {

	    public Vector3 rotation;

	    public int endFrame;

	    // copy properties from key
	    public override void CopyTo(AMKey key) {
	        AMRotationEulerKey a = key as AMRotationEulerKey;
	        a.enabled = false;
	        a.frame = frame;
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
	        Transform target = obj as Transform;

	        int frameRate = seq.take.frameRate;

	        //allow tracks with just one key
	        if(track.keys.Count == 1)
	            interp = (int)Interpolation.None;

	        if(!canTween) {
	            switch((track as AMRotationEulerTrack).axis) {
	                case AMRotationEulerTrack.Axis.X:
	                    seq.Insert(new AMActionTransLocalRotEulerX(this, frameRate, target, rotation.x));
	                    break;
	                case AMRotationEulerTrack.Axis.Y:
	                    seq.Insert(new AMActionTransLocalRotEulerY(this, frameRate, target, rotation.y));
	                    break;
	                case AMRotationEulerTrack.Axis.Z:
	                    seq.Insert(new AMActionTransLocalRotEulerZ(this, frameRate, target, rotation.z));
	                    break;
	                default:
	                    seq.Insert(new AMActionTransLocalRotEuler(this, frameRate, target, rotation));
	                    break;
	            }
	        }
	        else if(endFrame == -1) return;
	        else {
	            Vector3 endRotation = (track.keys[index + 1] as AMRotationEulerKey).rotation;

	            TweenParms tParms = new TweenParms();

	            switch((track as AMRotationEulerTrack).axis) {
	                case AMRotationEulerTrack.Axis.X:
	                    tParms = tParms.Prop("rotation", new AMPlugToTransformLocalEulerX(target, endRotation.x));
	                    break;
	                case AMRotationEulerTrack.Axis.Y:
	                    tParms = tParms.Prop("rotation", new AMPlugToTransformLocalEulerY(target, endRotation.y));
	                    break;
	                case AMRotationEulerTrack.Axis.Z:
	                    tParms = tParms.Prop("rotation", new AMPlugToTransformLocalEulerZ(target, endRotation.z));
	                    break;
	                default:
	                    tParms = tParms.Prop("rotation", new AMPlugToTransformLocalEuler(target, endRotation));
	                    break;
	            }

	            if(hasCustomEase())
	                tParms = tParms.Ease(easeCurve);
	            else
	                tParms = tParms.Ease((EaseType)easeType, amplitude, period);

	            seq.Insert(this, HOTween.To(this, getTime(frameRate), tParms));
	        }
	    }

	    //special plugin to grab actual rotation value from key and directly set them from given Transform
	    private class AMPlugToTransformLocalEuler : Holoville.HOTween.Plugins.Core.PlugVector3 {
	        private Transform mTrans;

	        public AMPlugToTransformLocalEuler(Transform t, Vector3 endRot)
	            : base(endRot) {
	            mTrans = t;
	        }

	        protected override void SetValue(Vector3 p_value) {
	            mTrans.localEulerAngles = p_value;
	        }
	    }

	    private class AMPlugToTransformLocalEulerX : PlugVector3X {
	        private Transform mTrans;

	        public AMPlugToTransformLocalEulerX(Transform t, float end)
	            : base(end) {
	            mTrans = t;
	        }

	        protected override object GetValue() {
	            Vector3 r = mTrans.localEulerAngles;
	            return new Vector3(((Vector3)base.GetValue()).x, r.y, r.z);
	        }

	        protected override void SetValue(Vector3 p_value) {
	            mTrans.localEulerAngles = p_value;
	        }
	    }

	    private class AMPlugToTransformLocalEulerY : PlugVector3Y {
	        private Transform mTrans;

	        public AMPlugToTransformLocalEulerY(Transform t, float end)
	            : base(end) {
	            mTrans = t;
	        }

	        protected override object GetValue() {
	            Vector3 r = mTrans.localEulerAngles;
	            return new Vector3(r.x, ((Vector3)base.GetValue()).y, r.z);
	        }

	        protected override void SetValue(Vector3 p_value) {
	            mTrans.localEulerAngles = p_value;
	        }
	    }

	    private class AMPlugToTransformLocalEulerZ : PlugVector3X {
	        private Transform mTrans;

	        public AMPlugToTransformLocalEulerZ(Transform t, float end)
	            : base(end) {
	            mTrans = t;
	        }

	        protected override object GetValue() {
	            Vector3 r = mTrans.localEulerAngles;
	            return new Vector3(r.x, r.y, ((Vector3)base.GetValue()).z);
	        }

	        protected override void SetValue(Vector3 p_value) {
	            mTrans.localEulerAngles = p_value;
	        }
	    }
	    #endregion

	}
}
