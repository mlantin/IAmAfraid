using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	bool m_trackLocalPlayer = false;
	int m_mocapnameIndex;

	void Start() {
		singleton = this;

		List<Dropdown.OptionData> menuOptions = preloadFiles.GetComponent<Dropdown> ().options;
		m_preloadFile = menuOptions [0].text+".json";
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
		List<Dropdown.OptionData> menuOptions = preloadFiles.GetComponent<Dropdown> ().options;

		//get the string value of the selected index
		m_preloadFile = menuOptions [val].text+".json";
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

}
