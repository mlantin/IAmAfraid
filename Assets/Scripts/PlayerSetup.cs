using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using HighlightingSystem;

public class PlayerSetup : NetworkBehaviour {
	public GameObject m_InputManager;
	public bool m_cycleCameras = true;

	private WaitForSeconds m_waitforit = new WaitForSeconds(10);
	static private List<Camera> m_playerCameras = new List<Camera> ();
	static private int m_nextCamera = 0;

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
			GameObject laser = controllerPointer.transform.FindChild ("Laser").gameObject;
			LaserRender laserScript = laser.AddComponent<LaserRender> ();
			GameObject reticle = laser.transform.FindChild ("Reticle").gameObject;
			laserScript.reticle = reticle;
		}

	}

	public override void OnStartServer() {
		GameObject playercameraObj = gameObject.transform.FindChild ("PlayerCamera").gameObject;
		Camera playercamera = playercameraObj.GetComponent<Camera>();
		m_playerCameras.Add (playercamera);
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

		// Set the position of the server player to be the eye of god.
		// Also disable tracking
		if (isServer) {
//			gameObject.transform.position = new Vector3 (0, 1.6f, 0);
			gameObject.transform.position = new Vector3 (0, 3, -.5f);
			gameObject.transform.Rotate (35, 0, 0);
		}
		// Add the Highlighter, audiolistener, GvrAudioListener, and GvrPointerPhysicsRaycaster scripts to this object
		HighlightingRenderer hlrender = playerCameraObj.AddComponent<HighlightingRenderer>();
		bool result = hlrender.LoadPreset ("Speed");
		if (result)
			Debug.Log ("set it to Speed");
		playerCameraObj.AddComponent<AudioListener>();
		playerCameraObj.AddComponent<GvrAudioListener> ();
		playerCameraObj.AddComponent<GvrPointerPhysicsRaycaster> ();

		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
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
		#endif

		if (isServer) {
			// Make the avatar invisible
			gameObject.transform.Find("PlayerCamera/Gamer").gameObject.SetActive(false);
			gameObject.transform.Find ("GvrControllerPointer/Controller/ddcontroller").gameObject.SetActive (false);
			if (m_cycleCameras)
				StartCoroutine (cycleThroughCameras());
		}
	}
		
	public override void OnNetworkDestroy() {
		if (isServer) {
			Camera playerCamera = gameObject.transform.FindChild ("PlayerCamera").gameObject.GetComponent<Camera> ();
			if (Camera.main == playerCamera) {
				switchCamera (m_playerCameras [m_nextCamera]);
			} else if (m_nextCamera == (m_playerCameras.Count - 1)) {
				m_nextCamera = 0;	
			}
			m_playerCameras.Remove (playerCamera);
		}
	}

	IEnumerator cycleThroughCameras() {
		while (true) {
			if (m_playerCameras.Count == 0)
				yield return m_waitforit;
			switchCamera (m_playerCameras [m_nextCamera]);
			m_nextCamera = (m_nextCamera + 1) % m_playerCameras.Count;
			yield return m_waitforit;
		}
	}

	void switchCamera(Camera camera) {
		if (Camera.main != camera) {
			Camera currentMainCamera = Camera.main;
			camera.tag = "MainCamera";
			if (currentMainCamera != null) {
				currentMainCamera.tag = "Untagged";
				currentMainCamera.enabled = false;
			}
			camera.enabled = true;
		}
	}

	void Update() {
//		if (isLocalPlayer)
//			publisher.relayData (playerOrigin);
	}
}
