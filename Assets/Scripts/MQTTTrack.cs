using UnityEngine;
using System.Collections;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;

using FlatBuffers;
using Holojam.Protocol;

using System;

public class MQTTTrack : MonoBehaviour {

	public GameObject playerCamera;
	public string label = "ECUVicon/Update/PIXEL1";
	bool labelSubscribed = false;

	bool track = false;
	bool UpdatedThisFrame = false;
	UnityEngine.Vector3 TrackedPosition = new UnityEngine.Vector3();
	Quaternion TrackedRotation = new Quaternion();

	UnityEngine.Vector3 viconPos = new UnityEngine.Vector3 ();
	Quaternion viconRot = new Quaternion();
	// This is the difference between what the Vicon rotation says we are and what 
	// the daydream head tracking says we are.
	Quaternion rotDiff; 
	public bool rotCorrected = false;

	private UnityEngine.Vector3 lastpos;

	// Use this for initialization
	void Start () {
		if (track && MQTTClient.singleton && !labelSubscribed) {
			MQTTClient.singleton.On (label, updateReceived);
			labelSubscribed = true;
		}
	}

	public bool Track {
		get {
			return track;
		}
		set {
			if (value != track && MQTTClient.singleton) {
				if (value == true && !labelSubscribed) {
					MQTTClient.singleton.On (label, updateReceived);
					labelSubscribed = true;
				} else if (labelSubscribed) {
					MQTTClient.singleton.Unsubscribe (label, updateReceived);
					labelSubscribed = false;
				}
			}
			track = value;
		}
	}

	public void SetLabel(string labelstr) {
		if (label != labelstr && track && MQTTClient.singleton) {
			if (labelSubscribed) {
				MQTTClient.singleton.Unsubscribe (label, updateReceived);
				labelSubscribed = false;
			}
			MQTTClient.singleton.On (labelstr, updateReceived);
			labelSubscribed = true;
		}
		label = labelstr;
	}

	void updateReceived(Nugget nugget) 
	{ 
		TrackedPosition.Set (
			nugget.Flakes(0).Value.Vector3s(0).Value.X,
			nugget.Flakes(0).Value.Vector3s(0).Value.Y,
			nugget.Flakes(0).Value.Vector3s(0).Value.Z);
		TrackedRotation.Set (
			nugget.Flakes (0).Value.Vector4s (0).Value.X,
			nugget.Flakes (0).Value.Vector4s (0).Value.Y,
			nugget.Flakes (0).Value.Vector4s (0).Value.Z,
			nugget.Flakes (0).Value.Vector4s (0).Value.W);
		UpdatedThisFrame = true;
		//deltaTime = Time.time - lastUpdateTime;
		//lastUpdateTime = Time.time;
		//Debug.Log (deltaTime);
	} 
		
	// Update is called once per frame
	void Update () {
		UpdateTracking ();
		//Debug.Log ("Frame time: " + Time.deltaTime);
	}

	void UpdateTracking() {
		if (UpdatedThisFrame) {
			viconPos = TrackedPosition;
			viconRot = TrackedRotation;
			viconPos.Set (viconPos.x, viconPos.y, -viconPos.z);
			viconRot.Set (viconRot.x, viconRot.y, -viconRot.z, -viconRot.w);
			if (!rotCorrected)
				correctRotation ();
			//			transform.position = rotDiff*viconPos;
			transform.position = viconPos;
			//			transform.rotation = viconRot;
			UpdatedThisFrame = false;
		} 
	}

	void correctRotation() {
		//rotDiff = playerCamera.transform.localRotation*Quaternion.Inverse (viconRot);
		rotDiff = playerCamera.transform.localRotation*viconRot;
		Debug.Log ("Rotdiff: " + rotDiff);
		transform.rotation = rotDiff;
		// ignore the x and z axes
		UnityEngine.Vector3 angles = transform.rotation.eulerAngles;
		angles.x = angles.z = 0;
		transform.eulerAngles = angles;
		rotCorrected = true;
	}


}
