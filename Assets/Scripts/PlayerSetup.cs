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
			// disable the camera and the listeners
			GameObject playercameraObj = gameObject.transform.FindChild ("PlayerCamera").gameObject;
			Camera playercamera = playercameraObj.GetComponent<Camera>();
			GvrAudioListener gvrListener = playercameraObj.GetComponent<GvrAudioListener>();
			AudioListener listener = playercameraObj.GetComponent<AudioListener>();
			playercamera.enabled = false;
			gvrListener.enabled = false;
			listener.enabled = false;


//			if (isServer) {
//				GameObject maincamera = GameObject.FindGameObjectWithTag ("MainCamera");
//				maincamera.transform.parent = gameObject.transform;
//			}
		}
	}

	public override void OnStartLocalPlayer() {

		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";

		//Make sure the MainCamera is the one for our local player
		Camera currentMainCamera = Camera.main;
		Camera playercamera = gameObject.transform.FindChild ("PlayerCamera").gameObject.GetComponent<Camera> ();
		playercamera.tag = "MainCamera";
		currentMainCamera.enabled = false;
		playercamera.enabled = true;


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
