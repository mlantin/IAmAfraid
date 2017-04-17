using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class IAANetworkManager : NetworkManager {

	// Use this for initialization
	void Start () {
		
	}

	public override void OnStartServer(){
		Debug.Log ("Do server stuff here");
	}

}
