using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonVerbalSequencer : NetworkBehaviour {
	
	// this will alternate between looping and not looping, starting with looping.
	// So the first value in the list is the amount of seconds to wait until the sound loop is turned off
	// There is a local list of times and a list that will be synced to the server. This is so the list only
	// gets sent once to the server once the drawing is done.
	List<float> times = new List<float> ();
	float currentTime = 0;
	int nextTime;
	float timeAtLastAdd;
	bool active = false;
	bool playstate = false; // whether the sound is playing or not

	// Use this for initialization
	void Start () {
		active = false;
	}
		
	public void startSequencer() {
		if (times.Count > 0) {
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
	}
		
	public void startNewSequence() {
		stopSequencer ();
		times.Clear ();
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

	public void addTime() {
		float t = Time.unscaledTime;
		float dt = t - timeAtLastAdd;
		timeAtLastAdd = t;
		times.Add (dt);
	}

	public void FixedUpdate() {
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
				playstate = true;
				LocalPlayer.singleton.CmdSetObjectHitState (netId, playstate);
			}
		}
	}
}
