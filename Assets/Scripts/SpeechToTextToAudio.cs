﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;

public class SpeechToTextToAudio : NetworkBehaviour {
	
	static public SpeechToTextToAudio singleton = null;

	private MakeSoundObject m_wordmakerScript = null;

	private int m_RecordingRoutine = 0;
	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private int m_RecordingBufferSize = 5;
	private int m_RecordingHZ = 44100;

	private bool m_readytosend = false;

	[SyncVar]
	private bool m_isRotating = false;

	private AudioClip m_mostRecentClip = null;
	private string m_mostRecentFilename = "";
	private string m_mostRecentTranscript = "";

	private SpeechToText m_SpeechToText = new SpeechToText();

	private MakeSoundObject wordMakerScript {
		get {
			if (m_wordmakerScript == null)
				m_wordmakerScript = IAAPlayer.playerObject.GetComponent<MakeSoundObject> ();
			return m_wordmakerScript;
		}
	}

	void Start()	{

		singleton = this;
		LogSystem.InstallDefaultReactors ();
		// To solve the Waston problem
		Config cfg = Config.Instance;

//		RequestPermissions ();
//		Active = true;
	}
		
	void Update() {
		//if (m_RecordingRoutine != 0 || Input.GetKey(KeyCode.Space)) {
		if (m_isRotating) {
			transform.RotateAround (transform.position, Vector3.up, 3);
		}
		if (Input.GetKeyDown (KeyCode.Space))
			IAAPlayer.localPlayer.CmdSetWatsonRotateCube (netId, true);
		else if (Input.GetKeyUp (KeyCode.Space))
			IAAPlayer.localPlayer.CmdSetWatsonRotateCube (netId, false);
	}

	// Only called by LocalPlayer proxy command
	public void setRotating(bool state) {
		m_isRotating = state;
	}

	public AudioClip mostRecentClip {
		get { return m_mostRecentClip; }
	}
		
	public bool Active
	{
		get { return m_SpeechToText.IsListening; }
		set
		{
			if (value && !m_SpeechToText.IsListening)
			{
				m_SpeechToText.DetectSilence = true;
				m_SpeechToText.EnableWordConfidence = false;
				m_SpeechToText.EnableTimestamps = false;
				m_SpeechToText.SilenceThreshold = 0.03f;
				m_SpeechToText.MaxAlternatives = 1;
				m_SpeechToText.EnableContinousRecognition = false;
				m_SpeechToText.EnableInterimResults = true;
				m_SpeechToText.OnError = OnError;
//				bool res = m_SpeechToText.StartListening(OnRecognize);
				Runnable.Run(startListening());
			}
			else if (!value && m_SpeechToText.IsListening)
			{
				m_SpeechToText.StopListening();
			}
		}
	}

	// A thread to try and retry to connect at least 10 times. Seems to be needed on Android.
	public IEnumerator startListening() {
		bool listening = false;
		int tries = 0;
		tries++;
		listening = m_SpeechToText.StartListening(OnRecognize);
		if (listening || tries == 10) {
			yield break;
		} else {
			yield return null;
		}
	}
		
	public void StartRecording()
	{
//		Active = true;
		if (m_RecordingRoutine == 0)
		{
			UnityObjectUtil.StartDestroyQueue();
			m_RecordingRoutine = Runnable.Run(RecordingHandler2());
			IAAPlayer.localPlayer.CmdSetWatsonRotateCube (netId, true);
		}
	}

	private void StopRecording()
	{
		m_readytosend = false;
		if (m_RecordingRoutine != 0)
		{
			Microphone.End(m_MicrophoneID);
			Runnable.Stop(m_RecordingRoutine);
			m_RecordingRoutine = 0;
			IAAPlayer.localPlayer.CmdSetWatsonRotateCube (netId, false);
		}
	}

	public void sendRecording() {
		m_readytosend = true;
	}

	private void OnError(string error)
	{
		Active = false;

	}

	private IEnumerator RecordingHandler2()
	{
		m_readytosend = false;
		Log.Debug("ExampleStreaming", "devices: {0}", Microphone.devices);
		m_Recording = Microphone.Start(m_MicrophoneID, false, m_RecordingBufferSize, m_RecordingHZ);
		yield return null;      // let m_RecordingRoutine get set..

		if (m_Recording == null)
		{
			StopRecording();
			yield break;
		}

		//m_textField.text = "Recording";
		while (m_RecordingRoutine != 0 && m_Recording != null)
		{
			int writePos = Microphone.GetPosition(m_MicrophoneID);
			if (writePos > m_Recording.samples || !Microphone.IsRecording (m_MicrophoneID)) {
				Log.Error ("MicrophoneWidget", "Microphone disconnected.");

				StopRecording ();
				yield break;
			} else if (m_readytosend) {
				float[] samples = null;
				samples = new float[writePos];

				Microphone.End (m_MicrophoneID);

				m_Recording.GetData (samples, 0);

				AudioData record = new AudioData ();
				record.MaxLevel = Mathf.Max (samples);
				record.Clip = AudioClip.Create ("Recording", writePos, m_Recording.channels, m_RecordingHZ, false);
				record.Clip.SetData (samples, 0);

				m_mostRecentClip = AudioClip.Create ("clipx", writePos, 1, m_RecordingHZ, false);
				m_mostRecentClip.SetData (samples, 0);
				m_SpeechToText.Recognize(m_mostRecentClip, OnRecognize);
				StopRecording ();
			} else {
				yield return new WaitUntil (() => m_readytosend == true);
			}
		}

		yield break;
	}
		
	private void OnRecognize(SpeechRecognitionEvent result)
	{
		if (result != null && result.results.Length > 0) {
			foreach (var res in result.results) {
				foreach (var alt in res.alternatives) {
					string text;
					if (res.final) {
						text = "Final: " + alt.transcript;
						m_mostRecentTranscript = alt.transcript;
						m_mostRecentFilename = "temp/" + Webserver.GenerateFileName (netId.ToString ());
						StartCoroutine (handleUpload ());
					} else {
						text = "Interim: " + alt.transcript;
					}
					Log.Debug ("ExampleStreaming", string.Format ("{0} ({1}, {2:0.00})\n", text, res.final ? "Final" : "Interim", alt.confidence));

				}
			}
		} else {
			// Only for debugging
//			string text = "Test";
//			m_mostRecentTranscript = text;
//			m_mostRecentFilename = Path.Combine("temp", Webserver.GenerateFileName (netId.ToString ()));
//			StartCoroutine (handleUpload ());
		}
	}

	IEnumerator handleUpload() {
		DownloadHandlerBuffer handler = new DownloadHandlerBuffer ();
		yield return StartCoroutine(Webserver.singleton.Upload (m_mostRecentFilename, m_mostRecentClip, handler));
		yield return new WaitUntil(() => handler.isDone == true);
		if (!hasAuthority)
			IAAPlayer.getAuthority (netId);
		yield return new WaitUntil(() => hasAuthority == true);
		spawnTheWord ();
		IAAPlayer.removeAuthority (netId);
	}


	public void spawnTheWord() {
		GameObject controller = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Controller").gameObject;
		Vector3 pos;
		Quaternion rot;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		pos = controller.transform.rotation * Vector3.forward
			+ controller.transform.position;
		rot = controller.transform.rotation;
		#else
		pos = Vector3.forward*2f;
		rot = Quaternion.identity;
		#endif
		wordMakerScript.CmdSpawnSoundObject (m_mostRecentTranscript, 1f, pos, rot, m_mostRecentFilename, true);
	}
}
