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
		gameObject.transform.parent = trackedplayer.transform;
		gameObject.transform.localPosition = Vector3.zero;
		// Make only the gamer object child of MainCamera
		GameObject gamer = gameObject.transform.FindChild("Gamer").gameObject;
		gamer.transform.parent = maincamera.transform;


		GameObject controllerPointer = gameObject.transform.FindChild ("GvrControllerPointer").gameObject;
		IAAInputManager inputscript = m_InputManager.GetComponent<IAAInputManager>();
		inputscript.controllerPointer = controllerPointer;
		inputscript.whatAreWe ();
		inputscript.SetVRInputMechanism ();

		// Add the GvrArmModelOffset scripts to the controller and laser
		GameObject controller = controllerPointer.transform.FindChild("Controller").gameObject;
		GvrArmModelOffsets controllerscript = controller.AddComponent<GvrArmModelOffsets> ();
		controllerscript.joint = GvrArmModelOffsets.Joint.Wrist;
		GameObject laser = controllerPointer.transform.FindChild ("Laser").gameObject;
		GvrArmModelOffsets laserscript = laser.AddComponent<GvrArmModelOffsets> ();
		laserscript.joint = GvrArmModelOffsets.Joint.Pointer;

//		inputscript.SetControllerInputActive (true);
		//}
	}

//	public override void OnStopClient() {
//		// Take back the reticle
//		GameObject reticle = GameObject.Find("Reticle");
//		GetComponent<AuthorityManager> ().CmdRemoveObjectAuthority (reticle.GetComponent<NetworkIdentity> ().netId);
//	}
}
