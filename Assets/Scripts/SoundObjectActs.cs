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
	protected bool m_target = false; // Whether the reticle is on this object
	protected const float m_holdtime = .5f; // seconds until we call it a press&hold.

	protected bool m_saved = false; // Whether the word is part of an environment that has been saved.
	protected GameObject m_laser = null;
	protected GameObject m_reticle = null;
	protected GameObject m_controller = null;
	protected GameObject m_tmpSphere = null;

	protected float m_distanceFromPointer = 1.0f;
	protected Quaternion m_rotq;
	protected Vector3 m_originalHitPoint;
	protected Vector3 m_originalHitPointLocal, m_hitPointToController;
	protected Quaternion m_originalControllerRotation, m_originalLaserRotation;

	public SoundObjectSequencer m_sequencer;
	protected AudioSource m_wordSource;
	[HideInInspector][SyncVar]
	public string m_serverFileName = "";
	[HideInInspector][SyncVar]
	public bool m_owned = false;
	[SyncVar (hook = "playSoundHook")]
	protected bool objectHit = false;
	[SyncVar (hook = "setLoopingHook")]
	public bool m_looping = false;
	[SyncVar (hook = "setDrawingHighlight")]
	protected bool m_drawingSequence = false;

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

	protected virtual void Awake() {
		m_wordSource = GetComponent<AudioSource> ();
		m_sequencer = GetComponent<SoundObjectSequencer> ();
		m_highlight = GetComponent<Highlighter> ();
		m_highlight.ConstantParams (HighlightColour);
	}

	protected enum OpState : byte { Op_Default, Op_PressAndHoldCandidate, Op_Recording, Op_AdjustDistance, Op_Moving, Op_Disabled };
	private OpState m_opstate_internal = OpState.Op_Default;
	protected OpState m_opstate {
		get {
			return m_opstate_internal;
		}
		set {
			if (value != m_opstate_internal) {
				Debug.Log ("From State: " + m_opstate_internal.ToString () + " To State: " + value.ToString ());
			}
			m_opstate_internal = value;
			if (!hasAuthority && (value == OpState.Op_Moving || value == OpState.Op_AdjustDistance)) {
				IAAPlayer.getAuthority (netId);
			} else if (hasAuthority && (value != OpState.Op_Moving && value != OpState.Op_AdjustDistance)) {
				IAAPlayer.removeAuthority (netId);
			}
		}
	}
	private bool m_adjustingDistance = false;
	void Transition() {
		// TODO: back to default when lose / fail to get authority
		switch (m_opstate) {
		case OpState.Op_Default:
			if (m_adjustingDistance && m_target) {
				m_opstate = OpState.Op_AdjustDistance;
			} else if (IAAController.ClickButtonDown && IAAController.IsRightPress && m_target) {
				m_opstate = OpState.Op_PressAndHoldCandidate;
			} else if (IAAController.ClickButtonDown && IAAController.IsCenterPress && m_target) {
				m_opstate = OpState.Op_Moving;
			}
			break;
		case OpState.Op_PressAndHoldCandidate:
			if (IAAController.IsPressed) {
				m_presstime += Time.deltaTime;
				if (m_presstime > m_holdtime) {
					m_opstate = OpState.Op_Recording;
					m_drawingPlane.SetNormalAndPosition ((Camera.main.transform.position - reticle.transform.position).normalized, reticle.transform.position);
					if (m_looping)
						IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState (netId);
					IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence (netId, true);
					IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
					IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
					m_sequencer.startNewSequence ();
				}
			} else {
				// Not long enough. Change looping state.
				IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState (netId);
				m_presstime = 0;
				m_opstate = OpState.Op_Default;
			}
			break;
		case OpState.Op_Recording:
			if (!IAAController.IsPressed) {
				m_opstate = OpState.Op_Default;
				m_sequencer.endSequence();
				IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence(netId,false);
				if (!m_looping)
					IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState(netId);
			}
			break;
		case OpState.Op_AdjustDistance:
			if (!m_adjustingDistance) {
				m_opstate = OpState.Op_Default;
			}
			break;
		case OpState.Op_Moving:
			if (IAAController.ClickButtonUp) {
				m_opstate = OpState.Op_Default;
			}
			break;
		default:
			break;
		}
	}
	void StateWork() {
		switch (m_opstate) {
		case OpState.Op_Default:
			break;
		case OpState.Op_PressAndHoldCandidate:
			break;
		case OpState.Op_Recording:
			break;
		case OpState.Op_AdjustDistance:
			break;
		case OpState.Op_Moving:
			moveObject();
			break;
		default:
			break;
		}
	}

	protected virtual void Update() {
		if (!isClient)
			return;

		Transition ();
		StateWork ();
		setVolumeFromHeight (transform.position.y);
	}

	private void moveObject() {
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

	private void saveMovingInfo(PointerEventData eventData) {
		m_originalControllerRotation = controller.transform.rotation;
		m_originalLaserRotation = laser.transform.rotation;
		m_originalHitPoint = eventData.pointerCurrentRaycast.worldPosition;
		m_hitPointToController = m_originalHitPoint - controller.transform.position;
		m_originalHitPointLocal = transform.InverseTransformPoint(m_originalHitPoint);

		Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
		m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
		m_distanceFromPointer = intersectionLaser.magnitude;
	}
	
	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	public virtual void OnGvrPointerHover(PointerEventData eventData) {
	}

	public void OnPointerEnter (PointerEventData eventData) {
		m_target = true;
		if (m_opstate != OpState.Op_Disabled) {
			if (!m_looping) {
				Debug.Log ("Setting hit");
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
				// Decided to not to get Authority until you click on a thing.
				// To avoid unnatural behaviour when multiple people are pointing on a same object
				// IAAPlayer.getAuthority (netId);
			}
			if (m_opstate == OpState.Op_Recording) {
				Debug.Log ("Adding time");
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		m_target = false;
		if (m_opstate != OpState.Op_Disabled) {
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
				// IAAPlayer.removeAuthority (netId);
			}
			if (m_opstate == OpState.Op_Recording) {
				m_sequencer.addTime ();
			}
		}
	}

	public virtual void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (GvrController.TouchPos.y > .85f) {
			if (this is WordActs) {
				IAAPlayer.localPlayer.CmdDestroyObject (netId);
			} else {
				IAAPlayer.localPlayer.CmdActivateTimedDestroy (netId);
			}
		}
	}

	public virtual void OnPointerDown (PointerEventData eventData) {
		saveMovingInfo (eventData);
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			
//			IAAPlayer.localPlayer.CmdSetSoundObjectMovingState (netId,true);
//			IAAPlayer.localPlayer.CmdSetSoundObjectPositioned(netId,false);
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

//	public void setMovingState(bool state) {
//		m_moving = state;
//	}

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

public static class IAAController {
	public static bool ClickButtonUp {
		get {
			return GvrController.ClickButtonUp;
		}
	}
	public static bool ClickButtonDown {
		get {
			return GvrController.ClickButtonDown;
		}
	}
	public static bool IsPressed {
		get {
			return GvrController.ClickButton;
		}
	}
	public static bool TouchUp {
		get {
			return GvrController.TouchUp;
		}
	}
	public static bool TouchDown {
		get {
			return GvrController.TouchDown;
		}
	}
	public static bool IsTouching {
		get {
			return GvrController.IsTouching;
		}
	}
	public static Vector2 Position {
		get {
			return GvrController.TouchPos;
		}
	}
	public static bool IsCenterPress {
		get {
			return ((Position - Vector2.one / 2f).sqrMagnitude < .09  && IsPressed);
		}
	}
	public static bool IsRightPress {
		get {
			return (Position.x > .85f && IsPressed);
		}
	}
}
