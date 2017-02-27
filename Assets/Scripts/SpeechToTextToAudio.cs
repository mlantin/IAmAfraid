using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;

public class SpeechToTextToAudio : MonoBehaviour {
	//private static string[] permissionNames = { "android.permission.RECORD_AUDIO" };
	//private static List<GvrPermissionsRequester.PermissionStatus> permissionList =
	//	new List<GvrPermissionsRequester.PermissionStatus>();

	public GameObject m_textcanvas = null;
	public GameObject m_wordmaker = null;
	private makeaword m_wordmakerScript = null;

	private Text m_textField;

	private int m_RecordingRoutine = 0;
	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private int m_RecordingBufferSize = 5;
	private int m_RecordingHZ = 44100;

	private bool m_readytosend = false;

	private AudioClip m_mostRecentClip = null;

	private SpeechToText m_SpeechToText = new SpeechToText();

	void Start()
	{
		m_wordmakerScript = m_wordmaker.GetComponent<MonoBehaviour>() as makeaword;
		m_textField = m_textcanvas.GetComponent<Text> ();
		LogSystem.InstallDefaultReactors();
		Log.Debug("ExampleStreaming", "Start();");

//		RequestPermissions ();
//		Active = true;
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
			m_textField.text = "success connecting!";
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
		}
	}

	public void sendRecording() {
		m_readytosend = true;
	}

	private void OnError(string error)
	{
		Active = false;

		m_textField.text = "Error! " + error;
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

		m_textField.text = "Recording";
		while (m_RecordingRoutine != 0 && m_Recording != null)
		{
			int writePos = Microphone.GetPosition(m_MicrophoneID);
			if (writePos > m_Recording.samples || !Microphone.IsRecording (m_MicrophoneID)) {
				m_textField.text = "Error: Microphone disconnected";
				Log.Error ("MicrophoneWidget", "Microphone disconnected.");

				StopRecording ();
				yield break;
			} else if (m_readytosend) {
				m_textField.text = "sending " + writePos + " samples";
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

	private IEnumerator RecordingHandler()
	{
		//m_textField.text = "Streaming devices: " + Microphone.devices;
		Log.Debug("ExampleStreaming", "devices: {0}", Microphone.devices);
		m_Recording = Microphone.Start(m_MicrophoneID, true, m_RecordingBufferSize, m_RecordingHZ);
		yield return null;      // let m_RecordingRoutine get set..

		if (m_Recording == null)
		{
			StopRecording();
			yield break;
		}

		bool bFirstBlock = true;
		int midPoint = m_Recording.samples / 2;
		float[] samples = null;

		while (m_RecordingRoutine != 0 && m_Recording != null)
		{
			int writePos = Microphone.GetPosition(m_MicrophoneID);
			//m_textField.text = "firstblock " + bFirstBlock.ToString() + " " + "midpoint is " + midPoint + " in here with " + writePos + " samples";
			if (writePos > m_Recording.samples || !Microphone.IsRecording(m_MicrophoneID))
			{
				m_textField.text = "Error: Microphone disconnected";
				Log.Error("MicrophoneWidget", "Microphone disconnected.");

				StopRecording();
				yield break;
			}

			if ((bFirstBlock && writePos >= midPoint)
				|| (!bFirstBlock && writePos < midPoint))
			{

				// front block is recorded, make a RecordClip and pass it onto our callback.
				samples = new float[midPoint];
				m_Recording.GetData(samples, bFirstBlock ? 0 : midPoint);

				AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(samples);
				record.Clip = AudioClip.Create("Recording", midPoint, m_Recording.channels, m_RecordingHZ, false);
				record.Clip.SetData(samples, 0);

				m_SpeechToText.OnListen(record);

				bFirstBlock = !bFirstBlock;

				//m_textField.text = "Sending sending sending sending sending";
			}
			else
			{
				// calculate the number of samples remaining until we ready for a block of audio, 
				// and wait that amount of time it will take to record.
				int remaining = bFirstBlock ? (midPoint - writePos) : (m_Recording.samples - writePos);
				float timeRemaining = (float)remaining / (float)m_RecordingHZ;

				yield return new WaitForSeconds(timeRemaining);
			}

		}

		yield break;
	}

	private void OnRecognize(SpeechRecognitionEvent result)
	{
		if (result != null && result.results.Length > 0)
		{
			foreach (var res in result.results)
			{
				foreach (var alt in res.alternatives)
				{
					string text;
					if (res.final) {
						text = "Final: " + alt.transcript;
						Vector3 pos = new Vector3 (0.0f, 0.0f, 1.5f);
						pos = GvrController.Orientation * pos;
						m_wordmakerScript.makeword (alt.transcript, 1f, pos, GvrController.Orientation, m_mostRecentClip);
					} else {
						text = "Interim: " + alt.transcript;
					}
					Log.Debug("ExampleStreaming", string.Format("{0} ({1}, {2:0.00})\n", text, res.final ? "Final" : "Interim", alt.confidence));
					m_textField.text = text;

				}
			}
		}
	}
	// Update is called once per frame
	void Update () {
		
	}

//	public void RequestPermissions() {
//		GvrPermissionsRequester permissionRequester = GvrPermissionsRequester.Instance;
//		if (permissionRequester == null) {
//			m_textField.text = "Permission requester cannot be initialized.";
//			return;
//		}
//		Debug.Log("Permissions.RequestPermisions: Check if permission has been granted");
//		if (!permissionRequester.IsPermissionGranted(permissionNames[0])) {
//			Debug.Log("Permissions.RequestPermisions: Permission has not been previously granted");
//			if (permissionRequester.ShouldShowRational(permissionNames[0])) {
//				m_textField.text = "This app needs to access the microphone.  Please grant permission when prompted.";
//			}
//			permissionRequester.RequestPermissions(permissionNames,
//				(GvrPermissionsRequester.PermissionStatus[] permissionResults) =>
//				{
//					permissionList.Clear();
//					permissionList.AddRange(permissionResults);
//					string msg = "";
//					foreach (GvrPermissionsRequester.PermissionStatus p in permissionList) {
//						msg += p.Name + ": " + (p.Granted ? "Granted" : "Denied") + "\n";
//					}
//					m_textField.text = msg;
//				});
//		}
//		else {
//			m_textField.text = "Microphone permission already granted!";
//		}
//	}
}
