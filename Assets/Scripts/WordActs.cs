using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using HighlightingSystem;

public class WordActs : SoundObjectActs
{
    private AudioSource m_wordSource;
    public WordSequencer m_sequencer;
    private int m_granularSlot;
	[SyncVar]
	private float m_granOffset = 0;
	private float m_localGranOffset = 0; // This one is not networked..for doing the sequencer.
	private AudioMixer m_mixer;

	private float m_xspace = 0;
	[HideInInspector]
	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);

	[HideInInspector][SyncVar]
	public string m_wordstr = "";
	[HideInInspector][SyncVar]
	public float m_scale = 1.0f;
	private Vector3 extent_i, position_i; // Using 'i' as a base char to correct shifting

	public GameObject alphabet;
	public float m_destroyDelay = 360; // The average amount of time in seconds to way for letters to die.

	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	// [HideInInspector] public bool m_preloaded = false;

	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<AudioSource> ();
		m_wordSource.loop = false;

		string ci = "i";
		MeshFilter[] letters = alphabet.GetComponentsInChildren<MeshFilter> ();
		extent_i = new Vector3 ();
		foreach (MeshFilter letter in letters) {
			if (letter.name == ci) {
				extent_i = letter.sharedMesh.bounds.extents;
				position_i = letter.transform.position;
				break;
			}
		}
		m_xspace = extent_i.x/2.5f;

		m_highlight = GetComponent<Highlighter> ();
		m_highlight.ConstantParams (HighlightColour);
		m_sequencer = GetComponent<WordSequencer> ();

	}

	public override void OnStartClient ()
	{
		base.OnStartClient ();
		fetchAudio (m_serverFileName);
	}

	void Start() {
		addLetters (m_wordstr);

		if (m_looping) {
			// Debug.LogWarning ("Looping: " + netId);
			m_highlight.ConstantOnImmediate (HighlightColour);
			m_sequencer.setCometVisibility (true);
			if (!m_sequencer.loadedFromScene)
				IAAPlayer.localPlayer.CmdGetSoundObjectSequencePath (netId);
			else
				IAAPlayer.localPlayer.CmdSoundObjectStartSequencer (netId);
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

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)

	public override void OnGvrPointerHover(PointerEventData eventData) {
		if (!m_looping) {
			m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
			IAAPlayer.localPlayer.CmdWordSetGranOffset (netId, m_granOffset);
		}
	}

	public override void OnPointerEnter (PointerEventData eventData) {
		m_target = true;
		if (m_positioned) {
			m_target = true;
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
				IAAPlayer.getAuthority (netId);
			}
			if (m_drawingPath) {
				m_sequencer.addTime ();
			}
		}
	}

	public override void OnPointerExit(PointerEventData eventData){
		m_target = false;
		if (m_positioned) {
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
				IAAPlayer.removeAuthority (netId);
			}
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
//		if (isServer) {
//			Debug.Log ("Exterminating");
//			if (!m_preloaded && !m_saved)
//				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
//		}
	}
		
	// Proxy Functions (START)
	// These are only called from the LocalPlayer proxy server command
		
	// Proxy Functions (END)

	// Hook Functions (START)

	public override void playSound(bool hit) {
		objectHit = hit;
		// Debug.LogWarning ("Plaing sound for " + netId);
		if (m_mixer == null) {
			return;
		}
		if (hit) {
			m_mixer.SetFloat ("Rate", 100f);
		} else {
			m_mixer.SetFloat ("Rate", 0f);
		}
	}


	public override void setLooping(bool loop) {
		m_looping = loop;
		if (IAAPlayer.localPlayer == null) {
			return;
		}
		Debug.LogWarning ("Seting Looping End");
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			IAAPlayer.localPlayer.CmdSoundObjectStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdSoundObjectStopSequencer(netId);
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
		float maxDescent = 0.0f, maxAscend = 0.0f, yFloat = 0.0f;
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

					Vector3 newLocalPosition = letterpos - lettercentre + extent;
					float descent = extent.y - letter.transform.position.y - (extent_i.y - position_i.y);
					maxDescent = Mathf.Max (maxDescent, descent);
					maxAscend = Mathf.Max (maxAscend, extent.y + letter.transform.position.y);
					newLocalPosition.y -= descent;

					boxsize.Set (boxsize.x + m_xspace + extent.x * 2f, Mathf.Max (boxsize.y, maxAscend + maxDescent), Mathf.Max (boxsize.z, extent.z * 2));
					newletter.transform.localPosition = newLocalPosition;
					letterpos.x += extent.x * 2f + m_xspace;
					yFloat = Mathf.Max (yFloat, maxAscend - boxsize.y / 2f);

					break;
				}
			}
		}

		Vector3 deltaPos = boxsize / 2f;
		deltaPos.y = yFloat;
		newword.transform.localPosition -= deltaPos;

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

	protected override void setVolumeFromHeight(float y) {
		float vol = Mathf.Clamp(-50+y/1.8f*56f, -50f,6f);
		// Debug.Log ("y = " + y + " Vol = " + vol);
		m_mixer.SetFloat("Volume", vol);
	}
		
	void setUpMixer() {
		m_mixer = Resources.Load ("grain" + (m_granularSlot + 1).ToString ()) as AudioMixer;
		m_wordSource.outputAudioMixerGroup = m_mixer.FindMatchingGroups ("Master") [0];
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
