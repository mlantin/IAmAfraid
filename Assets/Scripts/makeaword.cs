using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

// this is connected to the player object
public class makeaword : NetworkBehaviour {

	public GameObject alphabet;
	public GameObject wordPrefab;

	private float m_xspace = 0;

	// Use this for initialization
	void Start () {
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
	
	// Update is called once per frame
	void Update () {
	}

	public void makeword(string word, float scale, Vector3 pos, Quaternion rot, AudioClip clip, string clipfn) {
		CmdSpawnWord (word, scale, pos, rot, clipfn);
		// TODO: not sure if we can find out when the word has been created and assign the AudioClip
		// we already have on the client. If not from this function, then there is no point having it
		// as an intermediary. Could just call CmdSpawnWord directly and have a way of finding out which
		// client asked for it, and then assign the last audioclip recorded. It's a bit kludgy and relies
		// on no records taking place in between. Also we have an issue because it needs to be positioned
		// in the world. So the WordActs script needs to take that into account. Only pay attention to the 
		// m_positioned variable if you own the word.

		//GvrAudioSource wordsource = newwordTrans.GetComponent<GvrAudioSource>();
		//wordsource.clip = clip;
		//wordsource.loop = false;
		//wordsource.Play ();
	}

	[Command]
	void CmdSpawnWord(string word, float scale, Vector3 pos, Quaternion rot, string clipfn) {
		// Create the Bullet from the Bullet Prefab
		GameObject newwordTrans  = (GameObject)Instantiate(wordPrefab);		
		newwordTrans.transform.position = pos;
		newwordTrans.transform.rotation = rot;

		GameObject newword = newwordTrans.transform.GetChild (0).gameObject;

		GameObject newletter;
		MeshFilter lettermesh;
		Vector3 letterpos = new Vector3 ();
		Vector3 scalevec = new Vector3 (scale, scale, scale);
		Vector3 letterscale = new Vector3 (1f, 1f, 1f);

		newword.transform.localScale = scalevec;

		GvrAudioSource wordsource = newwordTrans.GetComponent<GvrAudioSource>();
		//wordsource.clip = clip;
		wordsource.loop = false;


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

		BoxCollider bc = newwordTrans.GetComponent<BoxCollider> ();
		bc.size = boxsize;

		wordActs wordscript = newwordTrans.GetComponent<wordActs> ();
		wordscript.bbdim = boxsize;
		wordscript.serverFileName = clipfn;

		//wordsource.Play ();

		NetworkServer.SpawnWithClientAuthority (newwordTrans, connectionToClient);
	}
		
}
