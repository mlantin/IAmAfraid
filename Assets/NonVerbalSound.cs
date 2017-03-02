using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonVerbalSound : MonoBehaviour {

	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private AudioClip m_mostRecentClip = null;
	private int m_RecordingBufferSize = 5;
	private int m_RecordingHZ = 44100;
	private bool m_recordingDone = false;

	// Use this for initialization
	void Start () {
		m_recordingDone = false;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
		
	public void PlaySound() {
		
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
		yield return null;      // let m_RecordingRoutine get set..

		if (m_Recording == null)
		{
			yield break;
		}

		while (m_Recording != null)
		{
			int writePos = Microphone.GetPosition(m_MicrophoneID);
			if (writePos > m_Recording.samples || !Microphone.IsRecording (m_MicrophoneID)) {
				StopRecording ();
			} else if (m_recordingDone) {
				float[] samples = null;
				samples = new float[writePos];

				Microphone.End (m_MicrophoneID);

				m_Recording.GetData (samples, 0);

				m_mostRecentClip = AudioClip.Create ("clipx", writePos, 1, m_RecordingHZ, false);
				m_mostRecentClip.SetData (samples, 0);
				// create a new sound object
				yield break;

			} else {
				yield return new WaitUntil (() => m_recordingDone == true);
			}
		}

		yield break;
	}
}
