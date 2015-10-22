using UnityEngine;

namespace MateAnimator{
	public interface AMIMeta {
	    AnimatorMeta meta { get; set; }
	    GameObject dataHolder { get; set; }
	}
}