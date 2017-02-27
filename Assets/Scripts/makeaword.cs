using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class makeaword : MonoBehaviour {

	public GameObject alphabet;
	public Text m_debugText;

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
		m_xspace = extent.x;
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void makeword(string word, float scale, Vector3 pos, Quaternion rot, AudioClip clip) {
		GameObject newword = new GameObject (word);
		wordActs wordscript = newword.AddComponent<wordActs> ();
		newword.transform.parent = transform;

		GameObject newletter;
		MeshFilter lettermesh;
		Vector3 letterpos = new Vector3 ();
		Vector3 scalevec = new Vector3 (scale, scale, scale);
		Vector3 letterscale = new Vector3 (1f, 1f, 1f);

		newword.transform.localScale = scalevec;
		newword.transform.position = pos;
		newword.transform.rotation = rot;

		AudioSource wordsource = newword.AddComponent<AudioSource>();
		wordsource.clip = clip;
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
					letterpos.x += extent.x*2;
					break;
				}
			}
		}
		Rigidbody rb = newword.AddComponent<Rigidbody> ();
		rb.useGravity = false;
		rb.constraints = RigidbodyConstraints.FreezeAll;

		BoxCollider bc = newword.AddComponent<BoxCollider> ();
		bc.size = boxsize;
		bc.center = boxsize / 2.0f;

		wordscript.bbdim = boxsize;
		wordscript.m_debugText = m_debugText;

		wordsource.Play ();
	}
}
