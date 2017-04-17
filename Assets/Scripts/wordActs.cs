﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class wordActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler 
#endif
{

	private static Vector3 m_relpos = new Vector3(0.0f,1.6f,0.0f);
	private static Vector3 m_laserdif = new Vector3(0f,0f,0f);
	private float m_distanceToPointer = 1.0f;
	private GvrAudioSource m_wordSource;

	private float m_xspace = 0;

	public GameObject alphabet;
	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);
	public Text m_debugText = null;

	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	public bool m_preloaded = false;
	[SyncVar]
	public bool m_positioned = false;
	// The hook should only be called once because the word will be set once
	[SyncVar]
	public string m_wordstr = "";
	[SyncVar]
	public float m_scale = 1.0f;
	[SyncVar]
	public string m_serverFileName = "";

	[SyncVar (hook ="playWord")]
	bool wordHit = false;

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
	}

	void Start() {
		addLetters (m_wordstr);
		fetchAudio (m_serverFileName);
	}

	// Update is called once per frame
	void Update () {
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if (isClient && !m_positioned && hasAuthority) {
			Vector3 pos = GvrController.ArmModel.pointerRotation * Vector3.forward*m_distanceToPointer + 
				GvrController.ArmModel.pointerPosition + m_relpos + m_laserdif;
//			pos.x -= 0.5f * transform.localScale.x * bbdim.x;
			transform.position = pos;
			transform.rotation = GvrController.ArmModel.pointerRotation;
			if (GvrController.ClickButtonUp) {
				StartCoroutine(setPositionedState(true));
			}
		}
		#endif
	}

	IEnumerator setPositionedState(bool state) {
		if (!hasAuthority)
			LocalPlayer.getAuthority (netId);
		yield return new WaitUntil(() => hasAuthority == true);
		CmdSetPositioned (state);

		if (state == true) {
			yield return null;
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
		StartCoroutine(setWordHitState(true));
	}

	public void OnPointerExit(PointerEventData eventData){
		StartCoroutine(setWordHitState(false));
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			CmdDestroyWord();
		} 
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			m_laserdif = eventData.pointerCurrentRaycast.worldPosition - (GvrController.ArmModel.pointerRotation * Vector3.forward * m_distanceToPointer+m_relpos);
			// take control again
			StartCoroutine(setPositionedState(false));
		}
	}
	#endif

	[Command]
	void CmdDestroyWord() {
		if (!m_preloaded)
			StartCoroutine(Webserver.singleton.DeleteAudioClip (m_serverFileName));
		Destroy (gameObject);
	}

	IEnumerator setWordHitState(bool state) {
		if (!hasAuthority)
			LocalPlayer.getAuthority (netId);
		yield return new WaitUntil(() => hasAuthority == true);
		CmdSetWordHit (state);

		// I'm doing this so that the player retains authority while they are on a word
		// But don't pull the rug out if we're currently positioning the word
		if (state == false && m_positioned) {
			yield return null;
			LocalPlayer.removeAuthority (netId);
		}
	}

	[Command]
	void CmdSetWordHit(bool state) {
		wordHit = state;
	}

	void playWord(bool hit) {
		if (hit) {
			Debug.Log ("play the word");
			m_wordSource.Play ();
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
}
