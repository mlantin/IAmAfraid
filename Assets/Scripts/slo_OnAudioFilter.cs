using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class slo_OnAudioFilter : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void OnAudioFilterRead(float[] buffer, int numChannels) {
		Debug.Log(buffer[0]);
	}
}
