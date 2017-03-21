using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalPlayer : MonoBehaviour {

	static private GameObject m_playerObject = null;
	static private AuthorityManager m_manager = null;

	static public GameObject playerObject {
		get { 
			if (m_playerObject != null) {
				return m_playerObject;
			} else {
				m_playerObject = GameObject.FindGameObjectWithTag ("Player");
				return m_playerObject;
			}
		}
	}

	static public void getAuthority(NetworkInstanceId netInstanceId) {
		if (m_manager == null) {
			m_manager = playerObject.GetComponent<AuthorityManager> ();
		}
		if (m_manager != null) {
			m_manager.CmdAssignObjectAuthority (netInstanceId);
		}
	}

	static public void removeAuthority(NetworkInstanceId netInstanceId) {
		if (m_manager == null) {
			m_manager = playerObject.GetComponent<AuthorityManager> ();
		}
		if (m_manager != null) {
			m_manager.CmdRemoveObjectAuthority (netInstanceId);
		}
	}
}
