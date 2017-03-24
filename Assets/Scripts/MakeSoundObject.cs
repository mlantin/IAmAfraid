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

	[Command]
	public void CmdSpawnSoundObjectInPlace(string clipfn, Vector3 pos, Quaternion rot) {
		GameObject soundobj  = (GameObject)Instantiate(m_soundObjectPrefab);	
		NonVerbalActs soundscript = soundobj.GetComponent<NonVerbalActs> ();
		soundscript.m_serverFileName = clipfn;

		NetworkServer.Spawn (soundobj);
	}
}
