using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

// this is connected to the player object
public class makeaword : NetworkBehaviour {
	public GameObject wordPrefab;

	[Command]
	public void CmdSpawnWord(string word, float scale, Vector3 pos, Quaternion rot, string clipfn, bool owned) {
		spawnWord (word, scale, pos, rot, clipfn, false, null, null, owned);
	}

	public void spawnWord(string word, float scale, Vector3 pos, Quaternion rot, string clipfn, bool looping, List<int> playTrigger, List<Vector3> path, bool owned) {
		Debug.Log ("Spawning");

		GameObject newwordTrans  = (GameObject)Instantiate(wordPrefab);		
		newwordTrans.transform.position = pos;
		newwordTrans.transform.rotation = rot;

		WordActs wordscript = newwordTrans.GetComponent<WordActs> ();
		wordscript.m_scale = scale;
		wordscript.m_wordstr = word;
		wordscript.m_serverFileName = clipfn;
		//wordscript.m_looping = looping;
		//wordscript.tmpPath = path;
		//wordscript.tmpPlayerTrigger = playTrigger;
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
