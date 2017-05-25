using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonVerbalSequencer : NetworkBehaviour {
	
	// this will alternate between looping and not looping, starting with looping.
	// So the first value in the list is the amount of seconds to wait until the sound loop is turned off
	// There is a local list of times and a list that will be synced to the server. This is so the list only
	// gets sent once to the server once the drawing is done.

	public GameObject comet; // The satellite that will trace the sequence path

	NonVerbalActs m_nvActs;

	List<int> playtriggers = new List<int> (); // A list of indices for when looping should be triggered. Anchored to path.
	List<Vector3> path = new List<Vector3>(); // A list of positions on the path, one for each fixed update

	int nextInOut;
	int nextPos;
	bool active = false; // Whether sequence is playing
	bool playstate = false; // whether the sound is playing or not

	// Use this for initialization
	void Start () {
		active = false;
		comet.SetActive (false);
		m_nvActs = gameObject.GetComponent<NonVerbalActs> ();
	}

	[ClientRpc]
	public void RpcStartSequencer() {
		nextPos = 0;
		comet.transform.localPosition = path [0];
		active = true;
		nextInOut = 0;
		playstate = true;
		m_nvActs.playSound (false);
		m_nvActs.playSound (true);
		setCometVisibility (true);
	}

	public void setCometVisibility(bool visible) {
		comet.SetActive(visible);
	}

	[ClientRpc]
	public void RpcStopSequencer() {
		active = false;
		playstate = false;
		setCometVisibility (false);
	}
		
	public void startNewSequence() {
		IAAPlayer.localPlayer.CmdObjectStopSequencer (netId);
		playtriggers.Clear ();
		path.Clear ();
	}

	public void endSequence() {
		if (!isServer) // In case we are a host...there is no point transfering data to ourselves
			IAAPlayer.localPlayer.CmdSetObjectSequencePath (netId, gameObject.GetInstanceID(), path.ToArray(), playtriggers.ToArray ());
	}
		
	[ClientRpc]
	public void RpcSyncPath(int objuid, Vector3[] p, int[] ts) {
		if (objuid == gameObject.GetInstanceID ())  // We don't need to sync because the data came from us
			return;
		
		playtriggers.Clear ();
		for (int i = 0; i < ts.Length; i++) {
			playtriggers.Add (ts [i]);
		}
		path.Clear ();
		for (int i = 0; i < p.Length; i++) {
			path.Add (p [i]);
		}
	}

	public void addPos(Vector3 p) {
		path.Add(p);
	}

	public void addTime() {
		playtriggers.Add (path.Count-1);
	}
		
	public void FixedUpdate() {
		if (!active || playtriggers.Count <= 1)
			return;
		
		bool toggleplay = false;
		if (path.Count > 0) {
			comet.transform.localPosition = path [nextPos];
			if (nextPos == playtriggers [nextInOut])
				toggleplay = true;
			if (toggleplay) {
				playstate = !playstate;
				m_nvActs.playSound(playstate);
				nextInOut++;
				if (nextInOut == playtriggers.Count) {
					nextInOut--;
				}
			}
			nextPos++;
			if (nextPos == path.Count) {
				active = false;
				if (isServer)
					IAAPlayer.localPlayer.CmdObjectStartSequencer (netId);
			}
		}
	}
}
