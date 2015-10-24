using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace MateAnimator{
	[AddComponentMenu("")]
	public class AnimatorMeta : MonoBehaviour {
		[SerializeField]
		List<AMTakeData> takeData = new List<AMTakeData>();

	    public List<AMTakeData> takes { get { return takeData; } }
	}
}