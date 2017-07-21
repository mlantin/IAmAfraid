using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LoadAndSaveState : NetworkBehaviour {

	static public bool Loaded = false;
	static public string sepString = "__";
	public bool loadInitialState = false;
	public LocalPlayerOptions.SceneFile stateFile = new LocalPlayerOptions.SceneFile ("testscene", "Test Scene", false);

	// Use this for initialization
	// public override void OnStartServer () {
	void Start() {
		if (!isServer)
			return;
		if (!Loaded && LocalPlayerOptions.singleton.preload) {
			stateFile = LocalPlayerOptions.singleton.PreloadFile;
			string jsonText = "";

			if (stateFile.isOnServer) {
				Debug.Log ("Scene name: " + stateFile.sceneName + "Title: " + stateFile.title);
				jsonText = Webserver.singleton.getScene (stateFile.sceneName);
				Debug.Log (jsonText);
			} else {
				/*
				string fileLocation = Path.Combine(Path.Combine("scene", stateFile.sceneName), "config.json");

				#if !UNITY_ANDROID || UNITY_EDITOR
				try
				{
					if (!Directory.Exists(Application.streamingAssetsPath))
						Directory.CreateDirectory(Application.streamingAssetsPath);
					string fullFilePath = Path.Combine(Application.streamingAssetsPath, fileLocation);
					jsonText = System.IO.File.ReadAllText(fullFilePath);
				}
				catch (System.IO.FileNotFoundException)
				{
					// mark as loaded anyway, so we don't keep retrying..
					Debug.Log("Failed to load state file.");
				}
				#else
				WWW request = new WWW(Path.Combine(Application.streamingAssetsPath, fileLocation));
				while (!request.isDone);
				jsonText = request.text;
				#endif
				*/
				IAAScene t = new IAAScene (stateFile.title, stateFile.sceneName, null);
			}

			List<WordInfo> wordlist = IAAScene.fromJSON(jsonText).wordInfoList;
			MakeWords creatorObject = GetComponent<MakeWords> ();

			if (wordlist != null) {
				
				wordlist.ForEach (x => {
					creatorObject.spawn (x, false);
				});
			}

			Loaded = true;
		}
	}

	[Command]
	public void CmdSaveState() {
		Debug.Log ("Saving state");
		GameObject[] words = GameObject.FindGameObjectsWithTag ("Word");
		GameObject[] sounds = GameObject.FindGameObjectsWithTag ("Sound");

		WordActs wordscript;
		List<WordInfo> stateList = new List<WordInfo>();
		foreach (GameObject obj in words) {
			wordscript = obj.GetComponent<WordActs> ();
			stateList.Add(new WordInfo(wordscript.m_wordstr, wordscript.m_serverFileName, wordscript.m_scale,
				obj.transform.position, obj.transform.rotation, wordscript.m_looping, wordscript.m_sequencer.playtriggers, wordscript.m_sequencer.path, wordscript.m_sequencer.scrubs));
			wordscript.saved = true;
		}
		NonVerbalActs soundscript;
		foreach (GameObject obj in sounds) {
			soundscript = obj.GetComponent<NonVerbalActs> ();
			stateList.Add (new WordInfo ("", soundscript.m_serverFileName, 1.0f,
				obj.transform.position, obj.transform.rotation, soundscript.m_looping, soundscript.m_sequencer.playtriggers, soundscript.m_sequencer.path, null));
			soundscript.saved = true;
		}
		/*
		string filename = Application.persistentDataPath + "/" + Webserver.GenerateFileName ("state") + ".json";
		Debug.Log ("Saving state to " + filename);
		*/

		IAAScene scene = new IAAScene ();
		scene.wordInfoList = stateList;
		LocalPlayerOptions.SceneFile sceneFile = LocalPlayerOptions.singleton.PreloadFile;
		if (sceneFile != null) {
			string oriTitle = sceneFile.title;
			int pos = sceneFile.title.LastIndexOf (sepString);
			if (pos != -1) {
				oriTitle = sceneFile.title.Substring (0, pos);
			}
			scene.title = Webserver.GenerateSceneName (oriTitle);
			scene.name = Webserver.GenerateSceneName (sceneFile.sceneName);
		} else {
			scene.title = Webserver.GenerateSceneName ("New Scene");
			scene.name = Webserver.GenerateSceneName ("NewScene");
		}
		Webserver.singleton.UploadNewScene (scene);
		
	}

	#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
	void Update() {
		if (GvrController.AppButtonUp) {
			CmdSaveState ();
		}
	}
	#endif
}
