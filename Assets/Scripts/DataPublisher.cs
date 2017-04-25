using UnityEngine;

using FlatBuffers;
using Holojam.Protocol;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class DataPublisher : MonoBehaviour {

	// Fields: think about the necessary instance variables.

	private int scaleFactor = 1000;

	private IPEndPoint remoteEndpoint;
	private UdpClient client;
	private string upstreamIP;
	private int upstreamPort;
	private int localPort;

	private Dictionary<string, UnityEngine.Vector3> vec3Dict;

	public DataPublisher(string upIp, int upPort, int locPort){
		// Think about the constructor parameters
		this.upstreamIP = upIp;
		this.upstreamPort = upPort;
		this.localPort = locPort;
		//Creates a UdpClient
		this.client = new UdpClient(localPort);
		//Creates an IPEndPoint to record the IP Address and port number of another machine.
		this.remoteEndpoint = new IPEndPoint(IPAddress.Parse(upIp), upPort);
		Debug.Log("A publisher was created");
	}

	// Methods

	// relays data matching specified filter:
	public void relayData(string filter){

		try
			{
			if (this.client.Available > 0) // Only read if we have some data
			{                           // queued in the network buffer.
				byte[] data = client.Receive(ref this.remoteEndpoint);

				Debug.Log("RECEIVED SOME FLATBUFFER DATA");

				var buf = new ByteBuffer(data);

				// Get an accessor to the root object inside the buffer.
				var nugget = Nugget.GetRootAsNugget(buf);

				// Create a unity Vector3 from the filtered data, and
				// assign the Vector3 to the transform's position:
				if(nugget.Origin == filter ) {
					GameObject.FindGameObjectWithTag(filter).transform.position = new UnityEngine.Vector3(
						nugget.Flakes(0).Value.Vector3s(0).Value.X/scaleFactor,
						nugget.Flakes(0).Value.Vector3s(0).Value.Y/scaleFactor,
						nugget.Flakes(0).Value.Vector3s(0).Value.Z/scaleFactor);
				}
			}
			} catch (Exception e) {
				Debug.Log ("A UDP Exception was caught ... !");
			}

	}

	public void somethingElse(){

	}

}
