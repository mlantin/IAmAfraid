using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		if (!isLocalPlayer) {
			gameObject.tag = "RemotePlayer";
			if (isServer) {
				GameObject maincamera = GameObject.FindGameObjectWithTag ("MainCamera");
				maincamera.transform.parent = gameObject.transform;
			}
		}
	}

	public override void OnStartLocalPlayer() {

		// tag the local player so we can look for it later in other objects
		gameObject.tag = "Player";

		Debug.Log(gameObject.transform.position);

		if (!isServer) {
			//Take control of the reticle
			GameObject reticle = GameObject.Find ("Reticle");
			GetComponent<AuthorityManager> ().CmdAssignObjectAuthority (reticle.GetComponent<NetworkIdentity> ().netId);

			// reparent the player hierarchy (Main Camera and GVRController) to this playerObject
			GameObject maincamera = GameObject.FindGameObjectWithTag("MainCamera");
			maincamera.transform.parent.position = this.gameObject.transform.position;
			this.gameObject.transform.parent = maincamera.transform;
			gameObject.transform.localPosition = Vector3.zero;
		}
	}

//	public override void OnStopClient() {
//		// Take back the reticle
//		GameObject reticle = GameObject.Find("Reticle");
//		GetComponent<AuthorityManager> ().CmdRemoveObjectAuthority (reticle.GetComponent<NetworkIdentity> ().netId);
//	}
}
