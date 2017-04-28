using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HolojamClient :  NetworkBehaviour {

	public bool m_track = true;

	private GameObject m_worldOrigin;
	private GameObject m_playerCamera;
	private bool m_trackingSpaceSet = false;
	private bool m_havemocap = false;
	private Vector3 m_newpos = new Vector3 ();
	private Quaternion m_newrot = new Quaternion();

	// Use this for initialization
	public override void OnStartLocalPlayer() {	
		m_worldOrigin = GameObject.Find ("WorldOrigin");
		m_playerCamera = transform.FindChild ("PlayerCamera").gameObject;
		if (m_track)
			DataPublisher.On ("PIXEL1", handleMocap);
	}
	
	// Update is called once per frame
	void Update () {
		if (m_havemocap)
			transform.position = m_worldOrigin.transform.rotation*m_newpos;
	}

	void handleMocap(MocapMsg msg) {
		// the data coming in is OpenGL convention, X Right, Y UP, Z Backward
		// Unity is the same but with Z pointing forward.
		m_newpos.Set(msg.pos.x, msg.pos.y, -msg.pos.z);
		m_newrot.Set(msg.rot.x, msg.rot.y, -msg.rot.z, -msg.rot.w);

		if (!m_havemocap) {
			m_havemocap = true;
			if (!m_trackingSpaceSet)
				setTrackingSpace ();
		}
	}

	void setTrackingSpace() {
		Quaternion rotdiff = m_playerCamera.transform.rotation*Quaternion.Inverse (m_newrot);
		m_worldOrigin.transform.rotation = rotdiff;
		m_trackingSpaceSet = true;
	}
}
