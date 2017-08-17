using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
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
	protected enum OpState : byte { Op_Default, Op_PressAndHoldCandidate, Op_Recording, 
		Op_AdjustDistance, Op_Moving, Op_NewSpawn, Op_Disabled };
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
	private int MaxFrameWaitingForAuthority = 16;
	private int m_controlWithNoAuth = 0;

	protected GameObject m_laser = null;
	protected GameObject m_reticle = null;
	protected GameObject m_controller = null;
	protected GameObject m_tmpSphere = null;
	protected Plane m_drawingPlane = new Plane ();
	protected Highlighter m_highlight;

	// protected float m_distanceFromPointer = 1.0f;
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
	/// <summary>
	/// An object is owned when someone is recording on it.
	/// </summary>
	[HideInInspector][SyncVar]
	public uint m_recorder = 0;
	/// <summary>
	/// The player who is controlling the sound/granular offset of the object is its sound owner.
	/// </summary>
	[HideInInspector][SyncVar]
	public uint m_soundOwner = 0;
	protected List<uint> m_soundOwnerQ = new List<uint>();
	[SyncVar (hook = "playSoundHook")]
	protected bool objectHit = false;
	[SyncVar (hook = "setLooping")]
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
			m_opstate = OpState.Op_NewSpawn;
		}
	}

	protected virtual void Start() {
		if (m_looping) {
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

	protected OpState m_opstate {
		get {
			return m_opstate_internal;
		}
		set {
			if (value != m_opstate_internal) {
				m_adjustDistanceCandidate = false;
				Debug.Log ("From State: " + m_opstate_internal.ToString () + " To State: " + value.ToString ());
			}
			if (!hasAuthority && (value == OpState.Op_Moving || value == OpState.Op_AdjustDistance || value == OpState.Op_NewSpawn)) {
				if (m_recorder != 0) // We don't want to mess things up when others are recording
					return;
				if (!IAAPlayer.localPlayer.getUserAuth (netId)) {
					return;
				}
				m_controlWithNoAuth = 0;
				IAAPlayer.getAuthority (netId);
			} else if (value == OpState.Op_Default || (hasAuthority && (value != OpState.Op_Moving && value != OpState.Op_AdjustDistance && value != OpState.Op_NewSpawn))) {
				IAAPlayer.localPlayer.removeUserAuth (netId);
				IAAPlayer.removeAuthority (netId);
			}
			m_opstate_internal = value;
		}
	}

	Vector2 delta = new Vector2(0, 0);
	void Transition() {
		// TODO: back to default when lose / fail to get recorder registered
		if (m_adjustDistanceCandidate || m_opstate == OpState.Op_AdjustDistance || m_opstate == OpState.Op_NewSpawn) {
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
			if (!m_target) {
				m_presstime = 0;
				m_opstate = OpState.Op_Default;
			} else {
				if (IAAController.IsPressed) {
					m_presstime += Time.deltaTime;
					if (m_presstime > m_holdtime && m_recorder == 0 && m_target) {
						m_presstime = 0;
						IAAPlayer.localPlayer.CmdSetSoundObjectRecorder (netId, true);
						m_opstate = OpState.Op_Recording;
						m_drawingPlane.SetNormalAndPosition ((Camera.main.transform.position - reticle.transform.position).normalized, reticle.transform.position);
						if (m_looping)
							IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState (netId);
						IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence (netId, true);
						m_sequencer.startNewSequence ();
					}
				} else {
					// Not long enough. Change looping state.
					IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState (netId);
					m_presstime = 0;
					m_opstate = OpState.Op_Default;
				}
			}
			break;
		case OpState.Op_Recording:
			if (!IAAController.IsPressed) {
				IAAPlayer.localPlayer.CmdSetSoundObjectRecorder (netId, false);
				m_opstate = OpState.Op_Default;
				m_sequencer.endSequence();
				IAAPlayer.localPlayer.CmdSetSoundObjectDrawingSequence(netId, false);
				if (!m_looping)
					IAAPlayer.localPlayer.CmdToggleSoundObjectLoopingState(netId);
			}
			break;
		case OpState.Op_AdjustDistance:
			if (IAAController.IsPressed) {
				if (m_target && IAAController.IsCenterPress) {
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
		case OpState.Op_NewSpawn:
			if (IAAController.ClickButtonDown) {
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
		case OpState.Op_NewSpawn:
			moveObject();
			adjustDistance (delta.y);
			break;
		default:
			break;
		}
	}

	private void checkAuthority() {
		if (!hasAuthority) {
			m_controlWithNoAuth++;
			Debug.Log ("Waiting for auth: " + m_controlWithNoAuth.ToString() + " Frames");
			if (m_controlWithNoAuth > MaxFrameWaitingForAuthority) {
				m_opstate = OpState.Op_Default;
				m_controlWithNoAuth = 0;
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
		Vector3 newdir = m_rotq * laser.transform.forward;
		float distance = (gameObject.transform.position - laser.transform.position).magnitude;
		Vector3 newpos = laser.transform.position + newdir * distance;
		if (this is WordActs && m_opstate != OpState.Op_NewSpawn) {
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

		if (this is WordActs && m_opstate != OpState.Op_NewSpawn) {
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
		// m_distanceFromPointer = intersectionLaser.magnitude;
	}

	public void updateSoundOwner() {
		Debug.Log ("Updating owner!");
		if (m_recorder != 0) {
			m_soundOwner = m_recorder;
			setHit (m_soundOwnerQ.IndexOf(m_recorder) != -1);
		} else {
			if (m_soundOwnerQ.Count > 0) {
				m_soundOwner = m_soundOwnerQ [0];
				setHit (true);
			} else {
				m_soundOwner = 0;
				setHit (false);
			}
		}
	}

	public void setHit(bool state) {
		objectHit = state;
	}

	public void soundOwnerIn(NetworkInstanceId playerId) {
		m_soundOwnerQ.Add (playerId.Value);
		updateSoundOwner ();
	}

	public void soundOwnerOut(NetworkInstanceId playerId) {
		bool result = m_soundOwnerQ.Remove (playerId.Value);
		if (!result) {
			Debug.LogError ("Deregister sound owner failed");
		}
		updateSoundOwner ();
	}
	
	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	public virtual void OnGvrPointerHover(PointerEventData eventData) {
	}

	public void OnPointerEnter (PointerEventData eventData) {
		m_target = true;
		IAAPlayer.localPlayer.CmdLineUpForOwner (netId);
		if (m_opstate == OpState.Op_Recording) {
			m_sequencer.addTime ();
		}
	}

	public void OnPointerExit(PointerEventData eventData){
		m_target = false;
		IAAPlayer.localPlayer.CmdQuitLineForOwner (netId);
		if (m_opstate == OpState.Op_Recording) {
			m_sequencer.addTime ();
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

	public void setRecorder(NetworkInstanceId playerId, bool state) {
		if (m_recorder == 0 && state) {
			m_recorder = playerId.Value;
		} else if (!state && m_recorder == playerId.Value) {
			m_recorder = 0;
		}
		updateSoundOwner ();
	}

	public void setDrawingSequence(bool val) {
		m_drawingSequence = val;
	}

	// Proxy Functions (END)

	// SyncVar does not call overrided function
	public void playSoundHook(bool state) {
		if (m_looping)
			return;
		playSound (state);
	}

	public void setLooping(bool val) {
		m_looping = val;
		if (IAAPlayer.localPlayer == null) {
			return;
		}
		if (m_looping) {
			m_highlight.ConstantOnImmediate (HighlightColour);
			IAAPlayer.localPlayer.CmdSoundObjectStartSequencer(netId);
		} else {
			m_highlight.ConstantOffImmediate ();
			IAAPlayer.localPlayer.CmdSoundObjectStopSequencer(netId);
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

	protected void setVolumeFromHeight(float y) {
		float vol = Mathf.Clamp(-50+y/1.8f*56f, -50f,6f);
		Debug.Log ("y = " + y + " Vol = " + vol);
		//		m_wordSource.gainDb = vol;
		m_wordSource.volume = y/1.8f;
	}

	public virtual void playSound(bool state) {
		Debug.Log ("This should not be called");
	}
}