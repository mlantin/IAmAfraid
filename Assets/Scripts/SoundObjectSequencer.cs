using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SoundObjectSequencer : NetworkBehaviour {

	// this will alternate between looping and not looping, starting with looping.
	// So the first value in the list is the amount of seconds to wait until the sound loop is turned off
	// There is a local list of times and a list that will be synced to the server. This is so the list only
	// gets sent once to the server once the drawing is done.

	public GameObject comet; // The satellite that will trace the sequence path
	[HideInInspector]
	public SoundObjectActs m_acts;
	[HideInInspector]
	public List<int> playtriggers = new List<int> (); // A list of indices for when looping should be triggered. Anchored to path.
	[HideInInspector]
	public List<Vector3> path = new List<Vector3>(); // A list of positions on the path, one for each fixed update
	[HideInInspector]
	public bool loadedFromScene = false;
	// A list of scrub values. Note this list is not the same length as the path
	// because we don't store the values when the path is not in the word
	[HideInInspector]
	public List<float> scrubs = new List<float> (); 

	protected int nextInOut;
	protected int nextPos;
	protected int nextScrub;
	protected bool active = false; // Whether sequence is playing
	protected bool playstate = false; // whether the sound is playing or not

	public void fillSequenceMessage(out SequenceMessage msg) {
		SequenceMessage m = new SequenceMessage ();
		m.path = path.ToArray ();
		m.playtriggers = playtriggers.ToArray ();
		m.scrubs = scrubs.ToArray ();
		msg = m;
	}

	void Awake() {
		m_acts = gameObject.GetComponent<SoundObjectActs> ();
	}

	void Start () {
		active = false;
		comet.SetActive (false);
	}

	public void endSequence() {
		if (!isServer)
			IAAPlayer.localPlayer.CmdSetSoundObjectSequencePath (netId, path.ToArray(), playtriggers.ToArray (), scrubs.ToArray());
	}

	[ClientRpc]
	public void RpcSyncPath(Vector3[] p, int[] ts, float[] sc) {
		//		if (objid == gameObject.GetInstanceID()) // We originated this data so no need to do a copy
		//			return; 
		Debug.LogWarning("RPCSYNC for word" + m_acts.netId);
		syncPath (p, ts, sc);
	}

	public void syncPath(Vector3[] p, int[] ts, float[] sc) {

		playtriggers.Clear ();
		for (int i = 0; i < ts.Length; i++) {
			playtriggers.Add (ts [i]);
		}
		path.Clear ();
		for (int i = 0; i < p.Length; i++) {
			path.Add (p [i]);
		}
		scrubs.Clear ();
		if (sc != null) {
			for (int i = 0; i < sc.Length; i++) {
				scrubs.Add (sc[i]);
			}
		}

		// Debug.Log ("Got the path without RPC");
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
		m_acts.playSound (false);
		m_acts.playSound (true);
	}

	[ClientRpc]
	public void RpcStopSequencer() {
		active = false;
		playstate = false;
		setCometVisibility (false);
	}

	public void startNewSequence() {
		IAAPlayer.localPlayer.CmdSoundObjectStopSequencer (netId);
		playtriggers.Clear ();
		path.Clear ();
		scrubs.Clear ();
	}

	public void addPos(Vector3 p) {
		path.Add(p);
	}

	public void addScrub(float x) {
		scrubs.Add (x);
	}

	public void addTime() {
		playtriggers.Add (path.Count-1);
	}

	public void setCometVisibility(bool visible) {
		comet.SetActive(visible);
	}

}
