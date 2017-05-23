using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WordSequencer : NetworkBehaviour {

	public GameObject comet; // The satellite that will trace the sequence path

	List<int> playtriggers = new List<int> (); // A list of indices for when looping should be triggered. Anchored to path.
	List<Vector3> path = new List<Vector3>(); // A list of positions on the path, one for each fixed update
	// A list of scrub values. Note this list is not the same length as the path
	// because we don't store the values when the path is not in the word
	List<float> scrubs = new List<float> (); 

	int nextInOut;
	int nextPos;
	int nextScrub;
	bool active = false; // Whether sequence is playing
	bool playstate = false; // whether the sound is playing or not

	// Use this for initialization
	void Start () {
		active = false;
		comet.SetActive (false);
	}

	public void startNewSequence() {
		stopSequencer ();
		playtriggers.Clear ();
		path.Clear ();
		scrubs.Clear ();
	}

	public void addPos(Vector3 p) {
		Debug.Log ("adding a position: " + p);
		path.Add(p);
	}

	public void addTime() {
		playtriggers.Add (path.Count-1);
	}

	public void addScrub(float x) {
		scrubs.Add (x);
	}

	public void endSequence() {
		if (!isServer)
			IAAPlayer.localPlayer.CmdSetWordSequencePath (netId, gameObject.GetInstanceID(), path.ToArray(), playtriggers.ToArray (), scrubs.ToArray());
	}

	[ClientRpc]
	public void RpcSyncPath(int objid, Vector3[] p, int[] ts, float[] sc) {
		if (objid == gameObject.GetInstanceID()) // We originated this data so no need to do a copy
			return; 
		
		playtriggers.Clear ();
		for (int i = 0; i < ts.Length; i++) {
			playtriggers.Add (ts [i]);
		}
		path.Clear ();
		for (int i = 0; i < p.Length; i++) {
			path.Add (p [i]);
		}
		scrubs.Clear ();
		for (int i = 0; i < sc.Length; i++) {
			scrubs.Add (sc[i]);
		}
	}

	public void startSequencer () {
		nextPos = 0;
		nextScrub = 0;
		comet.transform.localPosition = path [0];
		comet.SetActive (true);
		active = true;
		nextInOut = 0;
		playstate = true;
		IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
	}

	public void stopSequencer() {
		active = false;
		playstate = false;
		comet.SetActive (false);
	}

	[ClientRpc]
	public void RpcSetCometVisibility(bool visible) {
		comet.SetActive(visible);
	}

	public void FixedUpdate() {
		if (!isClient || !active || scrubs.Count <= 1)
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
			if (nextPos == path.Count) {
				nextPos = 0;
				nextScrub = 0;
			}
			if (toggleplay) {
				playstate = !playstate;
				IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
				nextInOut++;
				if (nextInOut == playtriggers.Count) {
					nextInOut = 0;
				}
			}
			if (playstate == true) {
				Debug.Log ("scrub to " + scrubs [nextScrub]);
				nextScrub++;
			}
		}
	}

}
