using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using HighlightingSystem;

public class wordActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler 
#endif
{
	static Color HighlightColour = new Color(0.639216f, 0.458824f, 0.070588f);

	private GvrAudioSource m_wordSource;
	private int m_granularSlot;
	[SyncVar]
	private float m_granOffset = 0;
	private float m_localGranOffset = 0; // This one is not networked..for doing the sequencer.
	private AudioMixer m_mixer;

	private bool m_saved = false; // Whether the word is part of an environment that has been saved.

	// This is to set a timer when a person clicks. If we linger long enough 
	// we call it a press&hold.
	float m_presstime = 0;
	bool m_presshold = true;
	bool m_pressOrigin = false;
	bool m_target = false;
	const float m_holdtime = .5f; // seconds until we call it a press&hold.

	private float m_distanceFromPointer = 1.0f;
	private Highlighter m_highlight;

	private Quaternion m_rotq;
	private Quaternion m_originalLaserRotation;
	private Vector3 m_originalHitPoint;

	[SyncVar] // Needs to be networked so we can change the volume that is based on height
	private bool m_moving = false;
	private GameObject m_laser = null;
	private GameObject m_reticle = null;
	private GameObject m_controller = null;

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

	GameObject controller {
		get {
			if (m_controller == null)
				m_controller = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Controller").gameObject;
			return m_controller;
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

	// Using 'i' as a base char to correct shifting
	private Vector3 extent_i;


	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
		m_wordSource.loop = false;

		string ci = "i";
		MeshFilter[] letters = alphabet.GetComponentsInChildren<MeshFilter> ();
		extent_i = new Vector3 ();
		foreach (MeshFilter letter in letters) {
			if (letter.name == ci) {
				extent_i = letter.sharedMesh.bounds.extents;
				break;
			}
		}
		m_xspace = extent_i.x/2.5f;

		m_highlight = GetComponent<Highlighter> ();
		m_highlight.ConstantParams (HighlightColour);

		m_sequencer = GetComponent<WordSequencer> ();
	}

	void Start() {
		addLetters (m_wordstr);
		fetchAudio (m_serverFileName);
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			m_sequencer.setCometVisibility (true);
			IAAPlayer.localPlayer.CmdGetWordSequencePath (netId);
		} else if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		}
	}
		
	// Update is called once per frame
	void Update () {
		if (!isClient)
			return;
		bool volumeChanged = false;
		if (!m_positioned || m_moving)
			volumeChanged = true;

		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)

		if (GvrController.ClickButtonUp)
			m_pressOrigin = false;
		
		if (!m_positioned || m_moving) {
			if (hasAuthority) {
				if (!m_moving) {
					// Condition: not positioned and not moving, has authority
					// Forward is a unit vector. Strange
					Vector3 newpos = laser.transform.position + laser.transform.forward;

					// Make sure we don't go through the floor
					if (newpos.y < 0.05f)
						newpos.y = 0.05f;
					transform.position = newpos;
				} else {
					// Condition: not positioned and moving, has authority
					// We have picked a word and we're moving it...
					Vector3 newdir = m_rotq * laser.transform.forward;
					Vector3 newpos = laser.transform.position + newdir * m_distanceFromPointer;
					// Make sure we don't go through the floor
					if (newpos.y < 0.05f)
						newpos.y = 0.05f;
					transform.position = newpos;
				}
					
				// transform.rotation = laser.transform.rotation;



				if (GvrController.ClickButtonUp) {
					m_positioned = true;
					m_moving = false;
					IAAPlayer.localPlayer.CmdSetWordMovingState(netId,false);
					IAAPlayer.localPlayer.CmdSetWordPositioned(netId, true);
					if (!m_target && !m_looping)
						IAAPlayer.localPlayer.CmdSetWordHitState (netId, false);
				}
			}
		} else if (m_positioned && !m_moving) {
			if (m_target && m_pressOrigin && GvrController.ClickButton) {
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
						IAAPlayer.localPlayer.CmdSetWordDrawingSequence(netId,false);
						if (!m_looping)
							IAAPlayer.localPlayer.CmdToggleWordLoopingState(netId);
						IAAPlayer.removeAuthority(netId);
					} else if (m_target) {
						IAAPlayer.localPlayer.CmdToggleWordLoopingState (netId);
						IAAPlayer.removeAuthority(netId);
					}
				}
				m_presshold = false;
			}
		}
		#endif
		if (volumeChanged)
			setVolumeFromHeight(transform.position.y);	
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)

	public void OnGvrPointerHover(PointerEventData eventData) {
		if (!m_looping) {
			m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
			IAAPlayer.localPlayer.CmdWordSetGranOffset (netId, m_granOffset);
		}
	}

	public void OnPointerEnter (PointerEventData eventData) {
		m_target = true;
		if (m_positioned) {
			m_target = true;
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetWordHitState (netId, true);
				IAAPlayer.getAuthority (netId);
			}
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		m_target = false;
		if (m_positioned) {
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetWordHitState (netId, false);
				IAAPlayer.removeAuthority (netId);
			}
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
			IAAPlayer.localPlayer.CmdDestroyObject (netId);
		}
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;

			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_originalLaserRotation = laser.transform.rotation;
			m_originalHitPoint = eventData.position;

			Vector3 controllerPosition = laser.transform.position;
