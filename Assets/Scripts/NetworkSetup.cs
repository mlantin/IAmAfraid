using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkSetup : MonoBehaviour {

	// Use this for initialization
	void Start () {
		#if UNITY_ANDROID
		NetworkManager.singleton.StartClient();
		#elif UNITY_EDITOR
		NetworkManager.singleton.StartHost();
		#endif
	}
	

}
