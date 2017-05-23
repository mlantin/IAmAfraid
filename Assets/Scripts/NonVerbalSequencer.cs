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
	}

	public void startSequencer() {
		if (playtriggers.Count > 0) {
			nextPos = 0;
			comet.transform.localPosition = path [0];
			comet.SetActive (true);
			active = true;
			nextInOut = 0;
			playstate = true;
			IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
		}
	}

	[ClientRpc]
	public void RpcSetCometVisibility(bool visible) {
		comet.SetActive(visible);
	}

	public void stopSequencer() {
		active = false;
		playstate = false;
		comet.SetActive (false);
	}
		
	public void startNewSequence() {
		stopSequencer ();
		playtriggers.Clear ();
		path.Clear ();
	}

	public void endSequence() {
		if (!isServer) // In case we are a host...there is no point transfering data to ourselves
			IAAPlayer.localPlayer.CmdSetObjectSequencePath (netId, path.ToArray(), playtriggers.ToArray ());
	}
		
	public void syncPath(Vector3[] p, int[] ts) {
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
		Debug.Log ("adding a position: " + p);
		path.Add(p);
	}

	public void addTime() {
		playtriggers.Add (path.Count-1);
	}

	public void FixedUpdate() {
		if (!isServer || !active || playtriggers.Count <= 1)
			return;
		
		bool toggleplay = false;
		if (path.Count > 0) {
			comet.transform.localPosition = path [nextPos];
			if (nextPos == playtriggers [nextInOut])
				toggleplay = true;
			if (nextPos == 0) { // We are starting again so reset the play
				if (playstate == true) {
					IAAPlayer.localPlayer.CmdSetObjectHitState (netId, false);
					IAAPlayer.localPlayer.CmdSetObjectHitState (netId, true);
				} else {
					toggleplay = true;
				}
			}
			nextPos++;
			if (nextPos == path.Count)
				nextPos = 0;
		
			if (toggleplay) {
				playstate = !playstate;
				IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
				nextInOut++;
				if (nextInOut == playtriggers.Count) {
					nextInOut = 0;
				}
			}
		}
	}
}
