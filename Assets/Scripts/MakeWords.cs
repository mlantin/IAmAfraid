using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MakeWords : NetworkBehaviour {

	private makeaword m_wordCreator;
	private MakeSoundObject m_soundCreator;

	void Awake() {
		m_wordCreator = GetComponent<makeaword> ();
		m_soundCreator = GetComponent<MakeSoundObject> ();
		Debug.Log (m_wordCreator);
	}

	public void spawn(WordInfo word, bool own) {
		Debug.Log (word);
		if (word.word != "") {
			m_wordCreator.spawnWord (word.word, word.scale, word.pos, word.rot, word.clipfn, false);
		} else {
			m_soundCreator.spawnSoundObjectInPlace (word.clipfn, word.pos, word.rot);
		}
	}
}
