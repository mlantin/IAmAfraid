using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonVerbalSequencer : NetworkBehaviour {
	
	NonVerbalActs obj;
	// this will alternate between looping and not looping, starting with looping.
	// So the first value in the list is the amount of seconds to wait until the sound loop is turned off
	List<float> times = new List<float>();
	float currentTime = 0;
	int nextTime;
	float timeAtLastAdd;
	bool active = false;

	// Use this for initialization
	void Start () {
		obj = gameObject.GetComponent<NonVerbalActs> ();
	}
		
	public void startSequencer() {
		if (times.Count > 0) {
			active = true;
			currentTime = 0;
			obj.setLooping (true);
			nextTime = 0;
		}
	}

	public void stopSequencer() {
		active = false;
	}

	public void startNewSequence() {
		times.Clear ();
		currentTime = 0;
		active = false;
		timeAtLastAdd = Time.unscaledTime;
	}

	public void addTime() {
		float t = Time.unscaledTime;
		float dt = t - timeAtLastAdd;
		timeAtLastAdd = t;
		times.Add (dt);
	}

	public void FixedUpdate() {
		if (!active)
			return;
		currentTime += Time.fixedUnscaledDeltaTime;
		if (currentTime > times [nextTime]) {
			if (nextTime == 0)
				obj.setLooping (false);
			LocalPlayer.singleton.CmdToggleObjectLoopingState (netId);
			nextTime++;
		}

	}
}
