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
	protected enum OpState : byte { Op_Default, Op_PressAndHoldCandidate, Op_Recording, Op_AdjustDistance, Op_Moving, Op_Disabled };
	private OpState m_opstate_internal = OpState.Op_Default;
	/// <summary>
	/// Touch position in last frame.
	/// </summary>
	private Vector2 m_adjustDistanceTouchPos = new Vector2 (0f, 0f);
	/// <summary>
	/// Delta of total movement in Y-axis.
	/// </summary>
	private float m_adjustDistanceCount = 0;
	/// <summary>
	/// The time after user lift finger in adjusting distance mode.
	/// </summary>
	private float m_adjustLiftTime = 0;
	private bool m_adjustDistanceCandidate = false;
	/// <summary>
	/// Adjusting distance disabled when user moved more than this value in X-axis.
	/// </summary>
	private float MinDeltaXToDisable = 0.1f;
	/// <summary>
	/// Minimum movement to enable adjusting distance mode.
	/// </summary>
	private float MinDeltaYThatCount = 0.03f;
	/// <summary>
	/// Min distance between object and controller.
	/// </summary>
	[Tooltip("Min distance between object and controller")]
	public float AdjustMinDistance = 0.3f;
	/// <summary>
	/// Max distance between object and controller.
	/// </summary>
	[Tooltip("Max distance between object and controller")]
	public float AdjustMaxDistance = 4f;
	/// <summary>
	/// The max time between lifting without losing distance control of the object.
	/// </summary>
	private float MaxTimeBetweenLifting = 0.4f;
	private int MaxFrameWaitingForAuthority = 25;
	private int m_controlWithNoAuth = 0;

	protected GameObject m_laser = null;
	protected GameObject m_reticle = null;
	protected GameObject m_controller = null;
	protected GameObject m_tmpSphere = null;
	protected Plane m_drawingPlane = new Plane ();
	protected Highlighter m_highlight;

	protected float m_distanceFromPointer = 1.0f;
	protected Quaternion m_rotq;
	protected Vector3 m_originalHitPoint;
	protected Vector3 m_originalHitPointLocal, m_hitPointToController;
	protected Quaternion m_originalControllerRotation, m_originalLaserRotation;

	[HideInInspector]
	public SoundObjectSequencer m_sequencer;
	public bool m_newSpawn = false;
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

	public override void OnStartClient() {
		base.OnStartClient ();
		if (m_newSpawn) {
			saveMovingInfo (null);
			m_opstate = OpState.Op_Moving;
		}
	}

	protected OpState m_opstate {
		get {
			return m_opstate_internal;
		}
		set {
			if (value != m_opstate_internal) {
				m_adjustDistanceCandidate = false;
				Debug.Log ("From State: " + m_opstate_internal.ToString () + " To State: " + value.ToString ());
			}
			if (!hasAuthority && (value == OpState.Op_Moving || value == OpState.Op_AdjustDistance)) {
				if (!IAAPlayer.localPlayer.getUserAuth())
					return;
				m_controlWithNoAuth = 0;
				IAAPlayer.getAuthority (netId);
			} else if (hasAuthority && (value != OpState.Op_Moving && value != OpState.Op_AdjustDistance)) {
				IAAPlayer.localPlayer.removeUserAuth ();
				IAAPlayer.removeAuthority (netId);
			}
			m_opstate_internal = value;
		}
	}

	Vector2 delta = new Vector2(0, 0);
	void Transition() {
		// TODO: back to default when lose / fail to get authority
		if (m_adjustDistanceCandidate || m_opstate == OpState.Op_AdjustDistance) {
			delta = IAAController.Position - m_adjustDistanceTouchPos;
			m_adjustDistanceTouchPos = IAAController.Position;
			if (IAAController.TouchDown) {
				delta = new Vector2(0, 0);
			}
		}
		if (m_adjustDistanceCandidate) {
			// Debug.Log (m_adjustDistanceCount);
			if (!IAAController.IsTouching) {
				m_adjustDistanceCandidate = false;
			}
			if (Mathf.Abs(delta.x) > MinDeltaXToDisable) {
				m_adjustDistanceCandidate = false;
				Debug.Log ("Adjust disabled");
			}
			if (delta.y * m_adjustDistanceCount > -1e-9) {
				m_adjustDistanceCount += delta.y;
			} else {
				m_adjustDistanceCount = 0;
			}
			if (Mathf.Abs(m_adjustDistanceCount) > MinDeltaYThatCount) {
				m_opstate = OpState.Op_AdjustDistance;
			}
		}

		switch (m_opstate) {
		case OpState.Op_Default:
			if (m_target && IAAController.ClickButtonDown && IAAController.IsRightPress) {
				m_opstate = OpState.Op_PressAndHoldCandidate;
			} else if (m_target && IAAController.ClickButtonDown && IAAController.IsCenterPress) {
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
			if (IAAController.IsPressed) {
				if (m_target) {
					m_opstate = OpState.Op_Moving;
				} else {
					m_opstate = OpState.Op_Default;
				}
			} else if (!IAAController.IsTouching) {
				if (IAAController.TouchUp) {
					m_adjustLiftTime = 0;
				} else {
					m_adjustLiftTime += Time.deltaTime;
					if (m_adjustLiftTime > MaxTimeBetweenLifting) {
						m_opstate = OpState.Op_Default;
					}
				}
			}
			checkAuthority ();
			break;
		case OpState.Op_Moving:
			if (IAAController.ClickButtonUp) {
				m_opstate = OpState.Op_Default;
			}
			checkAuthority ();
			break;
		default:
			break;
		}
		if (m_target && IAAController.TouchDown && m_opstate != OpState.Op_AdjustDistance) {
			m_adjustDistanceCandidate = true;
			m_adjustDistanceTouchPos = IAAController.Position;
			m_adjustDistanceCount = 0;
		}

		switch (m_opstate) {
		case OpState.Op_Default:
			break;
		case OpState.Op_PressAndHoldCandidate:
			break;
		case OpState.Op_Recording:
			break;
		case OpState.Op_AdjustDistance:
			adjustDistance (delta.y);
			break;
		case OpState.Op_Moving:
			moveObject();
			break;
		default:
			break;
		}
	}

	private void checkAuthority() {
		if (!hasAuthority) {
			m_controlWithNoAuth++;
			if (m_controlWithNoAuth > MaxFrameWaitingForAuthority) {
				IAAPlayer.localPlayer.removeUserAuth ();
				IAAPlayer.removeAuthority (netId);
				m_opstate = OpState.Op_Default;
			}
		} else {
			m_controlWithNoAuth = 0;
		}
	}

	bool updated = false;
	protected virtual void Update() {
		if (!isClient)
			return;
		updated = false;
		if (m_opstate == OpState.Op_Moving || m_opstate == OpState.Op_AdjustDistance) {
			Transition ();
			updated = true;
		}
	}

	protected virtual void LateUpdate() {
		if (!isClient)
			return;
		if (!updated)
			Transition ();
		setVolumeFromHeight (transform.position.y);
	}

	private void adjustDistance(float deltaY) {
		deltaY = -deltaY;
		Vector3 deltaFromController = gameObject.transform.position - controller.transform.position;
		float len = deltaFromController.magnitude;
		deltaFromController = deltaFromController.normalized;
		len += len * deltaY * deltaY * Mathf.Sign(deltaY) * 20f;
		if (len > AdjustMaxDistance || len < AdjustMinDistance) {
			return;
		}
		deltaFromController *= len;
		gameObject.transform.position = controller.transform.position + deltaFromController;
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
		Vector3 intersectionLaser;
		m_originalControllerRotation = controller.transform.rotation;
		m_originalLaserRotation = laser.transform.rotation;
		if (eventData == null) {
			m_originalHitPoint = laser.transform.position + laser.transform.forward;
			intersectionLaser = laser.transform.forward;
		} else {
			m_originalHitPoint = eventData.pointerCurrentRaycast.worldPosition;
			intersectionLaser = gameObject.transform.position - laser.transform.position;
		}
		m_hitPointToController = m_originalHitPoint - controller.transform.position;
		m_originalHitPointLocal = transform.InverseTransformPoint(m_originalHitPoint);
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
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, true);
				// Decided to not to get Authority until you click on a thing.
				// To avoid unnatural behaviour when multiple people are pointing on a same object
				// IAAPlayer.getAuthority (netId);
			}
			if (m_opstate == OpState.Op_Recording) {
				m_sequencer.addTime ();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		m_target = false;
		if (m_opstate != OpState.Op_Disabled) {
			if (!m_looping) {
				IAAPlayer.localPlayer.CmdSetSoundObjectHitState (netId, false);
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

	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
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