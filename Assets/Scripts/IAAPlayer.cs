﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SequenceMessage : MessageBase
{
	static public short SequenceMessageID;
	public NetworkInstanceId netId;
	public Vector3[] path;
	public int[] playtriggers;
	public float[] scrubs;
}

public class IAAPlayer : NetworkBehaviour {

	static public IAAPlayer localPlayer = null;
	static private GameObject cm_playerObject = null; // The local player object
	static private AuthorityManager m_manager = null; // The Authority manager on the local player object

	// other actions can't take place while we're drawing a sequence on any of the objects or words
	[HideInInspector]
	public bool m_drawingsequence = false;
	// Cannot control 2 objects at a time
	private NetworkInstanceId m_isControlling = new NetworkInstanceId(0);

	bool m_isObserver = false;

	//ViconActor m_tracker = null;
	MQTTTrack m_tracker = null;

	public override void OnStartLocalPlayer() {
		Debug.Log ("IAAPlayer: On StartLocalPlayer()");
		// localPlayer = this;
		//m_tracker = playerObject.GetComponent<ViconActor> ();
		m_tracker = playerObject.GetComponent<MQTTTrack>();

		m_isObserver = LocalPlayerOptions.singleton.observer;
		cm_playerObject = gameObject;

		SequenceMessage.SequenceMessageID = MsgType.Highest + 1;
		NetworkManager.singleton.client.RegisterHandler (SequenceMessage.SequenceMessageID, setSequence);
		localPlayer = this;
	}

//	void Start() {
//		if (isLocalPlayer) {
//			
//		}
//	}

	public void Update() {
		#if UNITY_ANDROID
		if (!isLocalPlayer) return;
		// Listen for recentering events and tell the tracker
		if (m_tracker != null && m_tracker.Track && GvrController.Recentered)
			m_tracker.rotCorrected = false;
		#endif
	}

	public bool isObserver {
		get {
			return m_isObserver;
		}
	}

	public bool getUserAuth(NetworkInstanceId netId) {
		if (m_isControlling.Value != 0 && m_isControlling != netId) {
			Debug.Log ("Get local Auth failed.");
			return false;
		} else {
			m_isControlling = netId;
			return true;
		}
	}

	public void removeUserAuth(NetworkInstanceId netId) {
		if (m_isControlling == netId) {
			m_isControlling = new NetworkInstanceId(0);
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
		NetworkIdentity netid = NetworkServer.objects [objid];
		GameObject obj = netid.gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		if (acts.m_looping)
			CmdToggleSoundObjectLoopingState (objid);
		NetworkServer.Destroy (NetworkServer.objects [objid].gameObject);
	}

	[Command]
	public void CmdActivateTimedDestroy(NetworkInstanceId objid) {
		NetworkIdentity netid = NetworkServer.objects [objid];
		GameObject obj = netid.gameObject;
		TimedDestroy destroyscript = obj.GetComponent<TimedDestroy> ();
		if (destroyscript) {
			SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
			if (acts.m_looping)
				CmdToggleSoundObjectLoopingState (objid);
			destroyscript.RpcActivate ();
		} else {
			CmdDestroyObject (objid);
		}
	}

	[Command]
	public void CmdLineUpForOwner(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		acts.soundOwnerIn (netId);
	}

	[Command]
	public void CmdQuitLineForOwner(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		acts.soundOwnerOut (netId);
	}

//	[Command]
//	public void CmdSetSoundObjectPositioned(NetworkInstanceId objid, bool state) {
//		NetworkIdentity netid = NetworkServer.objects [objid];
//		GameObject obj = netid.gameObject;
//		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
//		acts.m_positioned = state;
//		if (!state)
//			netid.AssignClientAuthority (connectionToClient);
//		else
//			netid.RemoveClientAuthority (connectionToClient);
//	}

//	[Command]
//	public void CmdSetSoundObjectHitState(NetworkInstanceId objid, bool state) {
//		try {
//			GameObject obj = NetworkServer.objects [objid].gameObject;
//			SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
//			acts.setHit (state, netId);
//		} catch (KeyNotFoundException e) {
//		}
//	}

	[Command]
	public void CmdSetSoundObjectRecorder(NetworkInstanceId objid, bool state) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		acts.setRecorder (netId, state);
	}

//	[Command]
//	public void CmdSetSoundObjectMovingState(NetworkInstanceId objid, bool state) {
//		GameObject obj = NetworkServer.objects [objid].gameObject;
//		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
//		acts.setMovingState (state);
//	}

	[Command]
	public void CmdToggleSoundObjectLoopingState(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		acts.toggleLooping ();
	}

	[Command]
	public void CmdSetSoundObjectDrawingSequence(NetworkInstanceId objid, bool val) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		acts.setDrawingSequence (val);
	}

	[Command]
	public void CmdSetSoundObjectSequencePath(NetworkInstanceId objid, Vector3[] p, int[] ts, float[] sc) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectSequencer seq = obj.GetComponent<SoundObjectSequencer> ();
		seq.RpcSyncPath (p,ts,sc);
	}

	[Command]
	public void CmdSoundObjectStartSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectSequencer seq = obj.GetComponent<SoundObjectSequencer> ();
		seq.RpcStartSequencer ();
	}

	[Command]
	public void CmdSoundObjectStopSequencer(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectSequencer seq = obj.GetComponent<SoundObjectSequencer> ();
		seq.RpcStopSequencer ();
	}
		
	[Command]
	public void CmdWordSetGranOffset(NetworkInstanceId objid, float f) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		WordActs acts = obj.GetComponent<WordActs> ();
		acts.setGranOffset (netId, f);
	}
		
	[Command]
	public void CmdGetSoundObjectSequencePath(NetworkInstanceId objid) {
		GameObject obj = NetworkServer.objects [objid].gameObject;
		SoundObjectSequencer seq = obj.GetComponent<SoundObjectSequencer> ();
		SequenceMessage msg;
		seq.fillSequenceMessage (out msg);
		msg.netId = objid;
		Debug.Log ("sending the seq fill message with " + msg);
		base.connectionToClient.Send(SequenceMessage.SequenceMessageID, msg );
	}

	public void setSequence(NetworkMessage seqmsg) {
		var msg = seqmsg.ReadMessage<SequenceMessage>();
		var soundObj = ClientScene.FindLocalObject(msg.netId);
		soundObj.GetComponent<SoundObjectSequencer>().syncPath(msg.path,msg.playtriggers,msg.scrubs);
	}

	//	[Command]
	//	public void CmdSeparateLetters(NetworkInstanceId objid) {
	//		NetworkIdentity netid = NetworkServer.objects [objid];
	//		GameObject obj = netid.gameObject;
	//		WordActs acts = obj.GetComponent<WordActs> ();
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
