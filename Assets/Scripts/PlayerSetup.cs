using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		if (isLocalPlayer) {
			// reparent the player hierarchy (Main Camera and GVRController) to this playerObject
			GameObject mainplayer = GameObject.Find ("Player");
			mainplayer.transform.parent = this.gameObject.transform;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