//			Vector3 fwd = GvrController.Orientation * Vector3.forward;
			Vector3 fwd = laser.transform.forward;
			Debug.Log ("FWD Direction: " + fwd * 100);
			Debug.Log ("Laser Rotatoin: " + laser.transform.rotation * Vector3.forward * 100); 
			Debug.Log ("Controller Pos: " + controller.transform.position * 100);
			Debug.Log ("Laser Pos: " + laser.transform.position * 100);


			var laserTransform = laser.transform;
			RaycastHit pointingAtWhat;
			if (Physics.Raycast (laserTransform.position, fwd, out pointingAtWhat)) {
				Debug.Log (pointingAtWhat.point);
			} else {
				Debug.Log ("Didn't get a hit point.");
			}


			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			IAAPlayer.localPlayer.CmdSetWordMovingState(netId,true);
			IAAPlayer.localPlayer.CmdSetWordPositioned(netId,false);
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
			m_sequencer.addPos (gameObject.transform.InverseTransformPoint (RayDrawingPlaneIntersect (reticle.transform.position)));
			if (m_target) {
				m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
				m_sequencer.addScrub (m_granOffset);
			}
		}
		if (m_looping) {
			if (m_mixer)
				m_mixer.SetFloat ("Offset", m_localGranOffset);
		} else {
			if (m_mixer)
				m_mixer.SetFloat ("Offset", m_granOffset);
		}
	}

	Vector3 getScrubValue() {
		return transform.InverseTransformPoint (reticle.transform.position);
	}

	public void setGranOffset(float s) {
		m_granOffset = s;
	}

	public void setLocalGranOffset(float s) {
		m_localGranOffset = s;
	}

	Vector3 RayDrawingPlaneIntersect(Vector3 p) {
		float enter;
		Vector3 raydir = (p - Camera.main.transform.position).normalized;
		Ray pathray = new Ray (Camera.main.transform.position, raydir);
		m_drawingPlane.Raycast (pathray, out enter);
		return Camera.main.transform.position + raydir * enter;
	}


	public override void OnNetworkDestroy() {
		separateLetters ();
		GranularUploadHandler.singleton.setSlotToEmpty (m_granularSlot);
		m_mixer.SetFloat ("Rate", 0f);
		Debug.Log ("EXTERMINATE!");
		if (isServer) {
			Debug.Log ("Exterminating");
			if (!m_preloaded && !m_saved)
				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
		}
	}

	public bool saved {
		get {
			return m_saved;
		}
		set {
			m_saved = value;
		}
	}
		
	// Proxy Functions (START)
	// These are only called from the LocalPlayer proxy server command

	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	public void toggleLooping() {
		m_looping = !m_looping;
	}

	public void setHit(bool state) {
		wordHit = state;
	}

	public void setMovingState(bool state) {
		m_moving = state;
	}

	// Proxy Functions (END)


	// Hook Functions (START)

	public void playWord(bool hit) {
		wordHit = hit;
		if (hit) {
			m_mixer.SetFloat ("Rate", 100f);
		} else {
			m_mixer.SetFloat ("Rate", 0f);
		}
	}


	void setLooping(bool loop) {
		m_looping = loop;
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			IAAPlayer.localPlayer.CmdWordStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdWordStopSequencer(netId);
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

	// Hook Functions (END)

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

					lettercentre = letter.sharedMesh.bounds.center; // Always zero
					extent = letter.sharedMesh.bounds.extents; // Half of their size

					boxsize.Set (boxsize.x + m_xspace + extent.x * 2, Mathf.Max (boxsize.y, extent.y*2), Mathf.Max (boxsize.z, extent.z * 2));

					Vector3 newLocalPosition = letterpos - lettercentre + extent;
					float descent = extent.y - letter.transform.position.y - extent_i.y;
					newLocalPosition.y -= descent;


					newletter.transform.localPosition = newLocalPosition;
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
			m_granularSlot = GranularUploadHandler.singleton.uploadSample(SpeechToTextToAudio.singleton.mostRecentClip);
			setUpMixer ();
		} else {
			StartCoroutine(Webserver.singleton.GetAudioClip (clipfn, 
				(newclip) => {
					m_granularSlot = GranularUploadHandler.singleton.uploadSample(newclip);
					setUpMixer();
				}));
		}
	}

	void setVolumeFromHeight(float y) {
		float vol = Mathf.Clamp(-50+y/1.8f*56f, -50f,6f);
		// Debug.Log ("y = " + y + " Vol = " + vol);
		m_mixer.SetFloat("Volume", vol);
	}
		
	void setUpMixer() {
		m_mixer = Resources.Load ("grain" + (m_granularSlot + 1).ToString ()) as AudioMixer;
		m_wordSource.audioSource.outputAudioMixerGroup = m_mixer.FindMatchingGroups ("Master") [0];
		//gameObject.AddComponent<SilentAudioSource> ();
		m_mixer.SetFloat ("Sample", m_granularSlot);
		m_mixer.SetFloat("Speed", 1.0f);
		m_mixer.SetFloat("Offset", 0.5f);
		m_mixer.SetFloat("Window", 0.1f);
		m_mixer.SetFloat("Rate", 0f);
		m_mixer.SetFloat("RndSpeed", 0f);
		m_mixer.SetFloat("RndOffset", 0.1f);
		m_mixer.SetFloat("RndWindow", 0.01f);
		m_mixer.SetFloat("RndRate", 0f);

		setVolumeFromHeight (transform.position.y);

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
