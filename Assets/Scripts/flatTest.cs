using UnityEngine;
using UnityEngine.Networking;

using FlatBuffers;
using Holojam.Protocol;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class flatTest : NetworkBehaviour {

	IPEndPoint remoteEndPoint;
	UdpClient client;
	string remoteIP;
	int remotePort;
	int localPort;

	// Use this for initialization
	void Start () {

		remoteIP = "127.0.0.1";
		remotePort = 9575;
		localPort = 9591;
		//Creates a UdpClient
		client = new UdpClient(localPort);

		//Creates an IPEndPoint to record the IP Address and port number of the other machine. 
		remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);

		Debug.Log("start called on the test subject (gameObject)...");
	}
	
	// Update is called once per frame
	void Update () {

		try
			{
			if (client.Available > 0) // Only read if we have some data 
			{                           // queued in the network buffer. 
				byte[] data = client.Receive(ref remoteEndPoint);
				Debug.Log("RECEIVED SOME DATA");
				var buf = new ByteBuffer(data);
				// Get an accessor to the root object inside the buffer.
				var nugget = Nugget.GetRootAsNugget(buf);

				// Get the origin
				//var origin = nugget.Origin;

				// Get x, y, and z from the flake at index 0 of the nugget:
				// var flx = nugget.Flakes(0).Value.Vector3s(0).Value.X/1000;
				// var fly = nugget.Flakes(0).Value.Vector3s(0).Value.Y/1000;
				// var flz = nugget.Flakes(0).Value.Vector3s(0).Value.Z/1000;

				// Create a unity Vector3 with the data:
				// assign the Vector3 to the transform's position:
				if(nugget.Flakes(0).Value.Label == "vec3") {
					gameObject.transform.position = new UnityEngine.Vector3(
						nugget.Flakes(0).Value.Vector3s(0).Value.X/1000,
						nugget.Flakes(0).Value.Vector3s(0).Value.Y/1000, 
						nugget.Flakes(0).Value.Vector3s(0).Value.Z/1000);

	//				gameObject.transform.rotation = new UnityEngine.Vector4(
	//					nugget.Flakes(0).Value.Vector4s(0).Value.X,
	//					nugget.Flakes(0).Value.Vector4s(0).Value.Y, 
	//					nugget.Flakes(0).Value.Vector4s(0).Value.Z,
	//					nugget.Flakes(0).Value.Vector4s(0).Value.W);
				}
			}
			} catch (Exception e) {
				Debug.Log ("A UDP Exception was caught ... !");
			}

	}
		
}
