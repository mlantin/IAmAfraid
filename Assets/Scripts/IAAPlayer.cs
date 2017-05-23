﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class IAAPlayer : NetworkBehaviour {

	static public IAAPlayer localPlayer;
	static private GameObject cm_playerObject = null; // The local player object
	static private AuthorityManager m_manager = null; // The Authority manager on the local player object

	// other actions can't take place while we're drawing a sequence on any of the objects or words
	[HideInInspector]
	public bool m_drawingsequence = false;

	bool m_isObserver = false;
	bool m_playerTracked = false;
	string m_mocapName;
	ViconActor m_tracker = null;

	public override void OnStartLocalPlayer() {
		localPlayer = this;
		m_tracker = playerObject.GetComponent<ViconActor> ();
		m_playerTracked = LocalPlayerOptions.singleton.trackLocalPlayer;
		if (m_playerTracked)
			m_mocapName = LocalPlayerOptions.singleton.mocapName;
		m_isObserver = LocalPlayerOptions.singleton.observer;
		cm_playerObject = gameObject;
	}

	public void Update() {
		#if UNITY_ANDROID
		if (!isLocalPlayer) return;
		// Listen for recentering events and tell the tracker
		if (m_tracker != null && m_tracker.track && GvrController.Recentered)
			m_tracker.rotCorrected = false;
		#endif
	}

	public bool playerTracked {
		get {
			return m_playerTracked;
		}
		set {
			m_playerTracked = value;
		}
	}

	public bool isObserver {
		get {
			return m_isObserver;
		}
	}

	public string mocapName {
		get {
			return m_mocapName;
		}
	}

	static public GameObject playerObject {
		get { 
			if (cm_playerObject != null) {
				return cm_playerObject;
			} else {
				cm_playerObject = GameObject.FindGameObjectWithTag ("Player");
				return cm_playerObject;
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
	public void CmdSetObjectSequencePath(NetworkInstanceId objid, Vector3[] p, int[] ts) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.syncPath (p,ts);
	}

	[Command]
	public void CmdObjectStartSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.RpcSetCometVisibility (true);
		seq.startSequencer ();
	}

	[Command]
	public void CmdObjectStopSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		NonVerbalSequencer seq = obj.GetComponent<NonVerbalSequencer> ();
		seq.RpcSetCometVisibility (false);
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

	[Command]
	public void CmdSetWordDrawingSequence(NetworkInstanceId objid, bool val) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		wordActs acts = obj.GetComponent<wordActs> ();
		acts.setDrawingSequence (val);
	}

	[Command]
	public void CmdWordStartSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		WordSequencer seq = obj.GetComponent<WordSequencer> ();
		seq.RpcSetCometVisibility (true);
		seq.startSequencer ();
	}

	[Command]
	public void CmdWordStopSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		WordSequencer seq = obj.GetComponent<WordSequencer> ();
		seq.RpcSetCometVisibility (false);
		seq.stopSequencer ();
	}

	[Command]
	public void CmdSetWordSequencePath(NetworkInstanceId objid, int instanceID, Vector3[] p, int[] ts, float[] sc) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		WordSequencer seq = obj.GetComponent<WordSequencer> ();
		seq.RpcSyncPath (instanceID,p,ts,sc);
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