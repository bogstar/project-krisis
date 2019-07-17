using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyLobbyPlayerList : MonoBehaviour
{
	public static MyLobbyPlayerList Instance;

	public Transform playerListContentTransform;

	List<MyLobbyPlayer> players = new List<MyLobbyPlayer>();

	void Awake()
	{
		Instance = this;
	}

	public void AddPlayer(MyLobbyPlayer player)
	{
		if (players.Contains(player))
			return;

		players.Add(player);

		player.transform.SetParent(playerListContentTransform, false);

		PlayerNumberChanged();
	}

	public void RemovePlayer(MyLobbyPlayer player)
	{
		players.Remove(player);

		PlayerNumberChanged();
	}

	public void PlayerNumberChanged()
	{
		if (players.Count > 1)
		{
			foreach (var p in players)
			{
				p.EnableReady(true);
			}
		}
		else
		{
			foreach (var p in players)
			{
				p.EnableReady(false);
			}
		}
	}
}
