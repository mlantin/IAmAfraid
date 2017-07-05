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

public class mqttTest : MonoBehaviour {
	private MqttClient client;

	private UnityEngine.Vector3 lastpos;

	// Use this for initialization
	void Start () {
		// create client instance 
		client = new MqttClient(IPAddress.Parse("10.1.1.5"),1883 , false , null ); 
		
		// register to message received 
		client.MqttMsgPublishReceived += client_MqttMsgPublishReceived; 
		
		string clientId = Guid.NewGuid().ToString(); 
		client.Connect(clientId); 
		
		// subscribe to the topic "/home/temperature" with QoS 2 
		client.Subscribe(new string[] { "holojam" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE }); 

	}
	void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 

		//Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message)  );
		ByteBuffer buf = new ByteBuffer(e.Message);
		Nugget nugget = Nugget.GetRootAsNugget (buf);
		lastpos = new UnityEngine.Vector3(
			nugget.Flakes(0).Value.Vector3s(0).Value.X,
			nugget.Flakes(0).Value.Vector3s(0).Value.Y,
			nugget.Flakes(0).Value.Vector3s(0).Value.Z);
		

	} 

	void OnGUI(){
		if ( GUI.Button (new Rect (20,40,80,20), "Level 1")) {
			Debug.Log("sending...");
			client.Publish("hello/world", System.Text.Encoding.UTF8.GetBytes("Sending from Unity3D!!!"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
			Debug.Log("sent");
		}
	}
	// Update is called once per frame
	void Update () {

		gameObject.transform.position = lastpos;
	}

	void OnApplicationQuit() {
		client.Disconnect ();
	}
}
