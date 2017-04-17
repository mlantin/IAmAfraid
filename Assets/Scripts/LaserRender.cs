using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserRender : MonoBehaviour {

	public GameObject reticle;
	/// Color of the laser pointer including alpha transparency
	public Color laserColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
	/// Maximum distance of the pointer (meters).
	[Range(0.0f, 10.0f)]
	public float maxLaserDistance = 0.75f;

	private LineRenderer lineRenderer;

	void Awake() {
		lineRenderer = gameObject.GetComponent<LineRenderer>();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void LateUpdate () {
		lineRenderer.SetPosition(0, transform.position);
		Vector3 endpoint = reticle.transform.position;
		if (Vector3.Distance (transform.position, reticle.transform.position) > maxLaserDistance) {
			Vector3 dir = (reticle.transform.position - transform.position);
			dir.Normalize ();
			endpoint = transform.position + dir * maxLaserDistance;
		}
		lineRenderer.SetPosition (1, endpoint);

		// Adjust transparency
		float alpha = 1.0f;
		lineRenderer.SetColors(Color.Lerp(Color.clear, laserColor, alpha), Color.clear);
	}
}
