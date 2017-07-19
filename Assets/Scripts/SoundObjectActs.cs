using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using HighlightingSystem;

public class SoundObjectActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
#endif
{
	static protected Color HighlightColour = new Color(0.639216f, 0.458824f, 0.070588f);
	// This is to set a timer when a person clicks. If we linger long enough 
	// we call it a press&hold.
	protected float m_presstime = 0;
	protected bool m_presshold = true;
	protected bool m_pressOrigin = false;
	protected bool m_target = false; // Whether the reticle is on this object
	protected const float m_holdtime = .5f; // seconds until we call it a press&hold.

	protected bool m_saved = false; // Whether the word is part of an environment that has been saved.
	[SyncVar] // Needs to be networked so we can change the volume that is based on height
	protected bool m_moving = false;
	protected GameObject m_laser = null;
	protected GameObject m_reticle = null;
	protected GameObject m_controller = null;
	protected GameObject m_tmpSphere = null;

	protected float m_distanceFromPointer = 1.0f;
	protected Quaternion m_rotq;
	protected Vector3 m_originalHitPoint;
	protected Vector3 m_originalHitPointLocal, m_hitPointToController;
	protected Quaternion m_originalControllerRotation, m_originalLaserRotation;

	[HideInInspector][SyncVar]
	public string m_serverFileName = "";
	[HideInInspector][SyncVar]
	public bool m_positioned = false;
	[SyncVar (hook = "playSoundHook")]
	protected bool objectHit = false;
	[SyncVar (hook = "setLoopingHook")]
	public bool m_looping = false;
	[SyncVar (hook = "setDrawingHighlight")]
	protected bool m_drawingSequence = false;

	protected bool m_drawingPath = false;
	protected Plane m_drawingPlane = new Plane ();
	protected Highlighter m_highlight;

	protected GameObject laser {
		get {
			if (m_laser == null) {
				m_laser = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Laser").gameObject;
			}
			return m_laser;
		}
	}

	protected GameObject reticle {
		get {
			if (m_reticle == null) {
				m_reticle = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Laser/Reticle").gameObject;
			}
			return m_reticle;
		}
	}

	protected GameObject controller {
		get {
			if (m_controller == null) {
				m_controller = IAAPlayer.playerObject.transform.Find ("GvrControllerPointer/Controller/ddcontroller").gameObject;
			}
			return m_controller;
		}
	}

	public virtual List<int> getSequenceTrigger() {
		return null;
	}

	public virtual List<Vector3> getSequencePath() {
		return null;
	}

	protected virtual void Update() {
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
					// Forward is a unit vector. Strange
					Vector3 newpos = laser.transform.position + laser.transform.forward;
					// Make sure we don't go through the floor
					if (newpos.y < 0.05f)
						newpos.y = 0.05f;
					transform.position = newpos;
					transform.rotation = GvrController.Orientation;

				} else {
					Transform letterTrans = transform.Find("Letters");
					// We have picked an object and we're moving it...
					Vector3 newdir = m_rotq*laser.transform.forward;
					Vector3 newpos = laser.transform.position+newdir*m_distanceFromPointer;
					if (this.GetType().Equals(typeof(WordActs))) {
						Quaternion deltaRotation = controller.transform.rotation * Quaternion.Inverse(m_originalControllerRotation);
						Vector3 tGlobal = transform.TransformPoint(m_originalHitPointLocal);
						transform.position = tGlobal; 
						letterTrans.localPosition -= m_originalHitPointLocal;
						Vector3 newPosOffset = deltaRotation * m_hitPointToController;
						newpos = controller.transform.position + newPosOffset;
					}
					// Make sure we don't go through the floor
					if (newpos.y < 0.05f)
						newpos.y = 0.05f;
					transform.position = newpos;
					transform.rotation = GvrController.Orientation;

					if (this.GetType().Equals(typeof(WordActs))) {
						letterTrans.localPosition += m_originalHitPointLocal;
						transform.position = transform.TransformPoint(-m_originalHitPointLocal);
					}
				}

				if (GvrController.ClickButtonUp) {
					m_positioned = true;
					m_moving = false;
					IAAPlayer.localPlayer.CmdSetSoundObjectMovingState (netId,false);
					IAAPlayer.localPlayer.CmdSetSoundObjectPositioned(netId,true);
					if (!m_target && !m_looping)
						IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
				}
			}
			volumeChanged = true;
		} else if (m_positioned && !m_moving) {
			if (m_target && m_pressOrigin && GvrController.ClickButton) {
				m_presstime += Time.deltaTime;
				if (!m_presshold && m_presstime > m_holdtime) {
					m_presshold = true;
					if (GvrController.TouchPos.x > .85f) {
						m_drawingPlane.SetNormalAndPosition((Camera.main.transform.position-reticle.transform.position).normalized,reticle.transform.position);
						if (m_looping)
							IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState(netId);
						IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence(netId,true);
						IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
						IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
						// TODO: refactor sequencer
						tmpStartNewSequence();// m_sequencer.startNewSequence();
						m_drawingPath = true;
					}
				}
			} else if (GvrController.ClickButtonUp) {
				// We put this here because we could be releasing outside of the original target
				if (GvrController.TouchPos.x > .85f) {
					if (m_drawingPath) {
						// TODO: refactor sequencer
						tmpEndSequence();// m_sequencer.endSequence();
						m_drawingPath = false;
						IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence(netId,false);
						if (!m_looping)
							IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState(netId);
						if (this.GetType().Equals(typeof(WordActs))) {
							IAAPlayer.removeAuthority(netId);
						}
					} else if (m_target) {
						IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState (netId);
						if (this.GetType().Equals(typeof(WordActs))) {
							IAAPlayer.removeAuthority(netId);
						}
					}
				}
				m_presshold = false;
			}
		}
		#endif
		if (volumeChanged)
			setVolumeFromHeight (transform.position.y);
	}

	protected virtual void tmpStartNewSequence() {
		Debug.Log ("This should not be called");
	}
	protected virtual void tmpEndSequence() {
		Debug.Log ("This should not be called");
	}
	
	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	public virtual void OnGvrPointerHover(PointerEventData eventData) {
		Vector3 reticleInWord;
		Vector3 reticleLocal;
		reticleInWord = eventData.pointerCurrentRaycast.worldPosition;
		reticleLocal = transform.InverseTransformPoint (reticleInWord);
		//m_debugText.text = "x: " + reticleLocal.x / bbdim.x + " y: " + reticleLocal.y/bbdim.y;
	}

	public virtual void OnPointerEnter (PointerEventData eventData) {
		Debug.Log ("Should not be called");
	}

	public virtual void OnPointerExit(PointerEventData eventData) {
		Debug.Log ("Should not be called");
	}

	public virtual void OnPointerClick (PointerEventData eventData) {
		if (!m_positioned)
			return;
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (GvrController.TouchPos.y > .85f) {
			IAAPlayer.localPlayer.CmdActivateTimedDestroy (netId);
		}
	}

	public virtual void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {

			m_originalControllerRotation = controller.transform.rotation;
			m_originalLaserRotation = laser.transform.rotation;
			m_originalHitPoint = eventData.pointerCurrentRaycast.worldPosition;
			m_hitPointToController = m_originalHitPoint - controller.transform.position;
			m_originalHitPointLocal = transform.InverseTransformPoint(m_originalHitPoint);

			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			IAAPlayer.localPlayer.CmdSetSoundObjectMovingState (netId,true);
			IAAPlayer.localPlayer.CmdSetSoundObjectPositioned(netId,false);
		}
		if (m_positioned && !m_moving) { // We are a candidate for presshold
			m_pressOrigin = true;
			m_presstime = 0;
			m_presshold = false;
		}
	}
	#endif

	// Proxy Function (START)
	// These are only called from the LocalPlayer proxy server command

	public void toggleLooping() {
		m_looping = !m_looping;
	}

	public void setHit(bool state) {
		objectHit = state;
	}

	public void setMovingState(bool state) {
		m_moving = state;
	}

	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	public bool saved {
		get {
			return m_saved;
		}
		set {
			m_saved = value;
		}
	}

	// Proxy Functions (END)

	// SyncVar does not call overrided function
	public void playSoundHook(bool state) {
		playSound (state);
	}
	public void setLoopingHook(bool state) {
		setLooping (state);
	}

	public void setDrawingHighlight(bool val) {
		m_drawingSequence = val;
		if (m_drawingSequence) {
			m_highlight.FlashingOn ();
		} else {
			m_highlight.FlashingOff ();
		}
	}

	public virtual void playSound(bool state) {
		Debug.Log ("This should not be called");
	}

	public virtual void setLooping(bool state) {
		Debug.Log ("This should not be called");
	}

	protected virtual void setVolumeFromHeight(float y) {
		Debug.Log ("This should not be called");
	}
}
