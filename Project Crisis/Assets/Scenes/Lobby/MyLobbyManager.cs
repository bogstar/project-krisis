using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyLobbyManager : Krisis.UI.MenuPanel
{
	public GameObject emptyPlayerSlotPrefab;

	public GameObject[] playerSlots;

	public Text matchInfoLabel;

	public Transform lobbyEntryHolder;

	public Button startGameButton;

	[Header("References")]
	[SerializeField]
	public LoadingScreen loadingScreen;

	GameManager gameManager;


	private void Start()
	{
		gameManager = GameManager.Instance;
		EnableStart(false);
		loadingScreen.gameObject.SetActive(false);
	}

	public int GetPlayerCount()
	{
		int count = 0;
		for (int i = 0; i < playerSlots.Length; i++)
		{
			if (playerSlots[i].GetComponent<MyLobbyPlayer>() != null)
			{
				count++;
			}
		}

		return count;
	}

	// Gets called only on the server.
	public GameObject AddPlayer()
	{
		int maxPlayers = 0;

		switch (MatchManager.Instance.playerCount)
		{
			case MatchManager.PlayerCount._2V2:
				maxPlayers = 4;
				break;
			case MatchManager.PlayerCount._3V3:
				maxPlayers = 6;
				break;
			case MatchManager.PlayerCount._5V5:
				maxPlayers = 10;
				break;
		}

		if (GetPlayerCount() >= maxPlayers)
		{
			return null;
		}

		GameObject newLobbyEntryGO = Instantiate(GameManager.Instance.lobbyEntryPrefab);

		return newLobbyEntryGO;
	}

	public void InsertPlayer(GameObject newLobbyEntryGO, int index)
	{
		MyLobbyPlayer lobbyPlayer = playerSlots[index].GetComponent<MyLobbyPlayer>();

		if (lobbyPlayer == null)
		{
			Destroy(playerSlots[index].gameObject);
			playerSlots[index] = newLobbyEntryGO;
			playerSlots[index].transform.SetSiblingIndex(index);
			newLobbyEntryGO.name = "Player slot " + (index + 1) + " (" + newLobbyEntryGO.GetComponent<MyLobbyPlayer>().myOwner.playerConnection.name + ")";
		}
	}

	public void RemovePlayer(int index)
	{
		Destroy(playerSlots[index].gameObject);

		for (int i = index; i < playerSlots.Length - 1; i++)
		{
			playerSlots[i] = playerSlots[i + 1];
			if (playerSlots[i].GetComponent<MyLobbyPlayer>() != null)
			{
				playerSlots[i].GetComponent<MyLobbyPlayer>().playerIndex--;
				if (playerSlots[i].GetComponent<MyLobbyPlayer>().myOwner != null)
				{
					playerSlots[i].name = "Player slot " + (i + 1) + " (" + playerSlots[i].GetComponent<MyLobbyPlayer>().myOwner.playerConnection.name + ")";
				}
				else
				{
					playerSlots[i].name = "Player slot " + (i + 1) + " (Unknown Player)";
				}
			}
			else
			{
				playerSlots[i].name = "Player slot " + (i + 1) + " (empty)";
			}
		}

		playerSlots[playerSlots.Length - 1] = Instantiate(emptyPlayerSlotPrefab, lobbyEntryHolder);
		playerSlots[playerSlots.Length - 1].name = "Player slot " + (playerSlots.Length) + " (empty)";
	}

	public void DisplayLobby()
	{
		gameObject.SetActive(true);

		StartCoroutine(WaitForMatchManagerToSpawn(() =>
		{
			int maxPlayers = 0;
			string label = "";
			switch (MatchManager.Instance.playerCount)
			{
				case MatchManager.PlayerCount._2V2:
					label = "2v2";
					maxPlayers = 4;
					break;
				case MatchManager.PlayerCount._3V3:
					label = "3v3";
					maxPlayers = 6;
					break;
				case MatchManager.PlayerCount._5V5:
					label = "5v5";
					maxPlayers = 10;
					break;
			}

			matchInfoLabel.text = MatchManager.Instance.gamemode + "\n" + label + "\n"
				+ GeneralLibrary.Instance.GetItem<LevelScriptableObject>(MatchManager.Instance.map).name + "\n"
				+ ((MatchManager.Instance.friendlyFire == true) ? "On" : "Off") + "\n";

			playerSlots = new GameObject[maxPlayers];

			for (int i = lobbyEntryHolder.childCount - 1; i > -1; i--)
			{
				Destroy(lobbyEntryHolder.GetChild(i).gameObject);
			}

			for (int i = 0; i < maxPlayers; i++)
			{
				playerSlots[i] = Instantiate(emptyPlayerSlotPrefab, lobbyEntryHolder);
				playerSlots[i].name = "Player slot " + (i + 1) + " (empty)";
			}
		}
		));
	}

	IEnumerator WaitForMatchManagerToSpawn(System.Action delegat)
	{
		while (true)
		{
			if (FindObjectOfType<MatchManager>() != null)
			{
				delegat();
				break;
			}

			yield return null;
		}
	}

	public void OnButtonPressBack()
	{
		if (MyNetworkManager.Instance.isNetworkActive)
		{
			MyNetworkManager.Instance.StopHostOrClient();
		}
		gameObject.SetActive(false);
	}

	public void OnButtonPressStart()
	{
		EnableStart(false);
		GameManager.Instance.OnButtonPressStartGame();
		loadingScreen.gameObject.SetActive(true);
	}

	public void EnableStart(bool enable)
	{
		startGameButton.interactable = enable;
	}
}