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
	private Text m_textField;

	private int m_RecordingRoutine = 0;
	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private int m_RecordingBufferSize = 2;
	private int m_RecordingHZ = 22050;

	private SpeechToText m_SpeechToText = new SpeechToText();

	void Start()
	{
//		m_textcanvas.transform.SetParent(Camera.main.GetComponent<Transform>(), true);
		m_textField = m_textcanvas.GetComponent<Text> ();
		LogSystem.InstallDefaultReactors();
		Log.Debug("ExampleStreaming", "Start();");

//		RequestPermissions ();
		Active = true;
		//StartRecording();
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
				m_SpeechToText.EnableContinousRecognition = true;
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

	// A thread to try and retry to connect at least 4 times. Seems to be needed on Android.
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
		Active = true;
		if (m_RecordingRoutine == 0)
		{
			UnityObjectUtil.StartDestroyQueue();
			m_RecordingRoutine = Runnable.Run(RecordingHandler());
		}
	}

	private void StopRecording()
	{
		if (m_RecordingRoutine != 0)
		{
			Microphone.End(m_MicrophoneID);
			Runnable.Stop(m_RecordingRoutine);
			m_RecordingRoutine = 0;
		}
	}

	private void OnError(string error)
	{
		Active = false;

		m_textField.text = "Error! " + error;
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
		m_textField.text = "got a result";

		if (result != null && result.results.Length > 0)
		{
			foreach (var res in result.results)
			{
				foreach (var alt in res.alternatives)
				{
					string text = "Watson: " + alt.transcript;
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
