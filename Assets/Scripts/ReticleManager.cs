using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ReticleManager : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		
	}

	// This is a kludge. I have to relinquish the Reticle back to the server so it doesn't get 
	// destroyed when I go away.
	void OnApplicationQuit() {
		if (!isServer && hasAuthority)
			LocalPlayer.removeAuthority (netId);
	}

	void onDestroy() {
		if (!isServer && hasAuthority)
			LocalPlayer.removeAuthority (netId);
	}
}
