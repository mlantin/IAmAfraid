using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NonVerbalRecord : MonoBehaviour {

	public GameObject m_soundParent; // The parent of all sound objects
	public Material m_soundMat;
	public Text m_DebugText;

	private string m_MicrophoneID = null;
	private AudioClip m_Recording = null;
	private AudioClip m_mostRecentClip = null;
	private int m_RecordingBufferSize = 10;
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
				// create a new sound object
				makeSoundObject();
				yield break;

			} else {
				yield return new WaitUntil (() => m_recordingDone == true);
			}
		}

		yield break;
	}

	private void makeSoundObject() {
		// Create a new sound object which somehow reflects the sound recorded
		GameObject soundobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Renderer rend = soundobj.GetComponent<Renderer>();
		if (rend != null){
			rend.material = m_soundMat;
		}
		soundobj.transform.localScale = Vector3.one * .1f;
		soundobj.transform.parent = m_soundParent.transform;
		// For now I've commented this out since it will never get called from a desktop client for now.
		// Eventually it might be nice to be able to add sounds via a desktop client. But later.
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		soundobj.transform.position = GvrController.ArmModel.pointerRotation * Vector3.forward
		+ GvrController.ArmModel.pointerPosition + Vector3.up * 1.6f;
		#endif
		NonVerbalActs soundscript = soundobj.AddComponent<NonVerbalActs> ();
		soundscript.m_DebugText = m_DebugText;

		GvrAudioSource wordsource = soundobj.AddComponent<GvrAudioSource>();
		wordsource.clip = m_mostRecentClip;
		wordsource.loop = false;

		Mesh mesh;
		Vector3[] verts;

		mesh = soundobj.GetComponent<MeshFilter>().mesh;
		verts = mesh.vertices;
		for(int i = 0; i < verts.Length; i++)
		{
			verts[i] *= Random.Range (.7f, 1.3f);
		}
		mesh.vertices = verts;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		SphereCollider col = soundobj.GetComponent<SphereCollider> ();
		Vector3 maxvert;
		maxvert = mesh.bounds.max;
		col.radius = Mathf.Max(Mathf.Max(maxvert.x,maxvert.y),maxvert.z)+ .02f;
	}
}
