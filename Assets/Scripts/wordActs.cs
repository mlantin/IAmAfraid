using System.Collections;
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

	private float m_distanceFromPointer = 1.0f;
	private GvrAudioSource m_wordSource;

	private Quaternion m_rotq;
	private bool m_moving = false;
	GameObject m_laser = null;

	GameObject laser {
		get {
			if (m_laser == null) 
				m_laser = LocalPlayer.playerObject.transform.FindChild ("GvrControllerPointer/Laser").gameObject;
			return m_laser;
		}
	}

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
		if (isClient && ((!m_positioned && hasAuthority) || (m_moving))) {
			if (!m_moving) {
				transform.position = laser.transform.position + laser.transform.forward;
			} else {
				// We have picked a word and we're moving it...
				Vector3 newdir = m_rotq*laser.transform.forward;
				transform.position = laser.transform.position+newdir*m_distanceFromPointer;
			}
			transform.rotation = laser.transform.rotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
				m_moving = false;
				LocalPlayer.singleton.CmdSetWordPositioned(netId, true);
			}
		}
		#endif
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
		Debug.Log ("x: " + (reticleLocal.x / bbdim.x + 0.5f) + " y: " + (reticleLocal.y / bbdim.y+.5f));
	}

	public void OnPointerEnter (PointerEventData eventData) {
		if (m_positioned)
			LocalPlayer.singleton.CmdSetWordHitState (netId,true);
	}

	public void OnPointerExit(PointerEventData eventData){
		if (m_positioned)
			LocalPlayer.singleton.CmdSetWordHitState (netId, false);
	}

	public void OnPointerClick (PointerEventData eventData) {
		//get the coordinates of the trackpad so we know what kind of event we want to trigger
		if (m_positioned && GvrController.TouchPos.y > .85f) {
			separateLetters ();
			LocalPlayer.singleton.CmdDestroyObject(netId);
		} 
	}

	public void OnPointerDown (PointerEventData eventData) {
		if ((GvrController.TouchPos - Vector2.one / 2f).sqrMagnitude < .09) {
			Vector3 intersectionLaser = gameObject.transform.position - laser.transform.position;
			m_rotq = Quaternion.FromToRotation (laser.transform.forward, intersectionLaser);
			m_distanceFromPointer = intersectionLaser.magnitude;
			m_positioned = false;
			m_moving = true;
			LocalPlayer.singleton.CmdSetWordPositioned(netId,false);
		}
	}
	#endif

	public override void OnNetworkDestroy() {
		Debug.Log ("EXTERMINATE!");
		if (isServer) {
			Debug.Log ("Exterminating");
			if (!m_preloaded)
				Webserver.singleton.DeleteAudioClipNoCheck (m_serverFileName);
		}
	}

	public void setHit(bool state) {
		wordHit = state;
	}

	void playWord(bool hit) {
		wordHit = hit;
		if (hit) {
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

	void separateLetters(){
		// First add a Rigid Body component to the letters
		GameObject letters = gameObject.transform.FindChild("Letters").gameObject;
		BoxCollider collider;
		Rigidbody rb;
		Vector3 rbf = new Vector3 ();
		foreach (Transform letter in letters.transform) {
			rb = letter.gameObject.AddComponent<Rigidbody> ();
			letter.gameObject.AddComponent<BoxCollider> ();
			rbf.x = Random.Range (-.1f, .1f);
			rbf.y = Random.Range (-1, 0);
			rbf.z = Random.Range (-.1f, .1f);
			rb.AddForce (rbf, ForceMode.VelocityChange);
			letter.parent = letter.parent.parent.parent;
		}
	}
}
