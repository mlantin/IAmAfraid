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
	
	private int upPort = 9575;
	private int locPort = 9591;
	private string playerOrigin = "PlayerQ";
	private DataPublisher publisher;

	// Use this for initialization
	void Start () {

		publisher = new DataPublisher ("127.0.0.1", upPort, locPort);

		Debug.Log("start called inside flatTest script ...");

	}

	// Update is called once per frame
	void Update () {
		
		publisher.relayData (playerOrigin);

	}
		
}
