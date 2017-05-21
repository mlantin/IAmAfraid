using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class TimedDestroy : NetworkBehaviour {

	public float m_destroyTime = 120f;
	public float m_shrinkTime = .3f;
	// Set this is the destroy will happen on the server
	public bool m_networked = false;

	bool shrink = false;
	float currentLerpTime = 0;
	Vector3 targetScale = new Vector3(.001f,.001f,.001f);
	Vector3 startingScale;
	// Use this for initialization
	void Start () {
		startingScale = transform.localScale;
	}

	public void activate() {
		StartCoroutine (delayDestroy ());
	}

	[ClientRpc]
	public void RpcActivate(){
		StartCoroutine (delayDestroy ());
	}

	// Update is called once per frame
	void Update () {
		if (shrink) {
			bool destroy = false;
			currentLerpTime += Time.deltaTime;
			if (currentLerpTime > m_shrinkTime) {
				if (m_networked && isServer) {
					Debug.Log ("On the server and destroying");
					IAAPlayer.localPlayer.CmdDestroyObject (netId);
				}
				else if (!m_networked)
					Destroy (gameObject);
			} else {
				float t = currentLerpTime / m_shrinkTime;
				//t = t * t * (3f - 2f * t);
				t=t*t*t * (t * (6f*t - 15f) + 10f);
				transform.localScale = Vector3.Lerp (startingScale, targetScale, t);
			}
		}
	}

	IEnumerator delayDestroy() {
		yield return new WaitForSeconds(m_destroyTime);
		shrink = true;
	}

}
