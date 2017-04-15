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
			playercamera.enabled = false;

			//Add the laser drawing script
			GameObject controllerPointer = gameObject.transform.FindChild ("GvrControllerPointer").gameObject;
			if (controllerPointer != null)
				Debug.Log ("Found the controller pointer");
			GameObject laser = controllerPointer.transform.FindChild ("Laser").gameObject;
			LaserRender laserScript = laser.AddComponent<LaserRender> ();
			GameObject reticle = laser.transform.FindChild ("Reticle").gameObject;
			laserScript.reticle = reticle;
		}
	}

	public override void OnStartLocalPlayer() {

		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";

		//Make sure the MainCamera is the one for our local player
		Camera currentMainCamera = Camera.main;
		GameObject playerCameraObj = gameObject.transform.FindChild ("PlayerCamera").gameObject;
		Camera playercamera = playerCameraObj.GetComponent<Camera> ();
		playercamera.tag = "MainCamera";
		if (currentMainCamera != null)
			currentMainCamera.enabled = false;
		playercamera.enabled = true;

		// Add the audiolistener, GvrAudioListener, and GvrPointerPhysicsRaycaster scripts to this object
		playerCameraObj.AddComponent<AudioListener>();
		playerCameraObj.AddComponent<GvrAudioListener> ();
		playerCameraObj.AddComponent<GvrPointerPhysicsRaycaster> ();

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

		// Add the GvrLaserPointer script to the laser object
		GvrLaserPointer laserPtrScript = laser.AddComponent<GvrLaserPointer> ();
		GameObject reticle = laser.transform.FindChild ("Reticle").gameObject;
		laserPtrScript.reticle = reticle;
	}
		
}
