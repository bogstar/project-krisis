using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Krisis.PlayerConnection;

public class PlayerList : MonoBehaviour
{
	public static PlayerList Instance;

	List<Player> players = new List<Player>();

	Dictionary<PlayerConnection, int> connToLatencyDict = new Dictionary<PlayerConnection, int>();

	public event System.Action OnPingUpdate;


	void Awake()
	{
		Instance = this;
	}

	public void AddPlayer(Player player)
	{
		if (players.Contains(player))
			return;
		
		players.Add(player);
	}

	public void RemovePlayer(Player player)
	{
		players.Remove(player);
	}
}
