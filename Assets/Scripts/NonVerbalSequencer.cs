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
	List<Vector3> path = new List<Vector3>();

	int nextTime;
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
			nextTime = 0;
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
		if (!isServer)
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
			if (nextPos == playtriggers [nextTime])
				toggleplay = true;
			nextPos++;
			if (nextPos == path.Count)
				nextPos = 0;
		}
		if (toggleplay) {
			playstate = !playstate;
			IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
			nextTime++;
			if (nextTime == playtriggers.Count) {
				nextTime = 0;
				// TODO: think about whether we should first stop the play if we are currently playing. This has
				// the effect of restarting the sound instead of keeping on going. I think this is a matter of 
				// just deciding which one makes more sense. Once it's playing, it will be consistent. But it won't be
				// exactly as recorded if we don't stop and start.
				playstate = true;
				IAAPlayer.localPlayer.CmdSetObjectHitState (netId, playstate);
			}
		}
	}
}
