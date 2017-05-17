using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViconActor : Holojam.Tools.Trackable {

	public GameObject playerCamera;
	public bool track = false;


	// As an example, expose all the Trackable properties in the inspector.
	// In practice, you probably want to control some or all of these manually in code.

	public string label = "Trackable";
	public string scope = ""; 

	Vector3 viconPos = new Vector3 ();
	Quaternion viconRot = new Quaternion();
	// This is the difference between what the Vicon rotation says we are and what 
	// the daydream head tracking says we are.
	Quaternion rotDiff; 
	public bool rotCorrected = false;

	// As an example, allow all the Trackable properties to be publicly settable
	// In practice, you probably want to control some or all of these manually in code.

	public void SetLabel(string label) { this.label = label; }
	public void SetScope(string scope) { this.scope = scope; }

	// Point the property overrides to the public inspector fields

	public override string Label { get { return label; } }
	public override string Scope { get { return scope; } }

	public override bool Deaf { get { return !track; } }

	protected override void UpdateTracking() {
		if (UpdatedThisFrame) {
			viconPos = TrackedPosition;
			viconRot = TrackedRotation;
			viconPos.Set (viconPos.x, viconPos.y, -viconPos.z);
			viconRot.Set (viconRot.x, viconRot.y, -viconRot.z, -viconRot.w);
			if (!rotCorrected)
				correctRotation ();
//			transform.position = rotDiff*viconPos;
			transform.position = viconPos;
//			transform.rotation = viconRot;
		}
	}

	void correctRotation() {
		//rotDiff = playerCamera.transform.localRotation*Quaternion.Inverse (viconRot);
		rotDiff = playerCamera.transform.localRotation*viconRot;
		Debug.Log ("Rotdiff: " + rotDiff);
		transform.rotation = rotDiff;
		rotCorrected = true;
	}

	void OnDrawGizmos() {
		DrawGizmoGhost();
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.gray;

		// Pivot
		Holojam.Utility.Drawer.Circle(transform.position, Vector3.up, Vector3.forward, 0.18f);
		Gizmos.DrawLine(transform.position - 0.03f * Vector3.left, transform.position + 0.03f * Vector3.left);
		Gizmos.DrawLine(transform.position - 0.03f * Vector3.forward, transform.position + 0.03f * Vector3.forward);

		// Forward
		Gizmos.DrawRay(transform.position, transform.forward * 0.18f);
	}

	// Draw ghost (in world space) if in local space
	protected void DrawGizmoGhost() {
		if (!LocalSpace || transform.parent == null) return;

		Gizmos.color = Color.gray;
		Gizmos.DrawLine(
			RawPosition - 0.03f * Vector3.left,
			RawPosition + 0.03f * Vector3.left
		);
		Gizmos.DrawLine(
			RawPosition - 0.03f * Vector3.forward,
			RawPosition + 0.03f * Vector3.forward
		);
		Gizmos.DrawLine(RawPosition - 0.03f * Vector3.up, RawPosition + 0.03f * Vector3.up);
	}
}
