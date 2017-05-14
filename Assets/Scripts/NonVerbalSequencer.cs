using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonVerbalSequencer : NetworkBehaviour {
	
	// this will alternate between looping and not looping, starting with looping.
	// So the first value in the list is the amount of seconds to wait until the sound loop is turned off
	List<float> times = new List<float>();
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
		LocalPlayer.singleton.CmdSetObjectHitState (netId, playstate);
	}

	public void startNewSequence() {
		stopSequencer ();
		times.Clear ();
		currentTime = 0;
		timeAtLastAdd = Time.unscaledTime;
	}

	public void addTime() {
		float t = Time.unscaledTime;
		float dt = t - timeAtLastAdd;
		timeAtLastAdd = t;
		times.Add (dt);
	}

	public void FixedUpdate() {
		if (!active || times.Count <= 1)
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
