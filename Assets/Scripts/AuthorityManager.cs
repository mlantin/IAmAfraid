using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AuthorityManager : NetworkBehaviour {

	[Command]
	public void CmdAssignObjectAuthority(NetworkInstanceId netInstanceId)
	{
		// Assign authority of this objects network instance id to the client
		bool success = false;
		GameObject obj = NetworkServer.objects [netInstanceId].gameObject;
		SoundObjectActs acts = obj.GetComponent<SoundObjectActs> ();
		// Debug.Log ("Assign authority for " + netId);
		if (acts == null || acts.m_recorder == 0) {
			success = NetworkServer.objects [netInstanceId].AssignClientAuthority (connectionToClient);
		}
		if (success)
			Debug.Log ("Successfully assigned authority to " + netInstanceId);
		else
			Debug.Log ("could not assign authority");
	}

	[Command]
	public void CmdRemoveObjectAuthority(NetworkInstanceId netInstanceId)
	{
		// Removes the  authority of this object network instance id to the client
		NetworkServer.objects[netInstanceId].RemoveClientAuthority(connectionToClient);
	}
}
