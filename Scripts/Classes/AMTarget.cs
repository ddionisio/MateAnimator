using UnityEngine;
using System.Collections;

public class AMTarget : MonoBehaviour {
#if UNITY_EDITOR
	void OnDrawGizmos() {
		Gizmos.color = new Color(245f/255f,107f/255f,30f/255f,1f);
		Gizmos.DrawSphere(transform.position, 0.2f * (AnimatorTimeline.e_gizmoSize/0.1f));
	}
#endif
}
