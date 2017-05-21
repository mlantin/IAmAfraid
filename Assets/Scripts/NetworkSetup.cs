using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.VR;


public class NetworkSetup : MonoBehaviour {

	public Canvas m_inputCanvas;
	public GameObject m_inputManager;
	public GameObject m_UICamera;
	public GameObject m_holojam;

	bool m_networkstarted = false;
	bool m_host = false;

	// Use this for initialization
	void Start () {
//		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
//		NetworkManager.singleton.StartHost();
//		Debug.Log("starting host");
//		#elif UNITY_ANDROID 
//		NetworkManager.singleton.StartClient();
//		Debug.Log("starting client");
//		#endif
	}

	public void setMocap(bool mocap) {
		if (mocap)
			m_holojam.SetActive (true);
		else
			m_holojam.SetActive (false);

		IAAPlayer.localPlayer.playerTracked = mocap;
	}

	public void setServerIP(string ip) {
		NetworkManager.singleton.networkAddress = ip;
	}

	public void startNetwork(bool host) {
		m_host = host;
		m_inputCanvas.enabled = false;
		m_UICamera.SetActive (false);
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		if (host) { // We are starting in host mode
			Debug.Log ("Host Mode");
			NetworkManager.singleton.StartHost();
		} else {
			Debug.Log ("Client Mode");
			NetworkManager.singleton.StartClient();
		}
		m_inputManager.SetActive (true);
		#elif UNITY_ANDROID
		StartCoroutine (LoadDevice ("daydream"));
		#endif
	}

	IEnumerator LoadDevice(string newDevice)
	{
		m_networkstarted = false;
		VRSettings.LoadDeviceByName(newDevice);
		m_inputManager.SetActive (true);
		yield return null;
		VRSettings.enabled = true;
		yield return new WaitUntil (() => VRSettings.isDeviceActive == true);
	}

	void OnApplicationPause (bool paused)  {
		if (!paused && !m_networkstarted && VRSettings.enabled) {
			if (m_host) { // We are starting in host mode
				Debug.Log ("Host Mode");
				NetworkManager.singleton.StartHost ();
			} else {
				Debug.Log ("Client Mode with host id " + NetworkManager.singleton.networkAddress);
				NetworkManager.singleton.StartClient ();
			}
		}
	}
}
