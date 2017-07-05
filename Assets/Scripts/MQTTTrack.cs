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
	private MqttClient client;

	public GameObject playerCamera;
	public bool track = false;
	public string label = "Trackable";
	public string scope = ""; 

	bool UpdatedThisFrame = false;
	UnityEngine.Vector3 TrackedPosition = new UnityEngine.Vector3();
	Quaternion TrackedRotation = new Quaternion();
	float deltaTime = 0;
	float lastUpdateTime;

	UnityEngine.Vector3 viconPos = new UnityEngine.Vector3 ();
	Quaternion viconRot = new Quaternion();
	// This is the difference between what the Vicon rotation says we are and what 
	// the daydream head tracking says we are.
	Quaternion rotDiff; 
	public bool rotCorrected = false;

	private UnityEngine.Vector3 lastpos;

	// Use this for initialization
	void Start () {
		if (track) {
			// create client instance 
			client = new MqttClient (IPAddress.Parse ("10.1.1.5"), 1883, false, null); 

			// register to message received 
			client.MqttMsgPublishReceived += client_MqttMsgPublishReceived; 

			string clientId = Guid.NewGuid ().ToString (); 
			client.Connect (clientId); 

			lastUpdateTime = Time.time;

			// subscribe to the topic "/home/temperature" with QoS 2 
			client.Subscribe (new string[] { "holojam" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
		}
	}
	void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 
		//Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message)  );
		Debug.Log("Received");
		ByteBuffer buf = new ByteBuffer(e.Message);
		Nugget nugget = Nugget.GetRootAsNugget (buf);
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
		else {
			Debug.Log ("No update");
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

	void OnApplicationQuit() {
		client.Disconnect ();
	}
}
