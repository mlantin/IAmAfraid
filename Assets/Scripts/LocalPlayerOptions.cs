using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayerOptions : MonoBehaviour {

	static public LocalPlayerOptions singleton;
	static private string[] mocapNames = { "PIXEL1", "PIXEL2", "PIXEL3" };

	public Dropdown mocapSubjects;

	// These are set from the UI
	bool m_observer = false;
	bool m_trackLocalPlayer = false;
	int m_mocapnameIndex;

	void Start() {
		singleton = this;
	}

	public bool observer {
		set {
			m_observer = value;
		}
		get {
			return m_observer;
		}
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
		}
	}

}
