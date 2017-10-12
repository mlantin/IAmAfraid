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
    
	[SyncVar]
	private float m_granOffset = 0;
	private float m_localGranOffset = 0; // This one is not networked..for doing the sequencer.
	private Hv_slo_Granular_AudioLib granular = null;
	private bool m_setMetroOn = false; // This is true when we went from off to on...it's to deal with a strange delay bug with the granular plugin

	private float m_xspace = 0;
	[HideInInspector]
	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);
	[HideInInspector][SyncVar]
	public string m_wordstr = "";
	[HideInInspector][SyncVar]
	public float m_scale = 1.0f;
	private Vector3 extent_i, position_i; // Using 'i' as a base char to correct shifting

	public GameObject alphabet;
	[Tooltip("The average amount of time in seconds to way for letters to die.")]
	public float m_destroyDelay = 360;

	// Use this for initialization
	protected override void Awake () {
		base.Awake ();
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
	}

	public override void OnStartClient ()
	{
		base.OnStartClient ();
		fetchAudio (m_serverFileName);
	}

	protected override void Start() {
		addLetters (m_wordstr);
		base.Start ();
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)

//	public override void OnPointerEnter (PointerEventData eventData) {
//		if (!m_looping) {
//			m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
//			setGrainPosition ();
//			IAAPlayer.localPlayer.CmdWordSetGranOffset (netId, m_granOffset);
//		}
//		base.OnPointerEnter (eventData);
//	}

	public override void OnGvrPointerHover(PointerEventData eventData) {
		if (!m_looping && m_soundOwner == IAAPlayer.localPlayer.netId.Value) {
			// Debug.Log ("I'm hovering on you");
			m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
//			setGrainPosition ();
			IAAPlayer.localPlayer.CmdWordSetGranOffset (netId, m_granOffset);
		}
	}

	#endif

	void FixedUpdate() {
		if (m_setMetroOn) {
			granular.SetFloatParameter (Hv_slo_Granular_AudioLib.Parameter.Metro, 1.0f);
			m_setMetroOn = false;
		}
		if (m_opstate == OpState.Op_Recording) {
			// Get the point on the current plane
			//m_sequencer.addPos (gameObject.transform.InverseTransformPoint (reticle.transform.position));
			m_sequencer.addPos (gameObject.transform.InverseTransformPoint (RayDrawingPlaneIntersect (IAAController.reticle.transform.position)));
			if (m_target) {
				m_granOffset = getScrubValue ().x / bbdim.x + 0.5f;
				m_sequencer.addScrub (m_granOffset);
			}
		} 
		setGrainPosition ();
	}

	void setGrainPosition() {
//		if (m_looping) {
//			if (granular)
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, m_localGranOffset);
//
//		} else {
		if (!m_looping && granular) {
				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, m_granOffset);

		}
	}

	Vector3 getScrubValue() {
		return transform.InverseTransformPoint (IAAController.reticle.transform.position);
	}

	public void setGranOffset(NetworkInstanceId playerId, float s) {
		if (playerId.Value == m_soundOwner) {
			m_granOffset = s;
			if (granular)
				granular.SetFloatParameter (Hv_slo_Granular_AudioLib.Parameter.Grainposition, m_granOffset);
		}
	}

	public void setLocalGranOffset(float s) {
		m_localGranOffset = s;
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, m_localGranOffset);
	}

	Vector3 RayDrawingPlaneIntersect(Vector3 p) {
		float enter;
		Vector3 raydir = (p - Camera.main.transform.position).normalized;
		Ray pathray = new Ray (Camera.main.transform.position, raydir);
		m_drawingPlane.Raycast (pathray, out enter);
		return Camera.main.transform.position + raydir * enter; 
	}


	public override void OnNetworkDestroy() {
		base.OnNetworkDestroy ();
		separateLetters ();
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 0.0f);
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
		if (granular == null) {
			return;
		}
		if (hit) {
			m_wordSource.Play ();
			// This doesn't seem to do anything if the word has been off for a while. There is a delay
			// between setting the word to playing and the granular plugin being able to
			// take messages. So I'm going to try setting a variable to wait until the next
			// update to send the metro=1 call.
			m_setMetroOn = true;
			granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 1.0f);
		} else {
			m_wordSource.Stop ();
			m_setMetroOn = false;
			granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 0.0f);
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
			setUpGranular (SpeechToTextToAudio.singleton.mostRecentClip);
		} else {
			StartCoroutine(Webserver.singleton.GetAudioClip (clipfn, 
				(newclip) => {
					setUpGranular(newclip);
				}));
		}
	}
		
	public void setUpGranular(AudioClip newclip) {
		granular = gameObject.AddComponent<Hv_slo_Granular_AudioLib>();
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length, (newclip.samples));
		granular.FillTableWithMonoAudioClip("source_Array", newclip);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 0.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindel_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindelay, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration, 150.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari, 1.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, 0.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari, 1.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate, 1.0f);
		assignMixer ();
		setVolumeFromHeight (transform.position.y);
//		m_wordSource.Play();
		m_wordSource.loop = true;
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
