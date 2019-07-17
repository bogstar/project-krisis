using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayerConnection_LobbyData : NetworkBehaviour
{
	public PlayerConnection playerConnection { get; private set; }
	LevelManager levelManager;

	[SyncVar (hook = "OnMyLobbyAvatar")]
	public GameObject myLobbyAvatar;


	private void OnEnable()
	{
		// This only comes to existance when we enable this component.
		playerConnection = GetComponent<PlayerConnection>();
		levelManager = LevelManager.Instance;

		print("PC_LD (" + netId + ") :: OnEnable; HasAuthority: " + hasAuthority);

		StartCoroutine(WaitForAuthority());
	}

	IEnumerator WaitForAuthority()
	{
		if (GameManager.Instance.state == GameManager.State.Game)
		{
			enabled = false;
			yield break;
		}

		while (true)
		{
			if (hasAuthority)
			{
				print("PC_LD (" + netId + ") :: Authority recieved.");

				if (MatchManager.Instance == null)
				{
					continue;
				}

				CmdSpawnLobbyPlayer();
				break;
			}
			else if (myLobbyAvatar != null)
			{
				print("PC_LD (" + netId + ") :: Authority not needed. It's some other player.");
				break;
			}

			print("PC_LD (" + netId + ") :: Waiting for authority.");

			yield return null;
		}
	}

	public void ChangeName(string newName)
	{
		playerConnection.SetName(newName);
	}

	public void ChangeTeam(short newTeam)
	{
		playerConnection.SetTeam(newTeam);
	}

	[Command]
	void CmdSpawnLobbyPlayer()
	{
		MyLobbyManager lobbyManager = FindObjectOfType<MyLobbyManager>();

		GameObject newLobbyEntryGO = lobbyManager.AddPlayer();
		newLobbyEntryGO.GetComponent<MyLobbyPlayer>().playerIndex = lobbyManager.GetPlayerCount();

		if (newLobbyEntryGO == null)
		{
			return;
		}

		NetworkServer.SpawnWithClientAuthority(newLobbyEntryGO, connectionToClient);
		myLobbyAvatar = newLobbyEntryGO;
	}

	void OnMyLobbyAvatar(GameObject newAvatar)
	{
		myLobbyAvatar = newAvatar;
	}
}