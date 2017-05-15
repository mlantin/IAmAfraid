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

	List<float> times = new List<float> ();
	List<Vector3> path = new List<Vector3>();

	float currentTime = 0;
	int nextTime;
	int nextPos;
	float timeAtLastAdd;
	bool active = false;
	bool activepath = false;
	bool playstate = false; // whether the sound is playing or not

	// Use this for initialization
	void Start () {
		active = false;
		activepath = false;
		comet.SetActive (false);
	}

	// just for trying something out
	public void activatePath() {
		comet.SetActive (true);
		activepath = true;
		nextPos = 0;
	}

	public void deactivatePath() {
		comet.SetActive (false);
		activepath = false;
	}

	public void startSequencer() {
		if (times.Count > 0) {
			nextPos = 0;
			comet.transform.localPosition = path [0];
			comet.SetActive (true);
			active = true;
			currentTime = 0;
			nextTime = 0;
			playstate = true;
			LocalPlayer.singleton.CmdSetObjectHitState (netId, playstate);
		}
	}

	public void stopSequencer() {
		active = false;
		playstate = false;
		comet.SetActive (false);
	}
		
	public void startNewSequence() {
		stopSequencer ();
		times.Clear ();
		path.Clear ();
		currentTime = 0;
		timeAtLastAdd = Time.unscaledTime;
	}

	public void endSequence() {
		if (!isServer)
			LocalPlayer.singleton.CmdSetObjectSequenceTimes (netId, times.ToArray ());
	}
		
	public void syncTimes(float[] ts) {
		times.Clear ();
		for (int i = 0; i < ts.Length; i++) {
			times.Add (ts [i]);
		}
	}

	public void addPos(Vector3 p) {
		Debug.Log ("adding a position: " + p);
		path.Add(p);
	}

	public void addTime() {
		float t = Time.unscaledTime;
		float dt = t - timeAtLastAdd;
		timeAtLastAdd = t;
		times.Add (dt);
	}

	public void FixedUpdate() {
		if (activepath && path.Count > 0) {
			comet.transform.localPosition = path [nextPos];
			nextPos++;
			if (nextPos == path.Count)
				nextPos = 0;
		}
		if (!isServer || !active || times.Count <= 1)
			return;
		currentTime += Time.fixedUnscaledDeltaTime;
		if (currentTime > times [nextTime]) {
			currentTime = 0;
			playstate = !playstate;
			LocalPlayer.singleton.CmdSetObjectHitState (netId, playstate);
			nextTime++;
			if (nextTime == times.Count) {
				nextTime = 0;
				// TODO: think about whether we should first stop the play if we are currently playing. This has
				// the effect of restarting the sound instead of keeping on going. I think this is a matter of 
				// just deciding which one makes more sense. Once it's playing, it will be consistent. But it won't be
				// exactly as recorded if we don't stop and start.
				playstate = true;
				LocalPlayer.singleton.CmdSetObjectHitState (netId, playstate);
			}
		}
	}
}
