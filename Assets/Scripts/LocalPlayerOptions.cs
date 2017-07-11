using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LocalPlayerOptions : MonoBehaviour {

	static public LocalPlayerOptions singleton;
	static private string[] mocapNames = { "PIXEL1", "PIXEL2", "PIXEL3" };

	public Dropdown mocapSubjects;
	public Toggle godToggle;
	public Dropdown preloadFiles;
	public GameObject holojam;

	// These are set from the UI
	bool m_observer = false;
	bool m_god = false;
	bool m_preload = true;
	[HideInInspector]
	public string m_preloadFile;
	[HideInInspector]
	public SceneFile m_preloadScene;
	private List<SceneFile> m_sceneFiles;
	private int m_sceneNum = 0;

	bool m_trackLocalPlayer = false;
	int m_mocapnameIndex;

	public class SceneFile {

		public string sceneName;
		public bool isOnServer;
		public string title;

		public SceneFile (string _sceneName, string _title, bool _onServer) {
			sceneName = _sceneName;
			isOnServer = _onServer;
			title = _title;
		}

	}

	public SceneFile PreloadFile {

		get { 
			return m_sceneFiles [m_sceneNum]; 
		}

	}

	void Start() {
		singleton = this;

		// First populate from PlayerPrefs if they exist
		populateUI();
		m_sceneFiles = new List<SceneFile>();
		m_sceneFiles.Add(new SceneFile("NewScene", "New Scene", false));
		List<string> t = new List<string> ();
		t.Add ("New Scene");
		preloadFiles.GetComponent<Dropdown> ().AddOptions (t);
		Webserver.singleton.getSceneList ();
		/*
		List<Dropdown.OptionData> menuOptions = preloadFiles.GetComponent<Dropdown> ().options;

		menuOptions.ForEach (x => {
			m_sceneFiles.Add(new SceneFile(x.text, x.text, false));
		});
		*/

	}

	public void AddServerScene(string title, string sceneName) {
		RemoveServerScene ();
		m_sceneFiles.Add(new SceneFile(sceneName, title, true));
		List<string> t = new List<string> ();
		t.Add ("+" + title);
		preloadFiles.GetComponent<Dropdown> ().AddOptions (t);
	}

	public void RemoveServerScene() {
		// TODO.
	}

	void populateUI() {
		if (PlayerPrefs.HasKey ("ServerIP")) {
			InputField serverip = transform.Find ("Panel/ServerIP").gameObject.GetComponent<InputField> ();
			string ip = PlayerPrefs.GetString ("ServerIP");
			serverip.text = ip;
			NetworkManager.singleton.networkAddress = ip;
		}
		if (PlayerPrefs.HasKey ("SoundServerIP")) {
			InputField soundserverip = transform.Find ("Panel/Sound Server IP").gameObject.GetComponent<InputField> ();
			string ip = PlayerPrefs.GetString ("SoundServerIP");
			soundserverip.text = ip;
			// The ip is set in Webserver script when it starts.
		}
	}

	public bool observer {
		set {
			m_observer = value;
			if (m_observer) {
				godToggle.interactable = true;
			} else {
				godToggle.interactable = false;	
			}
		}
		get {
			return m_observer;
		}
	}

	public bool god {
		set {
			m_god = value;
		}
		get {
			return m_god;
		}
	}

	public bool preload {
		set {
			m_preload = value;
			if (m_preload)
				preloadFiles.interactable = true;
			else
				preloadFiles.interactable = false;
		}
		get {
			return m_preload;
		}
	}

	public void setPreloadFile (int val) {
		m_sceneNum = val;
	}

	public int mocapNameIndex {
		get {
			return m_mocapnameIndex;
		}
		set {
			m_mocapnameIndex = value;
		}
	}

	public string mocapName {
		get {
			return mocapNames [m_mocapnameIndex];
		}
	}

	public bool trackLocalPlayer {
		get {
			return m_trackLocalPlayer;
		}
		set {
			m_trackLocalPlayer = value;
			mocapSubjects.interactable = m_trackLocalPlayer;
			holojam.SetActive (value);
		}
	}

	void OnApplicationQuit() {
		PlayerPrefs.Save ();
	}

}
