using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618

public class NetworkSetup : NetworkBehaviour
{
	[SyncVar]
	public NetworkInstanceId slave;
}