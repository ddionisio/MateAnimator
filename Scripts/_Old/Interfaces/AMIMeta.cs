using UnityEngine;

namespace M8.Animator {
	public interface AMIMeta {
	    AnimatorMeta meta { get; set; }
	    GameObject dataHolder { get; set; }
	}
}