using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HighlightingSystem;


public class wordActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler 
#endif
{

	// This is to set a timer when a person clicks. If we linger long enough 
	// we call it a press&hold.
	float m_presstime = 0;
	bool m_presshold = true;
	bool m_target = false;
	const float m_holdtime = .5f; // seconds until we call it a press&hold.

	private float m_distanceFromPointer = 1.0f;
	private GvrAudioSource m_wordSource;
	private Highlighter m_highlight;

	private Quaternion m_rotq;
	private bool m_moving = false;
	private GameObject m_laser = null;
	private GameObject m_reticle = null;

	private Plane m_drawingPlane;
	private bool m_drawingPath = false; 
	private WordSequencer m_sequencer;

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

	private float m_xspace = 0;
	[HideInInspector]
	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);

	public GameObject alphabet;
	public float m_destroyDelay = 360; // The average amount of time in seconds to way for letters to die.


	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	[HideInInspector]
	public bool m_preloaded = false;
	[HideInInspector][SyncVar]
	public bool m_positioned = false;
	// The hook should only be called once because the word will be set once
	[HideInInspector][SyncVar]
	public string m_wordstr = "";
	[HideInInspector][SyncVar]
	public float m_scale = 1.0f;
	[HideInInspector][SyncVar]
	public string m_serverFileName = "";

	[SyncVar (hook ="playWord")]
	bool wordHit = false;
	[SyncVar (hook ="setLooping")]
	bool m_looping = false;
	[SyncVar (hook = "setDrawingHighlight")]
	bool m_drawingSequence = false;


	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
		m_wordSource.loop = false;

		string ci = "i";
		MeshFilter[] letters = alphabet.GetComponentsInChildren<MeshFilter> ();
		Vector3 extent = new Vector3();
		foreach (MeshFilter letter in letters) {
			if (letter.name == ci) {
				extent = letter.sharedMesh.bounds.extents;
				break;
			}
		}
		m_xspace = extent.x/2.5f;

		m_highlight = GetComponent<Highlighter> ();
		Color col = new Color (204, 102, 255); // a purple
		m_highlight.ConstantParams (col);

		m_sequencer = GetComponent<WordSequencer> ();
	}

	void Start() {
		addLetters (m_wordstr);
		fetchAudio (m_serverFileName);
	}

	public override void OnStartClient() {
		//Make sure we are highlighted if we're looping or drawing at startup.
		if (m_looping) {
			m_highlight.ConstantOnImmediate ();
		} else if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		}
	}

	// Update is called once per frame
	void Update () {
		if (!isClient)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if ((!m_positioned && hasAuthority) || (m_moving)) {
			if (!m_moving) {
				transform.position = laser.transform.position + laser.transform.forward;
			} else {
				// We have picked a word and we're moving it...
				Vector3 newdir = m_rotq*laser.transform.forward;
				transform.position = laser.transform.position+newdir*m_distanceFromPointer;
			}
			transform.rotation = laser.transform.rotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
				m_moving = false;
				IAAPlayer.localPlayer.CmdSetWordPositioned(netId, true);
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
						m_drawingPlane.SetNormalAndPosition((Camera.main.transform.position-reticle.transform.position).normalized,reticle.transform.position);
						if (m_looping)
							IAAPlayer.localPlayer.CmdToggleWordLoopingState(netId);
						IAAPlayer.localPlayer.CmdSetWordDrawingSequence(netId,true);
						IAAPlayer.localPlayer.CmdSetWordHitState (netId, false);
						IAAPlayer.localPlayer.CmdSetWordHitState (netId, true);
						m_sequencer.startNewSequence();
						m_drawingPath = true;
					}
				}
			} else if (GvrController.ClickButtonUp) {
				if (GvrController.TouchPos.x > .85f) {
					if (m_drawingPath) {
						//m_sequencer.addTime();
						m_sequencer.endSequence();
						m_drawingPath = false;
						IAAPlayer.localPlayer.CmdSetObjectDrawingSequence(netId,false);
						if (!m_looping)
							IAAPlayer.localPlayer.CmdToggleObjectLoopingState(netId);
					} else if (m_target) {
						IAAPlayer.localPlayer.CmdToggleObjectLoopingState (netId);
						Debug.Log("Toggle sequencer");
					}
				}
				m_presshold = false;
			}
		}
		#endif
	}

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

	void setPositionedState(bool state) {
		if (!hasAuthority) {
			IAAPlayer.getAuthority (netId);
		}
		CmdSetPositioned (state);

		if (state == true) {
			IAAPlayer.removeAuthority (netId);
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
		if (m_drawingPath) {
			m_sequencer.addScrub (reticleLocal.x);
		}
//		Debug.Log ("x: " + (reticleLocal.x / bbdim.x + 0.5f) + " y: " + (reticleLocal.y / bbdim.y+.5f));
	}

	public void OnPointerEnter (PointerEventData eventData) {
		if (m_positioned) {
			m_target = true;
			if (!m_looping)
				IAAPlayer.localPlayer.CmdSetWordHitState (netId, true);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		if (m_positioned) {
			m_target = false;
			if (!m_looping)
				IAAPlayer.localPlayer.CmdSetWordHitState (netId, false);
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerClick (PointerEventData eventData) {
		if (!m_positioned)
			return;
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (GvrController.TouchPos.y > .85f)
			IAAPlayer.localPlayer.CmdDestroyObject (netId);
		else if (GvrController.TouchPos.x > .85f)
			IAAPlayer.localPlayer.CmdToggleWordLoopingState (netId);
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			IAAPlayer.localPlayer.CmdSetWordPositioned(netId,false);
		}
	}
	#endif
	
	// This is only called from the LocalPlayer proxy server command
	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	public override void OnNetworkDestroy() {
		separateLetters ();
		Debug.Log ("EXTERMINATE!");
		if (isServer) {
			Debug.Log ("Exterminating");
			if (!m_preloaded)
				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
		}
	}

	public void toggleLooping() {
		m_looping = !m_looping;
	}

	public void setHit(bool state) {
		wordHit = state;
	}

	void playWord(bool hit) {
		wordHit = hit;
		if (hit && !m_looping) {
			m_wordSource.Play ();
		}
	}


	void setLooping(bool loop) {
		m_looping = loop;

		if (m_looping) {
			m_highlight.ConstantOnImmediate ();
			IAAPlayer.localPlayer.CmdWordStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdWordStopSequencer(netId);
		}
	}

	void addLetters(string word) {

		GameObject newword = transform.GetChild (0).gameObject;

		GameObject newletter;
		MeshFilter lettermesh;
		Vector3 letterpos = new Vector3 ();
		Vector3 scalevec = new Vector3 (m_scale, m_scale, m_scale);
		Vector3 letterscale = new Vector3 (1f, 1f, 1f);

		newword.transform.localScale = scalevec;

		MeshFilter[] letters = alphabet.GetComponentsInChildren<MeshFilter> ();
		Vector3 lettercentre;
		Vector3 extent = new Vector3();
		Vector3 boxsize = new Vector3 ();
		string lowcaseWord = word.ToLower ();
		foreach (char c in lowcaseWord) {
			if (c == ' ') {
				letterpos.x += m_xspace*2;
				boxsize.x += m_xspace * 2;
				continue;
			}
			foreach (MeshFilter letter in letters) {
				if (letter.name == c.ToString()) {
					lettermesh = Instantiate(letter) as MeshFilter;
					newletter = lettermesh.gameObject;
					newletter.name = c.ToString ();
					newletter.transform.parent = newword.transform;
					newletter.transform.localScale = letterscale;
					newletter.transform.localRotation = Quaternion.identity;
					lettercentre = letter.sharedMesh.bounds.center;
					extent = letter.sharedMesh.bounds.extents;
					boxsize.Set (boxsize.x + m_xspace + extent.x * 2, Mathf.Max (boxsize.y, extent.y*2), Mathf.Max (boxsize.z, extent.z * 2));
					newletter.transform.localPosition = letterpos-lettercentre+extent;
					letterpos.x += extent.x*2 + m_xspace;
					break;
				}
			}
		}

		newword.transform.localPosition -= boxsize / 2f;

		BoxCollider bc = GetComponent<BoxCollider> ();
		bc.size = boxsize;

		bbdim = boxsize;
	}

	void fetchAudio(string clipfn) {
		if (hasAuthority) { // we created the sound clip so it's probably still in memory
			m_wordSource.clip = SpeechToTextToAudio.singleton.mostRecentClip;
		} else {
			StartCoroutine(Webserver.singleton.GetAudioClip (clipfn, (newclip) => { m_wordSource.clip = newclip;}));
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

	public void separateLetters(){
		Debug.Log ("Separating letters");
		// First add a Rigid Body component to the letters
		GameObject letters = gameObject.transform.Find("Letters").gameObject;
		BoxCollider collider;
		Rigidbody rb;
		PhysicMaterial letterbounce = new PhysicMaterial ();
		letterbounce.bounciness = .7f;
		Vector3 rbf = new Vector3 ();
		foreach (Transform letter in letters.transform) {
			rb = letter.gameObject.AddComponent<Rigidbody> ();
			collider = letter.gameObject.AddComponent<BoxCollider> ();
			collider.material = letterbounce;
			rbf.x = Random.Range (-.3f, .3f);
			rbf.y = Random.Range (-3, 3);
			rbf.z = Random.Range (-.3f, .3f);
			rb.AddForce (rbf, ForceMode.VelocityChange);
			TimedDestroy timerscript = letter.gameObject.AddComponent<TimedDestroy> ();
			timerscript.m_destroyTime = Random.Range (m_destroyDelay*.80f, m_destroyDelay*1.20f);
			timerscript.activate ();
			//letter.parent = letter.parent.parent.parent;
		}
		letters.transform.DetachChildren ();
	}
}
