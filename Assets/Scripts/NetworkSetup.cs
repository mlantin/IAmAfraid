using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkSetup : MonoBehaviour {

	// Use this for initialization
	void Start () {
		#if UNITY_EDITOR
		NetworkManager.singleton.StartHost();
		Debug.Log("starting host");
		#elif UNITY_ANDROID || UNITY_STANDALONE_OSX
		NetworkManager.singleton.StartClient();
		Debug.Log("starting client");
		#endif
	}
	

}
