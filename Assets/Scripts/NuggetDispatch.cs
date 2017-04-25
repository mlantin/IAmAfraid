using UnityEngine;

using FlatBuffers;
using Holojam.Protocol;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class NuggetDispatch : MonoBehaviour {

	public delegate void NuggetMsgHandler(Nugget msg);
	//static event NuggetMsgHandler OnNuggetMsg;
	//static Dictionary <string, NuggetMsgHandler> nuggetHandlers = new Dictionary<string, NuggetMsgHandler>();

	private Dictionary <string, UnityEngine.Vector3> vec3Dict;

	// Use this for initialization
	void Start () {
		//
	}

	// Update is called once per frame
	void Update () {
		//
	}

	public static void On(string subjectName, NuggetMsgHandler handler) {
//		if (subjectName)
//			nuggetHandlers [subjectName] += handler;
//		else
//			nuggetHandlers [subjectName] = handler;
	}



}



