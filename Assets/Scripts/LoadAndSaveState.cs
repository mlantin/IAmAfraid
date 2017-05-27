using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LoadAndSaveState : NetworkBehaviour {

	static public bool Loaded = false;
	public bool loadInitialState = false;
	public string stateFile = "sampleData.json";

	// Use this for initialization
	public override void OnStartServer () {
		if (!Loaded && LocalPlayerOptions.singleton.preload) {
			stateFile = LocalPlayerOptions.singleton.m_preloadFile;
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
				if (word.word != "") {
					wordscript.spawnWord (word.word, word.scale, word.pos, word.rot, word.clipfn, false);
				} else {
					soundscript.spawnSoundObjectInPlace (word.clipfn, word.pos, word.rot);
				}
			}
			Loaded = true;
		}
	}

	[Command]
	public void CmdSaveState() {
		GameObject[] words = GameObject.FindGameObjectsWithTag ("Word");
		GameObject[] sounds = GameObject.FindGameObjectsWithTag ("Sound");

		wordActs wordscript;
		List<WordInfo> stateList = new List<WordInfo>();
		foreach (GameObject obj in words) {
			wordscript = obj.GetComponent<wordActs> ();
			stateList.Add(new WordInfo(wordscript.m_wordstr, wordscript.m_serverFileName, wordscript.m_scale,
				obj.transform.position, obj.transform.rotation));
		}
		NonVerbalActs soundscript;
		foreach (GameObject obj in sounds) {
			soundscript = obj.GetComponent<NonVerbalActs> ();
			stateList.Add (new WordInfo ("", soundscript.m_serverFileName, 1.0f,
				obj.transform.position, obj.transform.rotation));
		}
		string filename = Application.persistentDataPath+"/"+Webserver.GenerateFileName ("state")+".json";
		Debug.Log ("Saving state to " + filename);
		WordInfo.writeFileWithNewWordInfoList (filename, stateList);
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	void Update() {
		if (GvrController.AppButtonUp) {
			CmdSaveState ();
		}
	}
	#endif
}
