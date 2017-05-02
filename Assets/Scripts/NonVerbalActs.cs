﻿using System.Collections;
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

	[SyncVar]
	public string m_serverFileName = "";
	public Text m_DebugText;
	private Highlighter m_highlight;

	private float m_distanceFromPointer = 1.0f;
	private GvrAudioSource m_wordSource;
	 
	GameObject m_laser = null;

	GameObject laser {
		get {
			if (m_laser == null) 
				m_laser = LocalPlayer.playerObject.transform.FindChild ("GvrControllerPointer/Laser").gameObject;
			return m_laser;
		}
	}

	private Quaternion m_rotq;
	private bool m_moving = false;

	// This indicates that the word was preloaded. It's not a SyncVar
	// so it's only valid on the server which is ok because only
	// the server needs to know. The variable is used to prevent
	// audio clip deletion.
	public bool m_preloaded = false;
	[SyncVar (hook ="playSound")]
	bool objectHit = false;
	[SyncVar]
	public bool m_positioned = false;

	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
		m_highlight = GetComponent<Highlighter> ();
	}

	void Update () {
		if (isServer)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if (isClient && ((!m_positioned && hasAuthority) || (m_moving))) {
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
			LocalPlayer.singleton.CmdSetObjectHitState (netId, true);
			m_highlight.ConstantOnImmediate ();
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		if (m_positioned) {
			LocalPlayer.singleton.CmdSetObjectHitState (netId, false);
			m_highlight.ConstantOffImmediate ();
		}
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			LocalPlayer.singleton.CmdDestroyObject (netId);
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

	void playSound(bool hit) {
		objectHit = hit;
		if (hit)
			m_wordSource.Play();
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
}
	
