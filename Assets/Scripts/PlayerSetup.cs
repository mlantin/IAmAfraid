using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		if (!isLocalPlayer)
			gameObject.tag = "RemotePlayer";
	}

	public override void OnStartLocalPlayer() {
		// reparent the player hierarchy (Main Camera and GVRController) to this playerObject
		GameObject maincamera = GameObject.FindGameObjectWithTag("MainCamera");
		this.gameObject.transform.parent = maincamera.transform;

		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";
	}

	// Update is called once per frame
	void Update () {
		
	}
}
