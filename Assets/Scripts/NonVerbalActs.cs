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

	[SyncVar (hook="fetchAudio")]
	public string m_serverFileName = "";
	public Text m_DebugText;

	private static Vector3 m_relpos = new Vector3(0.0f,1.6f,0.0f);
	private bool m_positioned = false;
	private float m_distanceFromPointer = 1.0f;
	private Vector3 m_pointerDir;
	private GvrAudioSource m_wordSource;

	[SyncVar (hook ="playSound")]
	bool objectHit = false;

	// Use this for initialization
	void Awake () {
		m_wordSource = GetComponent<GvrAudioSource> ();
	}

	void Update () {
		if (isServer)
			return;
		#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
		if (!m_positioned) {
			transform.position = GvrController.ArmModel.pointerRotation * Vector3.forward
				+GvrController.ArmModel.pointerPosition + m_relpos;
			transform.rotation = GvrController.ArmModel.pointerRotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
			}
		}
		#endif
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
		CmdSetObjectHit(true);
	}

	public void OnPointerExit(PointerEventData eventData){
		CmdSetObjectHit(false);
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			Destroy (this.gameObject);
		} 
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			m_distanceFromPointer = eventData.pointerCurrentRaycast.distance;
			//m_DebugText.text = m_distanceFromPointer.ToString () + " " + eventData.pointerCurrentRaycast.distance.ToString ();
			m_positioned = false;
		}
	}
	#endif

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
		Debug.Log ("The RADIUS is: "+col.radius);
	}
}
	
