using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

// this is connected to the player object
public class makeaword : NetworkBehaviour {
	public GameObject wordPrefab;

	[Command]
	public void CmdSpawnWord(string word, float scale, Vector3 pos, Quaternion rot, string clipfn, bool owned) {
		WordInfo tmpWord = new WordInfo (word, clipfn, scale, pos, rot);
		spawnWord (tmpWord, owned);
	}

	public void spawnWord(WordInfo word, bool owned) {
		Debug.Log ("Spawning");

		GameObject newwordTrans  = (GameObject)Instantiate(wordPrefab);		
		newwordTrans.transform.position = word.pos;
		newwordTrans.transform.rotation = word.rot;

		WordActs wordscript = newwordTrans.GetComponent<WordActs> ();
		wordscript.m_scale = word.scale;
		wordscript.m_wordstr = word.word;
		wordscript.m_serverFileName = word.clipfn;
		wordscript.m_looping = word.looping;
		wordscript.m_sequencer.playtriggers = word.playerTriggers;
		wordscript.m_sequencer.path = word.path;
		wordscript.m_sequencer.scrubs = word.scrubs;
		wordscript.m_sequencer.loadedFromScene = true;
		if (!owned) {
			// Right now it only gets here when it's a preload so we can assume that it is a preload
			// and set the variable in the gameobject indicating that.
			wordscript.m_positioned = true;
			// wordscript.m_preloaded = true;
		}

		if (owned)
			NetworkServer.SpawnWithClientAuthority (newwordTrans, connectionToClient);
		else
			NetworkServer.Spawn (newwordTrans);
	}
}
