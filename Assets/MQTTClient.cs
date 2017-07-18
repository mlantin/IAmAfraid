using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;

using FlatBuffers;
using Holojam.Protocol;

using System;

public class MQTTClient : MonoBehaviour {
	public static MQTTClient singleton;

	public string BrokerIP = "10.1.1.5";
	public int BrokerPort = 1883;

	public delegate void MQTTHandler(Nugget msg);

	static Dictionary <string, MQTTHandler> mqttHandlers = new Dictionary<string,MQTTHandler>();

	private MqttClient client;
	private string clientId;

	// Use this for initialization
	void Start () {
		// create client instance 
		client = new MqttClient (IPAddress.Parse (BrokerIP), BrokerPort, false, null); 

		// register to message received 
		client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

		clientId = Guid.NewGuid ().ToString (); 
		client.Connect (clientId); 
		
		singleton = this;
	}

	void registerSubscriptions() {
		foreach(var topic in mqttHandlers.Keys) {
			client.Subscribe (new string[]{ topic }, new byte[]{ MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
		}
	}

	public void On(string topic, MQTTHandler handler) {
		if (mqttHandlers.ContainsKey (topic))
			mqttHandlers [topic] += handler;
		else
			mqttHandlers [topic] = handler;

		client.Subscribe (new string[] {topic}, new byte[] {MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});
	}

	public void Unsubscribe(string topic, MQTTHandler handler) {
		if (mqttHandlers.ContainsKey (topic))
			mqttHandlers [topic] -= handler;
	}

	void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 
		if (mqttHandlers.ContainsKey (e.Topic)) {
			ByteBuffer buf = new ByteBuffer (e.Message);
			Nugget nugget = Nugget.GetRootAsNugget (buf);
			mqttHandlers [e.Topic] (nugget);
		}
	}

	void OnApplicationQuit() {
		client.Disconnect ();
	}

	void OnDisable() {
		client.Disconnect ();
	}

	void OnEnable() {
		if (client != null) {
			client.Connect (clientId);
		}
	}
}
