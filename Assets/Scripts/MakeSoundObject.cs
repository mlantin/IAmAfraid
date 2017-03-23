using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MakeSoundObject : NetworkBehaviour {

	public GameObject m_soundObjectPrefab;

	[Command]
	public void CmdSpawnSoundObject(string clipfn) {
		GameObject soundobj  = (GameObject)Instantiate(m_soundObjectPrefab);		

		NetworkServer.SpawnWithClientAuthority (soundobj, connectionToClient);

		NonVerbalActs soundscript = soundobj.GetComponent<NonVerbalActs> ();
		soundscript.m_serverFileName = clipfn;
	}
}
