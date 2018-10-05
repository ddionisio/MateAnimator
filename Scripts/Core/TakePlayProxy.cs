using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    /// <summary>
    /// Convenience for playing a take of animator through hook-ups
    /// </summary>
    [AddComponentMenu("M8/Animate Helper/Take Play Proxy")]
    public class TakePlayProxy : MonoBehaviour {
        public Animate animator;

        [TakeSelector(animatorField = "animator")]
        public string take;

        public bool resetTakeOnEnable;

        private int mTakeInd;

        public void Invoke() {
            animator.Play(mTakeInd);
        }

        void OnEnable() {
            if(resetTakeOnEnable)
                animator.ResetTake(mTakeInd);
        }

        void Awake() {
            mTakeInd = animator.GetTakeIndex(take);
        }
    }
}