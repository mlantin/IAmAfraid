using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class NonVerbalActs : NetworkBehaviour
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
, IGvrPointerHoverHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
#endif
{

	[SyncVar]
	public string m_serverFileName = "";
	public Text m_DebugText;

	private static Vector3 m_relpos = new Vector3(0.0f,1.6f,0.0f);
	private float m_distanceFromPointer = 1.0f;
	private Vector3 m_pointerDir;
	private GvrAudioSource m_wordSource;

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
	}

	void Update () {
		if (isServer)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if (isClient && !m_positioned && hasAuthority) {
			transform.position = GvrController.ArmModel.pointerRotation * Vector3.forward
				+GvrController.ArmModel.pointerPosition + m_relpos;
			transform.rotation = GvrController.ArmModel.pointerRotation;
			if (GvrController.ClickButtonUp) {
				StartCoroutine(setPositionedState(true));
			}
		}
		#endif
	}

	void Start() {
		randomizePaperBall ();
		fetchAudio (m_serverFileName);
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
		StartCoroutine (setObjectHitState (true));
	}

	public void OnPointerExit(PointerEventData eventData){
		StartCoroutine (setObjectHitState (false));
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			CmdDestroySoundObject();
		} 
	}

	[Command]
	void CmdDestroySoundObject() {
		if (!m_preloaded)
			StartCoroutine(Webserver.singleton.DeleteAudioClip (m_serverFileName));
		Destroy (gameObject);
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			m_distanceFromPointer = eventData.pointerCurrentRaycast.distance;
			//m_DebugText.text = m_distanceFromPointer.ToString () + " " + eventData.pointerCurrentRaycast.distance.ToString ();
			StartCoroutine(setPositionedState(false));
		}
	}
	#endif

	IEnumerator setObjectHitState(bool state) {
		if (!hasAuthority)
			LocalPlayer.getAuthority (netId);
		yield return new WaitUntil(() => hasAuthority == true);
		CmdSetObjectHit (state);

		// I'm doing this so that the player retains authority while they are on a paper ball
		// But don't pull the rug out if we're currently positioning the word
		if (state == false && m_positioned) {
			yield return null;
			LocalPlayer.removeAuthority (netId);
		}
	}

	[Command]
	void CmdSetObjectHit(bool state) {
		objectHit = state;
	}

	void playSound(bool hit) {
		if (hit)
			m_wordSource.Play();
		Debug.Log ("play the sound");
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
	
