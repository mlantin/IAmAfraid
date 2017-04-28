using UnityEngine;

using FlatBuffers;
using Holojam.Protocol;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class MocapMsg {
	public UnityEngine.Vector3 pos;
	public Quaternion rot;
}

public class DataPublisher : MonoBehaviour {

	public static DataPublisher singleton;
	public delegate void MocapHandler (MocapMsg msg);

	static Dictionary <string, MocapHandler> mocapHandlers = new Dictionary<string,MocapHandler>();

	// Fields: think about the necessary instance variables.

	public bool m_mocapActive = false;
	public bool m_multicast = false;
	public string m_upstreamIP = "127.0.0.1";
	public string m_multicastIP = "239.0.2.4";
	public int m_upstreamPort = 9591;
	public int m_localPort = 9575;
	public int m_scaleFactor = 1000;

	private IPEndPoint m_remoteEndpoint;
	private UdpClient m_client;

	private Dictionary<string, UnityEngine.Vector3> m_vec3Dict;

	public bool mocapActive {
		get {
			return m_mocapActive;
		}
		set {
			if (value != m_mocapActive) {
				if (value == true) {
					if (m_client == null)
						createUDPClient ();
				}
			}
			m_mocapActive = value;
		}
	}

	public void Start(){
		singleton = this;
		if (m_mocapActive) {
			createUDPClient ();
		}
	}

	private void createUDPClient() {
		try {
		if (m_multicast) {
			m_client = new UdpClient ();

			m_client.ExclusiveAddressUse = false;
			IPEndPoint localEp = new IPEndPoint (IPAddress.Any, m_upstreamPort);

			m_client.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			m_client.ExclusiveAddressUse = false;

			m_client.Client.Bind (localEp);

			IPAddress multicastaddress = IPAddress.Parse (m_multicastIP);
			m_client.JoinMulticastGroup (multicastaddress);
		} else {
			m_client = new UdpClient (m_localPort);
			//Creates an IPEndPoint to record the IP Address and port number of another machine.
			m_remoteEndpoint = new IPEndPoint (IPAddress.Parse (m_upstreamIP), m_upstreamPort);
		}
		Debug.Log ("A UDP client was created");
		} catch (Exception e) {
			Debug.Log ("UDP Exception: " + e.ToString());
		}
	}

	// Methods

	// Gets the packets received and distributes them
	public void Update (){

		try
		{
			if (m_client.Available > 0) // Only read if we have some data
			{                           // queued in the network buffer.
				byte[] data = new byte[1];
				// this kludge to get the last packet
				while (m_client.Available > 0) {
					data = m_client.Receive(ref this.m_remoteEndpoint);
				}
				var buf = new ByteBuffer(data);

				// Get an accessor to the root object inside the buffer.
				var nugget = Nugget.GetRootAsNugget(buf);

				int numflakes = nugget.FlakesLength;
				string subject;
				MocapMsg mocapmsg = new MocapMsg();
				for (int i = 0; i < numflakes; i++) {
				// Get which snowflake this came from
					subject = nugget.Flakes(i).Value.Label;
					if (mocapHandlers.ContainsKey(subject)) {
						mocapmsg.pos.Set(
							nugget.Flakes(i).Value.Vector3s(0).Value.X/m_scaleFactor,
							nugget.Flakes(i).Value.Vector3s(0).Value.Y/m_scaleFactor,
							nugget.Flakes(i).Value.Vector3s(0).Value.Z/m_scaleFactor);
						mocapmsg.rot.Set(
							nugget.Flakes(i).Value.Vector4s(0).Value.X,
							nugget.Flakes(i).Value.Vector4s(0).Value.Y,
							nugget.Flakes(i).Value.Vector4s(0).Value.Z,
							nugget.Flakes(i).Value.Vector4s(0).Value.W);
						mocapHandlers[subject](mocapmsg);
					}
				}
			}
		} catch (Exception e) {
			Debug.Log ("A UDP Exception was caught: "+e.ToString());
		}

	}

	public static void On(string subjectName, MocapHandler handler) {
		if (mocapHandlers.ContainsKey (subjectName))
			mocapHandlers [subjectName] += handler;
		else
			mocapHandlers [subjectName] = handler;
	}

}