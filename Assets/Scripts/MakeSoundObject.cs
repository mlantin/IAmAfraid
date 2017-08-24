using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MakeSoundObject : NetworkBehaviour {

	public GameObject nonVerbalPrefab;
	public GameObject wordPrefab;

	/// <summary>
	/// Cmds for spawning.
	/// </summary>
	/// <param name="clipfn">Full relative path for clip file.</param>
	/// <param name="isNewObject">Will have authority if it is newly generated.</param>
	[Command]
	public void CmdSpawnSoundObject(string word, float scale, Vector3 pos, Quaternion rot, string clipfn, bool isNewObject) {
		WordInfo tmpSound = new WordInfo (word, clipfn, scale, pos, rot);
		spawnSoundObject (tmpSound, isNewObject);
	}
	/// <summary>
	/// Spawn word and non verbal object
	/// </summary>
	/// <param name="sound">Wordinfo. Spawn as non verbal if sound.word equals ""</param>
	/// <param name="isNewObject">Will have authority if it is newly generated.</param>
	public void spawnSoundObject(WordInfo sound, bool isNewObject) {
		GameObject soundobj;
		if (string.IsNullOrEmpty(sound.word)) {
			soundobj = (GameObject)Instantiate (nonVerbalPrefab);
		} else {
			soundobj = (GameObject)Instantiate (wordPrefab);
		}
		soundobj.transform.position = sound.pos;
		soundobj.transform.rotation = sound.rot;
		SoundObjectActs soundscript = soundobj.GetComponent<SoundObjectActs> ();
		if (soundscript is WordActs) {
			((WordActs)soundscript).m_wordstr = sound.word;
			((WordActs)soundscript).m_scale = sound.scale;
		}
		soundscript.m_serverFileName = sound.clipfn;
		if (sound.looping) {
			soundscript.m_looping = true;
			soundscript.m_sequencer.playtriggers = sound.playerTriggers;
			soundscript.m_sequencer.loadedFromScene = true;
			if (sound.scrubs != null)
				soundscript.m_sequencer.scrubs = sound.scrubs;
			soundscript.m_sequencer.path = sound.path;
		}
		if (isNewObject) {
			soundscript.m_newSpawn = true;
			soundscript.m_creator = netId.Value;
			Debug.Log(connectionToClient.connectionId);
			NetworkServer.SpawnWithClientAuthority (soundobj, connectionToClient);
		} else {
			NetworkServer.Spawn (soundobj);
		}
	}
}
