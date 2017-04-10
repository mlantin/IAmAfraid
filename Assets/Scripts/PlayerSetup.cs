using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {

	public GameObject m_InputManager;

	void Awake() {
		m_InputManager = GameObject.Find ("IAAInputManager");
	}

	// Use this for initialization
	void Start () {
		if (!isLocalPlayer) {
			gameObject.tag = "RemotePlayer";
//			if (isServer) {
//				GameObject maincamera = GameObject.FindGameObjectWithTag ("MainCamera");
//				maincamera.transform.parent = gameObject.transform;
//			}
		}
	}

	public override void OnStartLocalPlayer() {

		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";

		// Create the player hierarchy. The TrackedPlayer object is the parent of the MainCamera object, which will
		// become the parent of the instantiated player prefab.
		GameObject maincamera = GameObject.FindGameObjectWithTag("MainCamera");
		GameObject trackedplayer = maincamera.transform.parent.gameObject;
		trackedplayer.transform.position = gameObject.transform.position;
		gameObject.transform.parent = maincamera.transform;
		gameObject.transform.localPosition = Vector3.zero;
		//if (!isServer) {
			// We are on a daydream so make sure we are the active GVRController
			// Get the input manager script
			IAAInputManager inputscript = m_InputManager.GetComponent<IAAInputManager>();
			GameObject controller = gameObject.transform.FindChild ("GvrControllerPointer").gameObject;
			inputscript.controllerPointer = controller;
		inputscript.whatAreWe ();
			inputscript.SetVRInputMechanism ();
//		inputscript.SetControllerInputActive (true);
		//}
	}

//	public override void OnStopClient() {
//		// Take back the reticle
//		GameObject reticle = GameObject.Find("Reticle");
//		GetComponent<AuthorityManager> ().CmdRemoveObjectAuthority (reticle.GetComponent<NetworkIdentity> ().netId);
//	}
}
