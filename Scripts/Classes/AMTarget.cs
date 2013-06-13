using UnityEngine;
using System.Collections;

public class AMTarget : MonoBehaviour {
	public float gizmo_size = 0.1f;
	void OnDrawGizmos() {
		Gizmos.color = new Color(245f/255f,107f/255f,30f/255f,1f);
		Gizmos.DrawSphere(transform.position, 0.2f * (gizmo_size/0.1f));
	}
}
