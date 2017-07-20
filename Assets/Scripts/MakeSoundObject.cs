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
		WordInfo tmpWord = new WordInfo ("", clipfn, 1f, pos, rot);
		spawnSoundObjectInPlace (tmpWord);
	}

	public void spawnSoundObjectInPlace(WordInfo sound) {
		// Right now this only gets called when it's a preload so we can assume that it is a preload
		// and set the variable in the gameobject indicating that.
		GameObject soundobj  = (GameObject)Instantiate(m_soundObjectPrefab);
		soundobj.transform.position = sound.pos;
		soundobj.transform.rotation = sound.rot;
		NonVerbalActs soundscript = soundobj.GetComponent<NonVerbalActs> ();
		soundscript.m_serverFileName = sound.clipfn;
		soundscript.m_positioned = true;
		// soundscript.m_preloaded = true;
		if (sound.looping) {
			soundscript.m_looping = true;
			soundscript.m_sequencer.playtriggers = sound.playerTriggers;
			soundscript.m_sequencer.loadedFromScene = true;
			soundscript.m_sequencer.path = sound.path;
		}
		NetworkServer.Spawn (soundobj);
	}
}
