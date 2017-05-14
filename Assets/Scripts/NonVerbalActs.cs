using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using HighlightingSystem;

public class NonVerbalActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
#endif
{

	// This is to set a timer when a person clicks. If we linger long enough 
	// we call it a press&hold.
	float m_presstime = 0;
	bool m_presshold = true;
	const float m_holdtime = .5f; // seconds until we call it a press&hold.

	[SyncVar]
	public string m_serverFileName = "";
	public Text m_DebugText;
	Highlighter m_highlight;
	NonVerbalSequencer m_sequencer;

	float m_distanceFromPointer = 1.0f;
	GvrAudioSource m_wordSource;
	 
	GameObject m_laser = null;

	GameObject laser {
		get {
			if (m_laser == null) 
				m_laser = LocalPlayer.playerObject.transform.FindChild ("GvrControllerPointer/Laser").gameObject;
			return m_laser;
		}
	}

	Quaternion m_rotq;
	bool m_moving = false;
	bool m_target = false; // Whether the reticle is on this object

	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	public bool m_preloaded = false;
	[SyncVar (hook ="playSound")]
	private bool objectHit = false;
	[SyncVar]
	public bool m_positioned = false;
	[SyncVar (hook = "setLooping")]
	bool m_looping = false;
	[SyncVar (hook = "setDrawing")]
	bool m_drawingSequence = false;

	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
		m_wordSource.loop = true;
		m_highlight = GetComponent<Highlighter> ();
		Color col = new Color (204, 102, 255); // a purple
		m_highlight.ConstantParams (col);

		m_sequencer = GetComponent<NonVerbalSequencer> ();
	}

	void Update () {
		if (isServer)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if ((!m_positioned && hasAuthority) || (m_moving)) {
			if (!m_moving) {
				transform.position = laser.transform.position + laser.transform.forward;
			} else {
				// We have picked an object and we're moving it...
				Vector3 newdir = m_rotq*laser.transform.forward;
				transform.position = laser.transform.position+newdir*m_distanceFromPointer;
			}
			transform.rotation = laser.transform.rotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
				m_moving = false;
				LocalPlayer.singleton.CmdSetObjectPositioned(netId,true);
			}
		} else if (m_positioned && !m_moving) {
			if (m_target && GvrController.ClickButtonDown) {
				m_presstime = 0;
				m_presshold = false;
			} else if (m_target && GvrController.ClickButton) {
				m_presstime += Time.deltaTime;
				if (!m_presshold && m_presstime > m_holdtime) {
					m_presshold = true;
					if (GvrController.TouchPos.x > .85f) {
						if (m_looping)
							LocalPlayer.singleton.CmdToggleObjectLoopingState(netId);
						LocalPlayer.singleton.CmdSetObjectDrawingSequence(netId,true);
						LocalPlayer.singleton.CmdSetObjectHitState (netId, false);
						LocalPlayer.singleton.CmdSetObjectHitState (netId, true);
						m_sequencer.startNewSequence();
					}
				}
			} else if (GvrController.ClickButtonUp) {
				if (GvrController.TouchPos.x > .85f) {
					if (m_drawingSequence) {
						m_sequencer.addTime();
						m_sequencer.endSequence();
						LocalPlayer.singleton.CmdSetObjectDrawingSequence(netId,false);
						if (!m_looping)
							LocalPlayer.singleton.CmdToggleObjectLoopingState(netId);
					} else if (m_target) {
						LocalPlayer.singleton.CmdToggleObjectLoopingState (netId);
						Debug.Log("Toggle sequencer");
					}
				}
				m_presshold = false;
			}
		}

		#endif
	}

	void Start() {
		randomizePaperBall ();
		fetchAudio (m_serverFileName);
	}

	void setPositionedState(bool state) {
		if (!hasAuthority) {
			LocalPlayer.getAuthority (netId);
		}
		CmdSetPositioned (state);

		if (state == true) {
			LocalPlayer.removeAuthority (netId);
		}
	}

	[Command]
	void CmdSetPositioned(bool state) {
		m_positioned = state;
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	public void OnGvrPointerHover(PointerEventData eventData) {
		Vector3 reticleInWord;
		Vector3 reticleLocal;
		reticleInWord = eventData.pointerCurrentRaycast.worldPosition;
		reticleLocal = transform.InverseTransformPoint (reticleInWord);
		//m_debugText.text = "x: " + reticleLocal.x / bbdim.x + " y: " + reticleLocal.y/bbdim.y;
	}

	public void OnPointerEnter (PointerEventData eventData) {
		if (m_positioned) {
			m_target = true;
			if (!m_looping)
				LocalPlayer.singleton.CmdSetObjectHitState (netId, true);
			if (m_drawingSequence) {
				m_sequencer.addTime ();
				Debug.Log ("Add a point to the sequence");
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		if (m_positioned) {
			m_target = false;
			if (!m_looping)
				LocalPlayer.singleton.CmdSetObjectHitState (netId, false);
			if (m_drawingSequence) {
				m_sequencer.addTime ();
				Debug.Log ("Add a point to the sequence");
			}
		}
	}

	public void OnPointerClick (PointerEventData eventData) {
		if (!m_positioned)
			return;
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (GvrController.TouchPos.y > .85f) {
			LocalPlayer.singleton.CmdActivateTimedDestroy (netId);
		}
	}
		
	public override void OnNetworkDestroy() {
		Debug.Log ("EXTERMINATE!");
		if (isServer) {
			Debug.Log ("Exterminating");
			if (!m_preloaded)
				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
		}
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			LocalPlayer.singleton.CmdSetObjectPositioned(netId,false);
		}
	}
	#endif

	// This is only called from the LocalPlayer proxy server command
	public void setHit(bool state) {
		objectHit = state;
	}

	// This is only called from the LocalPlayer proxy server command
	public void toggleLooping() {
		m_looping = !m_looping;
	}

	// This is only called from the LocalPlayer proxy server command
	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	void playSound(bool hit) {
		objectHit = hit;
//		if (m_looping)
//			return;
		if (hit) {
			m_wordSource.Play ();
		} else {
			m_wordSource.Stop ();
		}
	}

	void fetchAudio(string filename) {
		randomizePaperBall ();
		if (hasAuthority) { // we created the sound clip so it's probably still in memory
			m_wordSource.clip = NonVerbalRecord.singleton.mostRecentClip;
		} else {
			StartCoroutine(Webserver.singleton.GetAudioClip (filename, 
				(newclip) => { m_wordSource.clip = newclip;}));
		}
	}

	void randomizePaperBall() {
		Mesh mesh;
		Vector3[] verts;

		mesh = gameObject.GetComponent<MeshFilter>().mesh;
		verts = mesh.vertices;
		for(int i = 0; i < verts.Length; i++)
		{
			verts[i] *= Random.Range (.7f, 1.3f);
		}
		mesh.vertices = verts;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		SphereCollider col = gameObject.GetComponent<SphereCollider> ();
		Vector3 maxvert;
		maxvert = mesh.bounds.max;
		col.radius = Mathf.Max(Mathf.Max(maxvert.x,maxvert.y),maxvert.z)+ .02f;
	}

	public void setLooping(bool val) {
		m_looping = val;

		if (m_looping) {
//			m_wordSource.Play ();
			m_highlight.ConstantOnImmediate ();
//			m_sequencer.startSequencer ();
			LocalPlayer.singleton.CmdObjectStartSequencer(netId);
		} else {
//			m_wordSource.Stop ();
			m_highlight.ConstantOffImmediate ();
//			m_sequencer.stopSequencer ();
			LocalPlayer.singleton.CmdObjectStopSequencer(netId);
		}
	}

	public void setDrawing(bool val) {
		m_drawingSequence = val;
		if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		} else {
			m_highlight.FlashingOff ();
		}
	}
}

	
