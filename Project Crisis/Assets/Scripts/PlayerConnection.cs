using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerConnection : NetworkBehaviour
{
	[SyncVar (hook = "Hook_Name")]
	public new string name;
	[SyncVar (hook = "Hook_Team")]
	public short teamId;

	public Krisis.PlayerConnection.Latency latency { get; private set; }

	public event System.Action<string, string> OnMyName;
	public event System.Action<short> OnMyTeam;


	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		latency = GetComponent<Krisis.PlayerConnection.Latency>();
	}

	// PlayerConnection has appeared on the server and has NOT received authority yet!
	// This could be either a new PC, or a PC existing on the server.
	public override void OnStartClient()
	{
		// Start all the goodies needed on the PC.
		base.OnStartClient();

		// If the pc is of another player already existing on the server, refresh his name and team.
		Hook_Name(name);
		Hook_Team(teamId);

		// We will most certainly be in the lobby, but let's check anyway.
		// We need to wait to for Match Manager to spawn - it might not
		// get enabled the instance we spawn PC.
		if (GameManager.Instance.state == GameManager.State.MainMenu)
		{
			StartCoroutine(WaitForMatchManager(() => 
			{
				gameObject.GetComponent<PlayerConnection_LobbyData>().enabled = true;
			}
			));
		}
	}

	IEnumerator WaitForMatchManager(System.Action callback)
	{
		do
		{
			if (FindObjectOfType<MatchManager>() != null)
			{
				if (MatchManager.Instance.matchState == MatchManager.MatchState.InLobby)
				{
					print("PC (" + netId + ") :: Lobby is activated; activating LobbyData component.");
					callback();
					break;
				}
				else
				{
					print("PC (" + netId + ") :: MatchManager found, can spawn, but isn't in lobby yet.");
				}
			}

			print("PC (" + netId + ") :: Still waiting for MatchManager.");
			yield return null;
		}
		while (true);
	}

	// PlayerConnection has gained authority
	public override void OnStartAuthority()
	{
		// Do the goodies needed for gaining authority.
		base.OnStartAuthority();

		// If this PC has gained authority, it means it's the local player.
		MatchManager.localPlayerConnection = this;
	}

	// PlayerConnection has disconnected.
	private void OnDestroy()
	{
		// We will only alert the Match Manager if this was the local player.
		if (hasAuthority)
		{
			MatchManager.Instance.ClientLeft(this);
		}
	}

	/// <summary>
	/// Change this player's team. Will only work if PC has authority.
	/// </summary>
	/// <param name="newTeam"></param>
	public void SetTeam(short newTeam)
	{
		CmdSetTeam(newTeam);
	}

	[Command]
	void CmdSetTeam(short newTeam)
	{
		teamId = newTeam;
	}

	/// <summary>
	/// Change this player's name. Will only work if PC has authority.
	/// </summary>
	/// <param name="newName"></param>
	public void SetName(string newName)
	{
		CmdSetName(newName);
	}

	[Command]
	void CmdSetName(string newName)
	{
		if (!hasAuthority)
		{
			return;
		}

		// We regex all html tags from the name and shorten it to 10 characters.
		newName = System.Text.RegularExpressions.Regex.Replace(newName, "<.*?>", string.Empty);

		if (newName.Length > 10)
		{
			newName = newName.Remove(10);
		}

		if (name != newName)
		{
			name = newName;
			GameManager.Instance.localPlayerName = newName;
		}
	}

	/// <summary>
	/// Send Chat Message to server. Works only if PC has authority.
	/// </summary>
	/// <param name="message"></param>
	public void SendChatMessage(string message)
	{
		// Called locally when we want to send a message to the server.
		if (hasAuthority)
		{
			CmdSendChatMessage(gameObject, message);
		}
	}

	[Command]
	void CmdSendChatMessage(GameObject sender, string message)
	{
		// We have received a message from the client.
		// We will broadcast it to the other clients.
		RpcSendChatMessage(sender, message);
	}

	[ClientRpc]
	void RpcSendChatMessage(GameObject sender, string message)
	{
		// We have received a message from the server.
		// If this is our message, we will not display it again.
		if (!hasAuthority)
		{
			FindObjectOfType<ChatManager>().DisplayChatMessage(sender.GetComponent<PlayerConnection>(), message);
		}
	}

	#region SyncVar Hooks
	void Hook_Name(string newName)
	{
		// Game Manager will set local player name only if we have authority.
		if (hasAuthority)
		{
			GameManager.Instance.localPlayerName = newName;
		}

		string oldName = name;
		name = newName;
		gameObject.name = "Player Connection (" + newName + ")";

		OnMyName?.Invoke(oldName, newName);
	}

	void Hook_Team(short newTeam)
	{
		teamId = newTeam;

		OnMyTeam?.Invoke(teamId);
	}
	#endregion

	// --------------- Callback Registrations --------------------

	public void RegisterTeamCallback(System.Action<short> cb)
	{
		OnMyTeam += cb;
	}

	public void UnregisterTeamCallback(System.Action<short> cb)
	{
		OnMyTeam -= cb;
	}

	public void RegisterNameCallback(System.Action<string, string> cb)
	{
		OnMyName += cb;
	}

	public void UnregisterNameCallback(System.Action<string, string> cb)
	{
		OnMyName -= cb;
	}

	// Static Funcs

	public static PlayerConnection GetPlayerConnectionFromNetworkConnection(NetworkConnection conn)
	{
		foreach (var objNetId in conn.clientOwnedObjects)
		{
			PlayerConnection pc = ClientScene.FindLocalObject(objNetId).GetComponent<PlayerConnection>();
			if (pc != null)
				return pc;
		}
		return null;
	}
}