using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkSetup : MonoBehaviour {

	// Use this for initialization
	void Start () {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		NetworkManager.singleton.StartHost();
		Debug.Log("starting host");
		#elif UNITY_ANDROID 
		NetworkManager.singleton.StartClient();
		Debug.Log("starting client");
		#endif
	}
	

}
