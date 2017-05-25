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
	bool m_pressOrigin = false; // We were the origin of the last button press
	const float m_holdtime = .5f; // seconds until we call it a press&hold.

	[SyncVar]
	public string m_serverFileName = "";
	public Text m_DebugText;
	Highlighter m_highlight;
	NonVerbalSequencer m_sequencer;
	bool m_drawingPath = false;
	Plane m_drawingPlane = new Plane ();

	float m_distanceFromPointer = 1.0f;
	GvrAudioSource m_wordSource;
	 
	GameObject m_laser = null;
	GameObject m_reticle = null;

	Quaternion m_rotq;
	[SyncVar] // This needs to be a syncvar so we can vary the volume when the object is moving
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
	[SyncVar (hook = "setDrawingHighlight")]
	bool m_drawingSequence = false;
	Vector3 m_pathNormal = new Vector3(); // we'll set this to be the vector from the object to the camera.


	GameObject laser {
		get {
			if (m_laser == null) 
				m_laser = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Laser").gameObject;
			return m_laser;
		}
	}

	GameObject reticle {
		get {
			if (m_reticle == null)
				m_reticle = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Laser/Reticle").gameObject;
			return m_reticle;
		}
	}

	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
		m_wordSource.loop = true;
		m_highlight = GetComponent<Highlighter> ();
		Color col = new Color (204, 102, 255); // a purple
		m_highlight.ConstantParams (col);

		m_sequencer = GetComponent<NonVerbalSequencer> ();
	}

	public override void OnStartClient() {
		//Make sure we are highlighted if we're looping or drawing at startup.
		if (m_looping) {
			m_highlight.ConstantOnImmediate ();
			m_sequencer.setCometVisibility (true);
		} else if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		}
	}

	void Update () {
		if (!isClient)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if (GvrController.ClickButtonUp)
			m_pressOrigin = false;
		
		if (!m_positioned || m_moving) {
			if (hasAuthority) {
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
					IAAPlayer.localPlayer.CmdSetObjectMovingState (netId,false);
					IAAPlayer.localPlayer.CmdSetObjectPositioned(netId,true);
				}
			}
			setVolumeFromHeight (transform.position.y);
		} else if (m_positioned && !m_moving) {
			if (m_target && m_pressOrigin && GvrController.ClickButton) {
				m_presstime += Time.deltaTime;
				if (!m_presshold && m_presstime > m_holdtime) {
					m_presshold = true;
					if (GvrController.TouchPos.x > .85f) {
						m_drawingPlane.SetNormalAndPosition((Camera.main.transform.position-reticle.transform.position).normalized,reticle.transform.position);
						if (m_looping)
							IAAPlayer.localPlayer.CmdToggleObjectLoopingState(netId);
						IAAPlayer.localPlayer.CmdSetObjectDrawingSequence(netId,true);
						IAAPlayer.localPlayer.CmdSetObjectHitState (netId, false);
						IAAPlayer.localPlayer.CmdSetObjectHitState (netId, true);
						m_sequencer.startNewSequence();
						m_drawingPath = true;
					}
				}
			} else if (GvrController.ClickButtonUp) {
				// We put this here because we could be releasing outside of the original target
				if (GvrController.TouchPos.x > .85f) {
					if (m_drawingPath) {
						m_sequencer.endSequence();
						m_drawingPath = false;
						IAAPlayer.localPlayer.CmdSetObjectDrawingSequence(netId,false);
						if (!m_looping)
							IAAPlayer.localPlayer.CmdToggleObjectLoopingState(netId);
					} else if (m_target) {
						IAAPlayer.localPlayer.CmdToggleObjectLoopingState (netId);
					}
				}
				m_presshold = false;
			}
		}

		#endif
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
				IAAPlayer.localPlayer.CmdSetObjectHitState (netId, true);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		if (m_positioned) {
			m_target = false;
			if (!m_looping)
				IAAPlayer.localPlayer.CmdSetObjectHitState (netId, false);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerClick (PointerEventData eventData) {
		if (!m_positioned)
			return;
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (GvrController.TouchPos.y > .85f) {
			IAAPlayer.localPlayer.CmdActivateTimedDestroy (netId);
		}
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			IAAPlayer.localPlayer.CmdSetObjectMovingState (netId,true);
			IAAPlayer.localPlayer.CmdSetObjectPositioned(netId,false);
		}
		if (m_positioned && !m_moving) { // We are a candidate for presshold
			m_pressOrigin = true;
			m_presstime = 0;
			m_presshold = false;
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



	public override void OnNetworkDestroy() {
		Debug.Log ("EXTERMINATE!");
		if (isServer) {
			Debug.Log ("Exterminating");
			if (!m_preloaded)
				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
		}
	}

	// Proxy Function (START)
	// These are only called from the LocalPlayer proxy server command

	public void setHit(bool state) {
		objectHit = state;
	}

	public void setMovingState(bool state) {
		m_moving = state;
	}

	public void toggleLooping() {
		m_looping = !m_looping;
	}

	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	// Proxy Functions (END)


	public void playSound(bool hit) {
		objectHit = hit;
//		if (m_looping)
//			return;
		if (hit) {
			m_wordSource.Play ();
		} else {
			m_wordSource.Stop ();
		}
	}

	void setVolumeFromHeight(float y) {
		float dbvol = Mathf.Clamp(-50+y/1.8f*56f, -50f,6f);
		float vol = Mathf.Pow(10.0f, dbvol/20.0f);
		m_wordSource.volume = vol;
	}

	void fetchAudio(string filename) {
		randomizePaperBall ();
		if (hasAuthority) { // we created the sound clip so it's probably still in memory
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

	public void setLooping(bool val) {
		m_looping = val;

		if (m_looping) {
			m_highlight.ConstantOnImmediate ();
			IAAPlayer.localPlayer.CmdObjectStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdObjectStopSequencer(netId);
		}
	}

	public void setDrawingHighlight(bool val) {
		m_drawingSequence = val;
		if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		} else {
			m_highlight.FlashingOff ();
		}
	}
}

	
