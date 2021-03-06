﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NonVerbalRecord : MonoBehaviour {

	static public NonVerbalRecord singleton;

	public Material m_soundMat;
	public Text m_DebugText;

	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private int m_RecordingBufferSize = 10;
	private int m_RecordingHZ = 44100;
	private bool m_recordingDone = false;

	private AudioClip m_mostRecentClip = null;

	// Use this for initialization
	void Start () {
		singleton = this;
		m_recordingDone = false;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
		
	public void PlaySound() {
		
	}

	public AudioClip mostRecentClip {
		get { return m_mostRecentClip; }
	}

	public void StartRecording(){
		StartCoroutine ("RecordingHandler");
	}

	public void StopRecording(){
		m_recordingDone = true;
	}

	private IEnumerator RecordingHandler() {
		m_recordingDone = false;
		m_Recording = Microphone.Start(m_MicrophoneID, false, m_RecordingBufferSize, m_RecordingHZ);
		yield return null;

		if (m_Recording == null)
		{
			yield break;
		}

		while (m_Recording != null)
		{
			int writePos = Microphone.GetPosition(m_MicrophoneID);
			if (writePos > m_Recording.samples || !Microphone.IsRecording (m_MicrophoneID)) {
				StopRecording ();
			}
			if (m_recordingDone) {
				float[] samples = null;
				samples = new float[writePos];

				Microphone.End (m_MicrophoneID);

				m_Recording.GetData (samples, 0);

				m_mostRecentClip = AudioClip.Create ("clipy", writePos, 1, m_RecordingHZ, false);
				m_mostRecentClip.SetData (samples, 0);

				string filename = Path.Combine("temp", Webserver.GenerateFileName (IAAPlayer.playerObject.GetComponent<NetworkIdentity>().netId.ToString ()));
				DownloadHandlerBuffer handler = new DownloadHandlerBuffer ();
				yield return StartCoroutine(Webserver.singleton.Upload (filename, m_mostRecentClip, handler));
				//yield return new WaitUntil(() => handler.isDone == true);

				// create a new sound object
				IAAPlayer.playerObject.GetComponent<MakeSoundObject>().CmdSpawnSoundObject("", 1f, Vector3.one, Quaternion.identity, filename, true);
				yield break;

			} else {
				yield return new WaitUntil (() => m_recordingDone == true);
			}
		}

		yield break;
	}
}
