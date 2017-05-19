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
	public GameObject m_holojam;

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

		LocalPlayer.singleton.playerTracked = mocap;
	}

	public void setServerIP(string ip) {
		NetworkManager.singleton.networkAddress = ip;
	}

	public void startNetwork(bool host) {
		m_host = host;
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		if (host) { // We are starting in host mode
			Debug.Log ("Host Mode");
			NetworkManager.singleton.StartHost();
		} else {
			Debug.Log ("Client Mode");
			NetworkManager.singleton.StartClient();
		}
		m_inputManager.SetActive (true);
		m_inputCanvas.enabled = false;
		#elif UNITY_ANDROID
		StartCoroutine (LoadDevice ("daydream"));
		#endif
	}

	IEnumerator LoadDevice(string newDevice)
	{
		VRSettings.LoadDeviceByName(newDevice);
		yield return null;
		VRSettings.enabled = true;
		m_inputManager.SetActive (true);
		m_inputCanvas.enabled = false;
		yield return new WaitUntil (() => VRSettings.isDeviceActive == true);
		if (m_host) { // We are starting in host mode
			Debug.Log ("Host Mode");
			NetworkManager.singleton.StartHost();
		} else {
			Debug.Log ("Client Mode with host id " + NetworkManager.singleton.networkAddress);
			NetworkManager.singleton.StartClient();
		}

	}

}
