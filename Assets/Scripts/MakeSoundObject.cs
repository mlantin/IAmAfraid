using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MakeSoundObject : NetworkBehaviour {

	public GameObject m_soundObjectPrefab;

	[Command]
	public void CmdSpawnSoundObject(string clipfn) {
		GameObject soundobj  = (GameObject)Instantiate(m_soundObjectPrefab);	
		NonVerbalActs soundscript = soundobj.GetComponent<NonVerbalActs> ();
		soundscript.m_serverFileName = clipfn;
		NetworkServer.SpawnWithClientAuthority (soundobj, connectionToClient);

	}

	public void spawnSoundObjectInPlace(string clipfn, Vector3 pos, Quaternion rot, bool looping, List<int> playerTrigger, List<Vector3> path) {
		// Right now this only gets called when it's a preload so we can assume that it is a preload
		// and set the variable in the gameobject indicating that.
		GameObject soundobj  = (GameObject)Instantiate(m_soundObjectPrefab);
		soundobj.transform.position = pos;
		soundobj.transform.rotation = rot;
		NonVerbalActs soundscript = soundobj.GetComponent<NonVerbalActs> ();
		soundscript.m_serverFileName = clipfn;
		soundscript.m_positioned = true;
		//soundscript.m_looping = looping;
		//soundscript.tmpPath = path;
		//soundscript.tmpPlayerTrigger = playerTrigger;
		// soundscript.m_preloaded = true;

		NetworkServer.Spawn (soundobj);
	}
}
