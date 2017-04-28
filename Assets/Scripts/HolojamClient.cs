using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolojamClient : MonoBehaviour {

	private Vector3 m_newpos = new Vector3 ();
	private Quaternion m_newrot = new Quaternion();

	// Use this for initialization
	void Start () {
		DataPublisher.On ("PIXEL1", handleMocap);
	}
	
	// Update is called once per frame
	void LateUpdate () {
		transform.position = m_newpos;
		transform.rotation = m_newrot;
	}

	void handleMocap(MocapMsg msg) {
		// the data coming in is OpenGL convention, X Right, Y UP, Z Backward
		// Unity is the same but with Z pointing forward.
		m_newpos.Set(msg.pos.x, msg.pos.y, -msg.pos.z);
		m_newrot.Set(msg.rot.x, msg.rot.y, -msg.rot.z, -msg.rot.w);
	}
}
