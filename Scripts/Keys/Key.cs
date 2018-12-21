using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

namespace M8.Animator {
    public abstract class Key {
        public abstract SerializeType serializeType { get; }

        public enum Interpolation {
            Curve = 0,
            Linear = 1,
            None = 2
        }
                
        public int version = 0; //for upgrading/initializing

        public Interpolation interp = Interpolation.Curve;     // interpolation

        public int frame;
        public Ease easeType = Ease.Linear;//AMTween.EaseType.linear; 			// ease type, AMTween.EaseType enum
                                               //-1 = None
        public float amplitude = 0.0f;
        public float period = 0.0f;

        public List<float> customEase = new List<float>();
                
        public virtual bool canTween { get { return interp != Interpolation.None; } }

        private AnimationCurve _cachedEaseCurve;
        public AnimationCurve easeCurve {
            get {
                if(_cachedEaseCurve == null || _cachedEaseCurve.keys.Length <= 0) _cachedEaseCurve = getCustomEaseCurve();
                return _cachedEaseCurve;
            }
            set {
                _cachedEaseCurve = value;
            }
        }

        public virtual void destroy() {
            
        }

        public virtual void maintainKey(ITarget itarget, UnityEngine.Object targetObj) {
        }

        public virtual void CopyTo(Key key) {
            key.interp = interp;
            key.frame = frame;
            key.easeType = easeType;

            if(customEase != null)
                key.customEase = new List<float>(customEase);
            else
                key.customEase = null;
        }
        
        /// <summary>
        /// Use sequence to insert callbacks, or some other crap, just don't insert the tweener you are returning!
        /// target is set if required. index = this key's index in the track
        /// </summary>
        public virtual void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            Debug.LogError("Animator: No override for build.");
        }

        public float getWaitTime(int frameRate, float delay) {
            return (frame - 1f) / frameRate + delay;
        }

        public virtual int getStartFrame() {
            return frame;
        }

        public virtual int getNumberOfFrames(int frameRate) {
            return 0;
        }

        public float getTime(int frameRate) {
            return getNumberOfFrames(frameRate) / (float)frameRate;
        }

        public virtual AnimateTimeline.JSONAction getJSONAction(int frameRate) {
            return null;
        }

        public void setCustomEase(AnimationCurve curve) {
            customEase = new List<float>();
            foreach(Keyframe k in curve.keys) {
                customEase.Add(k.time);
                customEase.Add(k.value);
                customEase.Add(k.inTangent);
                customEase.Add(k.outTangent);
            }
        }

        public AnimationCurve getCustomEaseCurve() {
            if(customEase.Count == 0) {
                return new AnimationCurve();
            }
            if(customEase.Count % 4 != 0) {
                Debug.LogError("Animator: Error retrieving custom ease.");
                return new AnimationCurve();
            }

            AnimationCurve curve = new AnimationCurve();

            for(int i = 0; i < customEase.Count; i += 4) {
                curve.AddKey(new Keyframe(customEase[i], customEase[i + 1], customEase[i + 2], customEase[i + 3]));
            }
            return curve;
        }

        public bool hasCustomEase() {
            if(easeType == Ease.INTERNAL_Custom) return true;
            return false;
        }

        public bool setEaseType(Ease easeType) {
            if(easeType != this.easeType) {
                this.easeType = easeType;
                if(easeType == Ease.INTERNAL_Custom && customEase.Count <= 0) {
                    // set up default custom ease with linear
                    customEase = new List<float>() {
                        0f,0f,1f,1f,
                        1f,1f,1f,1f
                    };
                }
                return true;
            }
            return false;
        }
    }
}