using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using HighlightingSystem;

public class NonVerbalActs : SoundObjectActs
{

	public Text m_DebugText;
	private AudioSource m_wordSource;
	private NonVerbalSequencer m_sequencer;
	private Vector3 m_pathNormal = new Vector3(); // we'll set this to be the vector from the object to the camera.

	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	// public bool m_preloaded = false;


	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<AudioSource> ();
		m_wordSource.loop = true;
		m_highlight = GetComponent<Highlighter> ();
		m_highlight.ConstantParams (HighlightColour);
		m_sequencer = GetComponent<NonVerbalSequencer> ();
//		if (m_looping) {
//			m_sequencer.path = tmpPath;
//			m_sequencer.playtriggers = tmpPlayerTrigger;
//		}
	}

	public override void OnStartClient() {
		//Make sure we are highlighted if we're looping or drawing at startup.
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			m_sequencer.setCometVisibility (true);
		} else if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		}
	}

	protected override void tmpStartNewSequence() {
		m_sequencer.startNewSequence ();
	}
	protected override void tmpEndSequence() {
		m_sequencer.endSequence ();
	}

	public override List<int> getSequenceTrigger() {
		return m_sequencer.playtriggers;
	}

	public override List<Vector3> getSequencePath() {
		return m_sequencer.path;
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	public override void OnPointerEnter (PointerEventData eventData) {
		m_target = true;
		if (m_positioned) {
			if (!m_looping)
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public override void OnPointerExit(PointerEventData eventData){
		m_target = false;
		if (m_positioned) {
			if (!m_looping)
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	#endif
	void FixedUpdate() {
		if (m_drawingPath) {
			// Get the point on the current plane
			//m_sequencer.addPos (gameObject.transform.InverseTransformPoint (reticle.transform.position));
			m_sequencer.addPos (gameObject.transform.InverseTransformPoint (RayDrawingPlaneIntersect(reticle.transform.position)));
		}
	}

	Vector3 RayDrawingPlaneIntersect(Vector3 p) {
		float enter;
		Vector3 raydir = (p - Camera.main.transform.position).normalized;
		Ray pathray = new Ray (Camera.main.transform.position, raydir);
		m_drawingPlane.Raycast (pathray, out enter);
		return Camera.main.transform.position + raydir * enter;
	}

//	Vector3 pointOnDrawingPlane(Vector3 p) {
//		// First get the vector from the origin of the plane to p
//		Vector3 op = p - m_drawingPlaneOrig;
//		// Then get the dot product of that vector and the normal and multiply it by the normal.
//		Vector3 vpar = Vector3.Dot(op,m_drawingPlaneNorm)*m_drawingPlaneNorm;
//		Vector3 vperp = op - vpar;
//		return m_drawingPlaneOrig + vperp;
//	}

	void Start() {
		randomizePaperBall ();
		fetchAudio (m_serverFileName);
	}

	// Proxy Functions (END)

	public override void playSound(bool hit) {
		objectHit = hit;
//		if (m_looping)
//			return;
		if (hit) {
			m_wordSource.Play ();
		} else {
			m_wordSource.Stop ();
		}
	}

	protected override void setVolumeFromHeight(float y) {
		float dbvol = Mathf.Clamp(-50+y/1.8f*56f, -50f,6f);
		float vol = Mathf.Pow(10.0f, dbvol/20.0f);
		m_wordSource.volume = vol;
		//m_wordSource.gainDb = dbvol;
	}

	void fetchAudio(string filename) {
//		if (hasAuthority || (isServer && isClient)) { // we created the sound clip so it's probably still in memory
			// Unfortunately hasAuthority is never true because authority is assigned after object creation.
			// There may be another way of telling ourselves that the audioclip is ours but I don't know what
			// it would be. All clients are sent the same spawn message. For now, I've added some logic
			// to at least not go to the server when we are host. The idea is if we are running standalone on
			// the phone that we don't need an extra server. This will most probably break when we are running a 
			// proper host because we won't be the one that generated the clip. There might be a way of finding
			// out if there is only one client.
		if (hasAuthority) {
			m_wordSource.clip = NonVerbalRecord.singleton.mostRecentClip;
			setVolumeFromHeight (transform.position.y);
		} else {
			StartCoroutine(Webserver.singleton.GetAudioClip (filename, 
				(newclip) => { m_wordSource.clip = newclip; setVolumeFromHeight(transform.position.y);}));
		}
	}

	void randomizePaperBall() {
		Mesh mesh;
		Vector3[] verts;

		mesh = gameObject.GetComponent<MeshFilter>().mesh;
		verts = mesh.vertices;
		for(int i = 0; i < verts.Length; i++)
		{
			verts[i] = verts[i].normalized*Random.Range (.3f, .7f);
		}
		mesh.vertices = verts;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		SphereCollider col = gameObject.GetComponent<SphereCollider> ();
		Vector3 maxvert;
		maxvert = mesh.bounds.max;
		col.radius = Mathf.Max(Mathf.Max(maxvert.x,maxvert.y),maxvert.z)+ .02f;
	}

	public override void setLooping(bool val) {
		m_looping = val;
		if (IAAPlayer.localPlayer == null) {
			Debug.Log ("Quit set looping hook");
			return;
		}
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			IAAPlayer.localPlayer.CmdObjectStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdObjectStopSequencer(netId);
		}
	}

}

	
