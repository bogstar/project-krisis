using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MyLobbyPlayer : NetworkBehaviour
{
	public InputField playerNameField;
	public Dropdown playerTeamField;
	public Toggle readyToggle;
	public Button kickButton;
	public Button banButton;
	public Button captainButton;
	public Image colorSquare;
	public Text latencyLabel;

	public PlayerConnection_LobbyData myOwner;

	[SyncVar (hook = "Hook_Ready")]
	public bool ready;

	[SyncVar (hook = "Hook_PlayerIndex")]
	public int playerIndex = -1;

	MatchManager matchManager = MatchManager.Instance;


	private void Start()
	{
		// Every Lobby player will have access to this.
		playerTeamField.ClearOptions();

		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		options.Add(new Dropdown.OptionData(matchManager.redTeam.teamName));
		options.Add(new Dropdown.OptionData(matchManager.blueTeam.teamName));

		playerTeamField.AddOptions(options);
	}

	private void Update()
	{
		// Update latency.
		if (myOwner != null)
		{
			latencyLabel.text = myOwner.playerConnection.GetComponent<Krisis.PlayerConnection.Latency>().latency.ToString();
		}
	}

	// MyLobbyPlayer has appeared on the server and has NOT received authority yet!
	// This could be either a new LobbyPlayer, or a LobbyPlayer existing on the server.
	public override void OnStartClient()
	{
		// Start all the goodies needed on the LobbyPlayer.
		base.OnStartClient();

		print("Lobby Player (" + netId + ") :: I'm in OnStartClient.");

		// Lobby Player shouldn't exist if we are InGame.
		if (GameManager.Instance.state == GameManager.State.Game)
		{
			Destroy(gameObject);
			return;
		}

		// We will initiate coroutine to wait until we find our playerConnection.
		StartCoroutine(WaitForHisLobbyPlayer((pcl) =>
		{
			SetupPlayer(pcl);
		}
		));

		// If the LobbyPlayer is of another player already existing on the server, 
		/*
		Hook_Name(name);
		Hook_Team(teamId);
		*/

		// We will most certainly be in the lobby, but let's check anyway.
		// We need to wait to for Match Manager to spawn - it might not
		// get enabled the instance we spawn PC.
		/*
		if (GameManager.Instance.state == GameManager.State.MainMenu)
		{
			StartCoroutine(WaitForMatchManager(() =>
			{
				gameObject.GetComponent<PlayerConnection_LobbyData>().enabled = true;
			}
			));
		}
		*/
	}

	// LobbyPlayer has gained authority
	public override void OnStartAuthority()
	{
		// Do the goodies needed for gaining authority.
		base.OnStartAuthority();

		print("Lobby Player (" + netId + ") :: I'm in OnStartAuthority.");

		// We will initiate coroutine to wait until we find our playerConnection.
		StartCoroutine(WaitForHisLobbyPlayer((pcl) =>
		{
			SetupLocalPlayer(pcl);
		},
		true
		));
	}

	// LobbyPlayer has disconnected.
	private void OnDestroy()
	{
		// We will only remove callbacks if this is the local player.
		if (hasAuthority && myOwner != null)
		{
			myOwner.playerConnection.UnregisterTeamCallback(OnMyTeam);
			myOwner.playerConnection.UnregisterNameCallback(OnMyName);
		}
	}

	IEnumerator WaitForHisLobbyPlayer(System.Action<PlayerConnection_LobbyData> cb, bool delay = false)
	{
		while (true)
		{
			foreach (var pcl in FindObjectsOfType<PlayerConnection_LobbyData>())
			{
				if (pcl.myLobbyAvatar != null && pcl.myLobbyAvatar.GetComponent<MyLobbyPlayer>() != null && pcl.myLobbyAvatar.GetComponent<MyLobbyPlayer>() == this)
				{
					if (delay)
					{
						yield return null;
					}
					cb(pcl);
					yield break;
				}
			}
			yield return null;
		}
	}

	// Every single LobbyPlayer will be Setup-ed.
	void SetupPlayer(PlayerConnection_LobbyData owner)
	{
		// This could be our player or another player.
		print("Lobby Player (" + netId + ") :: SetupPlayer.");

		// First of all, we need to set our owner.
		myOwner = owner;

		// Player needs to broadcast their player index.
		Hook_PlayerIndex(playerIndex);
		// Player needs to broadcast their ready state.
		Hook_Ready(ready);

		// We need to see which slot we are occupying and set our position there.
		transform.SetParent(FindObjectOfType<MyLobbyManager>().lobbyEntryHolder);
		FindObjectOfType<MyLobbyManager>().InsertPlayer(gameObject, playerIndex);
		// Something messes up the scale of the transform. Let's reset it.
		transform.localScale = Vector3.one;

		// We can't change names of other players.
		playerNameField.interactable = false;
		// We can't change teams of other players.
		playerTeamField.interactable = false;
		// We can't change ready states of other players.
		EnableReady(false);

		// This other player name and team input field will be changed from other players.
		myOwner.playerConnection.RegisterNameCallback(OnMyName);
		myOwner.playerConnection.RegisterTeamCallback(OnMyTeam);
		// Let's nudge the player to refresh their name and team.
		Input_NameChange(myOwner.playerConnection.name);
		Input_TeamChange(myOwner.playerConnection.teamId);
		//myOwner.ChangeName(myOwner.playerConnection.name);
		//myOwner.ChangeTeam(myOwner.playerConnection.teamId);
		//OnMyName(myOwner.playerConnection.name);
		//OnMyTeam(myOwner.playerConnection.teamId);

		// Let's set up other player's onclick events.
		captainButton.onClick.RemoveAllListeners();
		captainButton.onClick.AddListener(() => { MatchManager.Instance.PromoteToCaptain(myOwner.playerConnection, MatchManager.localPlayerConnection); });
		kickButton.onClick.RemoveAllListeners();
		kickButton.onClick.AddListener(() => { KickPlayer(myOwner.playerConnection.connectionToClient); });
		banButton.onClick.RemoveAllListeners();
		banButton.onClick.AddListener(() => { BanPlayer(myOwner.playerConnection.connectionToClient); });

		// If we are not the server, we can't kick or ban people.
		if (!isServer)
		{
			Destroy(kickButton.gameObject);
			Destroy(banButton.gameObject);
		}
	}

	// Only our local LobbyPlayer will be Setup-ed.
	void SetupLocalPlayer(PlayerConnection_LobbyData owner)
	{	
		// This is only our local player.
		print("Lobby Player (" + netId + ") :: SetupLocalPlayer.");

		// Since we just rejoined the lobby, we will set our name.
		myOwner.ChangeName(GameManager.Instance.localPlayerName);

		// We need to re-enable the input fields.
		playerNameField.interactable = true;
		playerTeamField.interactable = true;
		EnableReady(true);

		// Let's set up our events.
		playerNameField.onEndEdit.RemoveAllListeners();
		playerNameField.onEndEdit.AddListener((newName) => { Input_NameChange(newName); });
		playerTeamField.onValueChanged.RemoveAllListeners();
		playerTeamField.onValueChanged.AddListener((newSelection) => { Input_TeamChange((short)newSelection); });
		readyToggle.onValueChanged.RemoveAllListeners();
		readyToggle.onValueChanged.AddListener(Input_ReadyToggle);

		// We can't kick or ban ourselves, so let's remove those buttons.
		Destroy(kickButton.gameObject);
		Destroy(banButton.gameObject);

		// We can't promote ourselves to captain, so let's remove that button.
		Destroy(captainButton.gameObject);
	}

	public void OnMyName(string oldName, string newName)
	{
		playerNameField.text = newName;
	}

	public void OnMyTeam(short newTeam)
	{
		playerTeamField.value = newTeam;
		colorSquare.color = MatchManager.GetTeamFromId(newTeam).GetColor();
	}

	public void Input_NameChange(string newName)
	{
		OnMyName(myOwner.playerConnection.name, newName);
		myOwner.ChangeName(newName);
	}

	public void Input_TeamChange(short newTeam)
	{
		OnMyTeam(newTeam);
		myOwner.ChangeTeam(newTeam);
	}

	public void Input_ReadyToggle(bool newReady)
	{
		CmdReadyChange(newReady);
	}

	[Command]
	void CmdReadyChange(bool newReady)
	{
		Hook_Ready(newReady);
	}

	void KickPlayer(NetworkConnection conn)
	{
		MyNetworkManager.Instance.KickPlayer(conn);
	}

	void BanPlayer(NetworkConnection conn)
	{
		MyNetworkManager.Instance.BanPlayer(conn);
	}

	public void EnableReady(bool enable)
	{
		if (hasAuthority)
		{
			readyToggle.interactable = enable;
		}
		else
		{
			readyToggle.interactable = false;
		}
	}

	// --- SyncVar Hooks --------------------------------

	void Hook_Ready(bool newReady)
	{
		ready = newReady;
		readyToggle.isOn = newReady;

		if (newReady)
		{
			playerTeamField.interactable = false;
			playerNameField.interactable = false;
		}
		else
		{
			playerTeamField.interactable = hasAuthority;
			playerNameField.interactable = hasAuthority;
		}

		MatchManager.Instance.CheckForReadys();
	}

	void Hook_PlayerIndex(int newPlayerIndex)
	{
		playerIndex = newPlayerIndex;
	}
}
