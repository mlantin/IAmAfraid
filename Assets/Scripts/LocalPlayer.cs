using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalPlayer : NetworkBehaviour {

	static private GameObject m_playerObject = null;
	static private AuthorityManager m_manager = null;
	static public LocalPlayer singleton;

	public override void OnStartLocalPlayer() {
		singleton = this;
		m_playerObject = gameObject;
	}

	static public GameObject playerObject {
		get { 
			if (m_playerObject != null) {
				return m_playerObject;
			} else {
				m_playerObject = GameObject.FindGameObjectWithTag ("Player");
				return m_playerObject;
			}
		}
	}

	static public void getAuthority(NetworkInstanceId netInstanceId) {
		if (m_manager == null) {
			m_manager = playerObject.GetComponent<AuthorityManager> ();
		}
		if (m_manager != null) {
			m_manager.CmdAssignObjectAuthority (netInstanceId);
		}
	}

	static public void removeAuthority(NetworkInstanceId netInstanceId) {
		if (m_manager == null) {
			m_manager = playerObject.GetComponent<AuthorityManager> ();
		}
		if (m_manager != null) {
			m_manager.CmdRemoveObjectAuthority (netInstanceId);
		}
	}

	[Command]
	public void CmdDestroyObject(NetworkInstanceId objid) {
		NetworkServer.Destroy (NetworkServer.objects [objid].gameObject);
	}

	[Command]
	public void CmdSetObjectPositioned(NetworkInstanceId objid, bool state) {
		NetworkIdentity netid = NetworkServer.objects [objid];
		GameObject obj = netid.gameObject;
		NonVerbalActs acts = obj.GetComponent<NonVerbalActs> ();
		acts.m_positioned = state;
		if (!state)
			netid.AssignClientAuthority (connectionToClient);
		else
			netid.RemoveClientAuthority (connectionToClient);
		Debug.Log ("Set positioned to " + state);
	}

	[Command]
	public void CmdSetObjectHitState(NetworkInstanceId objid, bool state){
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalActs acts = obj.GetComponent<NonVerbalActs> ();
		acts.setHit (state);
	}

	[Command]
	public void CmdSetWordHitState(NetworkInstanceId objid, bool state) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		wordActs acts = obj.GetComponent<wordActs> ();
		acts.setHit (state);
	}

	[Command]
	public void CmdSetWordPositioned(NetworkInstanceId objid, bool state) {
		NetworkIdentity netid = NetworkServer.objects [objid];
		GameObject obj = netid.gameObject;
		wordActs acts = obj.GetComponent<wordActs> ();
		acts.m_positioned = state;
		if (!state)
			netid.AssignClientAuthority (connectionToClient);
		else
			netid.RemoveClientAuthority (connectionToClient);
	}

	[Command]
	public void CmdSetWatsonRotateCube(NetworkInstanceId objid, bool state) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SpeechToTextToAudio stt = obj.GetComponent<SpeechToTextToAudio> ();
		stt.setRotating (state);
	}
}
