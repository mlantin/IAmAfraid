using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class wordActs : MonoBehaviour {

	private static Vector3 m_relpos = new Vector3(0.0f,1.6f,0.0f);
	private bool m_positioned = false;

	public Vector3 bbdim = new Vector3(0.0f,0.0f,0.0f);

	// Use this for initialization
	void Start () {
		EventTrigger trigger = GetComponent<EventTrigger>( );
		EventTrigger.Entry entry = new EventTrigger.Entry( );
		entry.eventID = EventTriggerType.PointerEnter;
		entry.callback.AddListener( ( data ) => { wordEnter( (PointerEventData)data ); } );
		trigger.triggers.Add( entry );
	}

	// Update is called once per frame
	void Update () {
		if (!m_positioned) {
			transform.position = GvrController.ArmModel.pointerRotation * Vector3.forward + 
				GvrController.ArmModel.pointerPosition + (m_relpos - 0.5f * bbdim);
			transform.rotation = GvrController.ArmModel.pointerRotation;
			if (GvrController.ClickButtonUp) {
				m_positioned = true;
			}
		}
	}

	// TODO also this should be the hover callback, not enter. Need to figure out how to do that.
	public void wordEnter( PointerEventData data ) {
		Vector3 reticleInWord;
		GvrLaserPointer pointer;
		if (GvrPointerManager.Pointer.GetType () == typeof(GvrLaserPointer)) {
			pointer = (GvrLaserPointer)GvrPointerManager.Pointer;
			// Need to get the reticle position in word coordinates....TODO
			reticleInWord = pointer.reticle.transform.position;
		}
	}
}
