using System.Collections;
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
	//private static string[] permissionNames = { "android.permission.RECORD_AUDIO" };
	//private static List<GvrPermissionsRequester.PermissionStatus> permissionList =
	//	new List<GvrPermissionsRequester.PermissionStatus>();


	public GameObject m_textcanvas = null;

	private makeaword m_wordmakerScript = null;

	private Text m_textField;

	private int m_RecordingRoutine = 0;
	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private int m_RecordingBufferSize = 5;
	private int m_RecordingHZ = 44100;

	private bool m_readytosend = false;

	[SyncVar]
	private bool m_isRotating = false;

	private AudioClip m_mostRecentClip = null;

	private SpeechToText m_SpeechToText = new SpeechToText();

	void Start()
	{
		m_wordmakerScript = LocalPlayer.playerObject.GetComponent<makeaword>();
		m_textField = m_textcanvas.GetComponent<Text> ();
		LogSystem.InstallDefaultReactors();

//		RequestPermissions ();
//		Active = true;
	}

	void Update() {
		//if (m_RecordingRoutine != 0 || Input.GetKey(KeyCode.Space)) {
		if (m_isRotating) {
			transform.RotateAround (transform.position, Vector3.up, 3);
		}
		if (Input.GetKeyDown (KeyCode.Space))
			setRotateState (true);
		else if (Input.GetKeyUp (KeyCode.Space))
			setRotateState (false);
	}

	void setRotateState(bool state) {
		if (!GetComponent<NetworkIdentity>().hasAuthority)
			LocalPlayer.getAuthority (netId);
		// TODO:There is a problem here...the rotate state will not be set by the server
		// if it had to get authority first. So this next call will always fail if 
		// the getAuthority call was done in the previous line. In practice the first
		// time you click on the cube, it won't rotate.
		CmdSetRotateState (state);
	}

	[Command]
	void CmdSetRotateState(bool state) {
		m_isRotating = state;
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
			setRotateState(true);
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
			setRotateState(false);
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
						string filename = Webserver.singleton.GenerateFileName (netId.ToString ());
						StartCoroutine (Webserver.singleton.Upload (filename, m_mostRecentClip));

						Vector3 pos;
						Quaternion rot;
						#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
						pos = GvrController.ArmModel.pointerRotation * Vector3.forward + 
							GvrController.ArmModel.pointerPosition + Vector3.up * 1.6f;
						rot = GvrController.ArmModel.pointerRotation;
						#else
						pos = Vector3.forward*2f;
						rot = Quaternion.identity;
						#endif
						m_wordmakerScript.CmdSpawnWord (alt.transcript, 1f, pos, rot, filename);
					} else {
						text = "Interim: " + alt.transcript;
					}
					Log.Debug("ExampleStreaming", string.Format("{0} ({1}, {2:0.00})\n", text, res.final ? "Final" : "Interim", alt.confidence));
					m_textField.text = text;

				}
			}
		}
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
