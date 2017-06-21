using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WordSequencer : NetworkBehaviour {

	public GameObject comet; // The satellite that will trace the sequence path

	wordActs m_wordActs;

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
		m_wordActs = gameObject.GetComponent<wordActs> ();
		active = false;
		comet.SetActive (false);
	}

	public void startNewSequence() {
		IAAPlayer.localPlayer.CmdWordStopSequencer(netId);
		playtriggers.Clear ();
		path.Clear ();
		scrubs.Clear ();
	}

	public void addPos(Vector3 p) {
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
			IAAPlayer.localPlayer.CmdSetWordSequencePath (netId, path.ToArray(), playtriggers.ToArray (), scrubs.ToArray());
	}

	public void fillSequenceMessage(out SequenceMessage msg) {
		SequenceMessage m = new SequenceMessage ();
		m.path = path.ToArray ();
		m.playtriggers = playtriggers.ToArray ();
		m.scrubs = scrubs.ToArray ();
		msg = m;
	}

	[ClientRpc]
	public void RpcSyncPath(Vector3[] p, int[] ts, float[] sc) {
//		if (objid == gameObject.GetInstanceID()) // We originated this data so no need to do a copy
//			return; 

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

	public void syncPath(Vector3[] p, int[] ts, float[] sc) {
		//		if (objid == gameObject.GetInstanceID()) // We originated this data so no need to do a copy
		//			return; 

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

		Debug.Log ("Got the path without RPC");
	}

	[ClientRpc]
	public void RpcStartSequencer () {
		nextPos = 0;
		nextScrub = 0;
		if (path.Count > 0) {
			comet.transform.localPosition = path [0];
			setCometVisibility (true);
		}
		active = true;
		nextInOut = 0;
		playstate = true;
		m_wordActs.playWord (true);
	}

	[ClientRpc]
	public void RpcStopSequencer() {
		active = false;
		playstate = false;
		setCometVisibility (false);
	}

	public void setCometVisibility(bool visible) {
		comet.SetActive(visible);
	}

	public void FixedUpdate() {
		if (!active || scrubs.Count <= 1)
			return;

		bool toggleplay = false;
		if (path.Count > 0) {
			comet.transform.localPosition = path [nextPos];
			if (playtriggers.Count > 0) {
				if (nextPos == playtriggers [nextInOut])
					toggleplay = true;
				if (toggleplay) {
					playstate = !playstate;
					m_wordActs.playWord (playstate);
					nextInOut++;
					if (nextInOut == playtriggers.Count) {
						nextInOut--;
					}
				}
			}
			if (playstate == true) {
				m_wordActs.setLocalGranOffset (scrubs [nextScrub]);
				nextScrub++;
			}
			nextPos++;
			if (nextPos == path.Count) {
				active = false;
				if (isServer)
					IAAPlayer.localPlayer.CmdWordStartSequencer (netId);
			}
		}
	}

}
