using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalPlayer : NetworkBehaviour {

	static private GameObject m_playerObject = null;
	static private AuthorityManager m_manager = null;
	static public LocalPlayer singleton;

	// other actions can't take place while we're drawing a sequence on any of the objects or words
	public bool m_drawingsequence = false;


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
	public void CmdActivateTimedDestroy(NetworkInstanceId objid) {
		NetworkIdentity netid = NetworkServer.objects [objid];
		GameObject obj = netid.gameObject;
		TimedDestroy destroyscript = obj.GetComponent<TimedDestroy> ();
		if (destroyscript)
			destroyscript.RpcActivate ();
	}

	// Non Verbal activities
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
		try {
			GameObject obj = NetworkServer.objects [objid].gameObject;
			NonVerbalActs acts = obj.GetComponent<NonVerbalActs> ();
			acts.setHit (state);
		} catch (KeyNotFoundException e){
		}
	}

	[Command]
	public void CmdToggleObjectLoopingState(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalActs acts = obj.GetComponent<NonVerbalActs> ();
		acts.toggleLooping ();
	}

	[Command]
	public void CmdSetObjectDrawingSequence(NetworkInstanceId objid, bool val) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalActs acts = obj.GetComponent<NonVerbalActs> ();
		acts.setDrawingSequence (val);
	}

	[Command]
	public void CmdSetObjectSequenceTimes(NetworkInstanceId objid, float[] ts) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.syncTimes (ts);
	}

	[Command]
	public void CmdObjectStartSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.startSequencer ();
	}

	[Command]
	public void CmdObjectStopSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.stopSequencer ();
	}

	// Word activities
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
	public void CmdToggleWordLoopingState(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		wordActs acts = obj.GetComponent<wordActs> ();
		acts.toggleLooping ();
	}

//	[Command]
//	public void CmdSeparateLetters(NetworkInstanceId objid) {
//		NetworkIdentity netid = NetworkServer.objects [objid];
//		GameObject obj = netid.gameObject;
//		wordActs acts = obj.GetComponent<wordActs> ();
//		Debug.Log ("I'm going to separate letters now");
//		acts.RpcSeparateLetters ();
//		NetworkServer.Destroy (NetworkServer.objects [objid].gameObject);
//	}

	[Command]
	public void CmdSetWatsonRotateCube(NetworkInstanceId objid, bool state) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SpeechToTextToAudio stt = obj.GetComponent<SpeechToTextToAudio> ();
		stt.setRotating (state);
	}
}
