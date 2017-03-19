using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class wordActs : NetworkBehaviour, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler {

	private static Vector3 m_relpos = new Vector3(0.0f,1.6f,0.0f);
	private static Vector3 m_laserdif = new Vector3(0f,0f,0f);
	private bool m_positioned = false;
	private float m_distanceToPointer = 1.0f;
	private GvrAudioSource m_wordSource;

	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);
	public Text m_debugText = null;

	// Use this for initialization
	void Start () {
		m_wordSource = GetComponent<GvrAudioSource> ();
	}

	// Update is called once per frame
	void Update () {
		if (!m_positioned) {
			Vector3 pos = GvrController.ArmModel.pointerRotation * Vector3.forward*m_distanceToPointer + 
				GvrController.ArmModel.pointerPosition + m_relpos + m_laserdif;
//			pos.x -= 0.5f * transform.localScale.x * bbdim.x;
			transform.position = pos;
			transform.rotation = GvrController.ArmModel.pointerRotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
			}
		}
	}

	public void OnGvrPointerHover(PointerEventData eventData) {
		Vector3 reticleInWord;
		Vector3 reticleLocal;
		reticleInWord = eventData.pointerCurrentRaycast.worldPosition;
		reticleLocal = transform.InverseTransformPoint (reticleInWord);
		m_debugText.text = "x: " + reticleLocal.x / bbdim.x + " y: " + reticleLocal.y/bbdim.y;
	}

	public void OnPointerEnter (PointerEventData eventData) {
		m_wordSource.Play ();
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			Destroy (this.gameObject);
		} 
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			m_laserdif = eventData.pointerCurrentRaycast.worldPosition - (GvrController.ArmModel.pointerRotation * Vector3.forward * m_distanceToPointer+m_relpos);
			m_positioned = false;
		}
	}
}
