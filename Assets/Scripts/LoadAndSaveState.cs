using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LoadAndSaveState : NetworkBehaviour {

	public bool loadInitialState = false;
	public string stateFile = "sampleData.json";

	// Use this for initialization
	public override void OnStartLocalPlayer () {
		if (isServer && loadInitialState) {
			string jsonText = "";
			#if !UNITY_ANDROID || UNITY_EDITOR
			try
			{
				if (!Directory.Exists(Application.streamingAssetsPath))
					Directory.CreateDirectory(Application.streamingAssetsPath);
				string fullFilePath = Application.streamingAssetsPath + "/" + stateFile;
				jsonText = System.IO.File.ReadAllText(fullFilePath);
			}
			catch (System.IO.FileNotFoundException)
			{
				// mark as loaded anyway, so we don't keep retrying..
				Debug.Log("Failed to load state file.");
			}
			#else
			WWW request = new WWW(Application.streamingAssetsPath + "/" + stateFile);
			while (!request.isDone);
			jsonText = request.text;
			#endif
			List<WordInfo> wordlist = WordInfo.newWordInfoListFromString(jsonText);

			makeaword wordscript = GetComponent<makeaword> ();
			MakeSoundObject soundscript = GetComponent<MakeSoundObject> ();
			foreach(WordInfo word in wordlist) {
				if (word.word != "")
					wordscript.CmdSpawnWord (word.word, word.scale, word.pos, word.rot, word.clipfn, false);
				else
					soundscript.CmdSpawnSoundObjectInPlace (word.clipfn, word.pos, word.rot);
			}
		}
	}
}
