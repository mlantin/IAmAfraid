using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestroy : MonoBehaviour {

	public float m_destroyTime = 120f;
	public float m_shrinkTime = 5f;

	bool shrink = false;
	float currentLerpTime = 0;
	Vector3 targetScale = new Vector3(.001f,.001f,.001f);
	Vector3 startingScale;
	// Use this for initialization
	void Start () {
		startingScale = transform.localScale;
		StartCoroutine (delayDestroy ());
	}
	
	// Update is called once per frame
	void Update () {
		if (shrink) {
			bool destroy = false;
			currentLerpTime += Time.deltaTime;
			if (currentLerpTime > m_shrinkTime) {
				Destroy (this.gameObject);
			} else {
				float pct = currentLerpTime / m_shrinkTime;
				transform.localScale = Vector3.Lerp (startingScale, targetScale, pct);
			}
		}
	}

	IEnumerator delayDestroy() {
		yield return new WaitForSeconds(m_destroyTime);
		shrink = true;
		BoxCollider collider = gameObject.GetComponent<BoxCollider> ();
		collider.attachedRigidbody.constraints = RigidbodyConstraints.FreezeAll;
	}

}
