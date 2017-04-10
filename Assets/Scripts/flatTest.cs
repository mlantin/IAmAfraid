using UnityEngine;

using FlatBuffers;
using Holojam.Protocol;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class flatTest : MonoBehaviour {

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
		Debug.Log("start called on sphere...");
		
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
				var scope = nugget.Origin;
				Debug.Log(scope);
	
				}
			}
		catch (Exception e) {
				Debug.Log ("Caught a UDP Exception");
			}
		
	}
		
}
