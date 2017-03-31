using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

// this is connected to the player object
public class makeaword : NetworkBehaviour {
	public GameObject wordPrefab;

	[Command]
	public void CmdSpawnWord(string word, float scale, Vector3 pos, Quaternion rot, string clipfn, bool owned) {
		Debug.Log ("Spawning");

		GameObject newwordTrans  = (GameObject)Instantiate(wordPrefab);		
		newwordTrans.transform.position = pos;
		newwordTrans.transform.rotation = rot;

		wordActs wordscript = newwordTrans.GetComponent<wordActs> ();
		wordscript.m_scale = scale;
		wordscript.m_wordstr = word;
		wordscript.m_serverFileName = clipfn;
		if (!owned) {
			// Right now it only gets here when it's a preload so we can assume that it is a preload
			// and set the variable in the gameobject indicating that.
			wordscript.m_positioned = true;
			wordscript.m_preloaded = true;
		}

		if (owned)
			NetworkServer.SpawnWithClientAuthority (newwordTrans, connectionToClient);
		else
			NetworkServer.Spawn (newwordTrans);
	}
		
}
