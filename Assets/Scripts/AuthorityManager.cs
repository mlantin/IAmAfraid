using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AuthorityManager : NetworkBehaviour {

	[Command]
	public void CmdAssignObjectAuthority(NetworkInstanceId netInstanceId)
	{
		// Assign authority of this objects network instance id to the client
		NetworkServer.objects[netInstanceId].AssignClientAuthority(connectionToClient);
	}

	[Command]
	public void CmdRemoveObjectAuthority(NetworkInstanceId netInstanceId)
	{
		// Removes the  authority of this object network instance id to the client
		NetworkServer.objects[netInstanceId].RemoveClientAuthority(connectionToClient);
	}
}
