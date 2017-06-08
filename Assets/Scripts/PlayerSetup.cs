using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using HighlightingSystem;

public class PlayerSetup : NetworkBehaviour {
	public GameObject m_InputManager;
	public bool m_cycleCameras = true;
	public Camera m_observerCamera;
	[SyncVar]
	public bool m_observer = false;

	private WaitForSeconds m_waitforit = new WaitForSeconds(10);
	static private List<Camera> m_playerCameras = new List<Camera> ();
	static private int m_nextCamera = 0;

	void Awake() {
		m_InputManager = GameObject.Find ("IAAInputManager");
	}
		
	// Use this for initialization
	void Start () {
		Debug.Log ("In Start");
		if (!isLocalPlayer) {
			gameObject.tag = "RemotePlayer";
			// disable the camera and the listeners
			GameObject playercameraObj = gameObject.transform.Find ("PlayerCamera").gameObject;
			Camera playercamera = playercameraObj.GetComponent<Camera>();
			playercamera.enabled = false;

			//Add the laser drawing script
			GameObject controllerPointer = gameObject.transform.Find ("GvrControllerPointer").gameObject;
			GameObject laser = controllerPointer.transform.Find ("Laser").gameObject;
			LaserRender laserScript = laser.AddComponent<LaserRender> ();
			GameObject reticle = laser.transform.Find ("Reticle").gameObject;
			laserScript.reticle = reticle;
		}
		if (m_observer) {
			// Make the avatar invisible
			gameObject.transform.Find ("PlayerCamera/Gamer").gameObject.SetActive (false);
			gameObject.transform.Find ("GvrControllerPointer/Controller/ddcontroller").gameObject.SetActive (false);
			// Disable tracking
			ViconActor tracking = gameObject.GetComponent<ViconActor> ();
			tracking.track = false;
		}
	}

	public override void OnStartServer() {
		Debug.Log ("in OnStartServer");

		GameObject playerCameraObj = gameObject.transform.Find ("PlayerCamera").gameObject;
		Camera playercamera = playerCameraObj.GetComponent<Camera>();
		m_playerCameras.Add (playercamera);
		Debug.Log ("There are now " + m_playerCameras.Count + " cameras");
		// add the highlighter
		HighlightingRenderer hlrender = playerCameraObj.AddComponent<HighlightingRenderer>();
		hlrender.LoadPreset ("Speed");
	}

	public override void OnStartLocalPlayer() {
		Debug.Log ("In OnStartLocalPlayer");
		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";

		//Make sure the MainCamera is the one for our local player
		Camera currentMainCamera = Camera.main;
		GameObject playerCameraObj = gameObject.transform.Find ("PlayerCamera").gameObject;
		Camera playercamera = playerCameraObj.GetComponent<Camera> ();
		playercamera.tag = "MainCamera";
		if (currentMainCamera != null)
			currentMainCamera.enabled = false;
		playercamera.enabled = true;

		// Set the position of the server player to be the eye of god.
		// Also disable tracking
		if (isServer && LocalPlayerOptions.singleton.observer) {
			m_observer = true;
			if (LocalPlayerOptions.singleton.god) {
//			gameObject.transform.position = new Vector3 (0, 1.6f, 0);
				gameObject.transform.position = new Vector3 (0, 3, -.5f);
				gameObject.transform.Rotate (35, 0, 0);
			} 
				
		} else {
		// Add the Highlighter, audiolistener, GvrAudioListener, and GvrPointerPhysicsRaycaster scripts to this object
			HighlightingRenderer hlrender = playerCameraObj.GetComponent<HighlightingRenderer>();
			if (hlrender == null) {
				hlrender = playerCameraObj.AddComponent<HighlightingRenderer> ();
				hlrender.LoadPreset ("Speed");
			}
			ViconActor tracking = gameObject.GetComponent<ViconActor> ();
			if (LocalPlayerOptions.singleton.trackLocalPlayer) {
				// Turn on Holojam
				Debug.Log("Turning on Holojam");
				GameObject holojam = GameObject.FindGameObjectWithTag ("Holojam");
				holojam.SetActive (true);
				tracking.track = true;
				tracking.SetLabel(LocalPlayerOptions.singleton.mocapName);
			} else {
				tracking.track = false;
			}
		}

		playerCameraObj.AddComponent<AudioListener>();
		playerCameraObj.AddComponent<GvrAudioListener> ();
		playerCameraObj.AddComponent<GvrPointerPhysicsRaycaster> ();

		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		GameObject controllerPointer = gameObject.transform.Find ("GvrControllerPointer").gameObject;
		IAAInputManager inputscript = m_InputManager.GetComponent<IAAInputManager>();
		inputscript.controllerPointer = controllerPointer;
		inputscript.whatAreWe ();
		inputscript.SetVRInputMechanism ();

		// Add the GvrArmModelOffset scripts to the controller and laser
		GameObject controller = controllerPointer.transform.Find("Controller").gameObject;
		GvrArmModelOffsets controllerscript = controller.AddComponent<GvrArmModelOffsets> ();
		controllerscript.joint = GvrArmModelOffsets.Joint.Wrist;
		GameObject laser = controllerPointer.transform.Find ("Laser").gameObject;
		GvrArmModelOffsets laserscript = laser.AddComponent<GvrArmModelOffsets> ();
		laserscript.joint = GvrArmModelOffsets.Joint.Pointer;

		// Add the GvrLaserPointer script to the laser object
		GvrLaserPointer laserPtrScript = laser.AddComponent<GvrLaserPointer> ();
		laserPtrScript.maxReticleDistance = 4.0f;
		GameObject reticle = laser.transform.Find ("Reticle").gameObject;
		laserPtrScript.reticle = reticle;
		#endif

		if (isServer && m_observer) {
			// Make the avatar invisible
			gameObject.transform.Find("PlayerCamera/Gamer").gameObject.SetActive(false);
			gameObject.transform.Find ("GvrControllerPointer/Controller/ddcontroller").gameObject.SetActive (false);
			if (m_cycleCameras) {
				// Remove the server camera (the first one) and save it.
				m_observerCamera = m_playerCameras [0];
				m_playerCameras.RemoveAt (0);
				StartCoroutine (cycleThroughCameras ());
			}
		}
	}
		
	public override void OnNetworkDestroy() {
		if (isServer && IAAPlayer.localPlayer.isObserver) {
			Camera playerCamera = gameObject.transform.Find ("PlayerCamera").gameObject.GetComponent<Camera> ();
			if (m_playerCameras.Count == 1)
				switchCamera (IAAPlayer.localPlayer.transform.Find("PlayerCamera").gameObject.GetComponent<Camera>());
			else if (Camera.main == playerCamera) {
				switchCamera (m_playerCameras [m_nextCamera]);
				m_nextCamera = (m_nextCamera + 1) % (m_playerCameras.Count-1);
			} else if (m_nextCamera == (m_playerCameras.Count - 1)) {
				m_nextCamera--;	
			}
			m_playerCameras.Remove (playerCamera);
		}
	}

	IEnumerator cycleThroughCameras() {
		while (true) {
			if (m_playerCameras.Count == 0)
				yield return m_waitforit;
			else {
				if (m_nextCamera == 0 && LocalPlayerOptions.singleton.god && Camera.main != m_observerCamera)
					switchCamera (m_observerCamera);
				else {
					switchCamera (m_playerCameras [m_nextCamera]);
					m_nextCamera = (m_nextCamera + 1) % m_playerCameras.Count;
				}

			}
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
}
