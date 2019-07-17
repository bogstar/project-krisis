using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MatchManager : NetworkBehaviour
{
	[SyncVar]
	public Gamemode gamemode;
	[SyncVar]
	public PlayerCount playerCount;
	[SyncVar]
	public string map;
	[SyncVar]
	public MatchState matchState;
	[SyncVar]
	public bool friendlyFire;

	public float teamLifeCooldown = 30f;
	public int teamMaxLives = 5;

	public float countDown;

	public Team redTeam;
	public Team blueTeam;
	public Color color;

	public List<PlayerConnection> redTeamPlayerConnections { get { return redTeam.players; } }
	public List<PlayerConnection> blueTeamPlayerConnections { get { return blueTeam.players; } }
	public List<PlayerConnection> allPlayerConnections
	{
		get
		{
			List<PlayerConnection> players = new List<PlayerConnection>(redTeamPlayerConnections);
			players.AddRange(blueTeamPlayerConnections);
			return players;
		}
	}

	LevelManager levelManager;
	GameManager gameManager;

	public static PlayerConnection localPlayerConnection;

	public static MatchManager Instance { get; private set; }


	private void OnEnable()
	{
		StopAllCoroutines();
		StartCoroutine(WaitForGameManager());
		if (MyNetworkManager.Instance != null)
		{
			MyNetworkManager.Instance.RegisterServerSceneChangeCallback(OnSceneChangeServer);
		}
		if (NetworkServer.active)
		{
			StartNewMatch();
			SetMatchData();
		}
	}

	protected virtual void Awake()
	{
		if (Instance == null)
		{
			DontDestroyOnLoad(gameObject);
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Update()
	{
		if (GameManager.Instance.state == GameManager.State.Game)
		{
			if (matchState == MatchState.InPregame)
			{
				float timeLeftInPregame = countDown - (float)NetworkTime.time;

				InGameGUI.Instance.infoScreen.Display("Pregame: " + (int)timeLeftInPregame);

				if (timeLeftInPregame <= 0)
				{
					EnterInGameState();
				}
			}

			if (matchState == MatchState.InPostgame)
			{
				float timeLeftInPostgame = countDown - (float)NetworkTime.time;

				string text = InGameGUI.Instance.infoScreen.GetCurrentText();
				string[] texts = text.Split('\n');

				InGameGUI.Instance.infoScreen.Display(texts[0] + "\nTime in postgame remaining: " + (int)timeLeftInPostgame);

				if (timeLeftInPostgame <= 0)
				{
					GameManager.Instance.BackToMainMenu();
				}
			}

			if (redTeam.lives < teamMaxLives)
			{
				redTeam.timeForNextLife += Time.deltaTime;
				if (redTeam.timeForNextLife >= teamLifeCooldown)
				{
					CmdModifyTeamLife(redTeam.teamId, redTeam.lives + 1);
				}
			}
			if (blueTeam.lives < teamMaxLives)
			{
				blueTeam.timeForNextLife += Time.deltaTime;
				if (blueTeam.timeForNextLife >= teamLifeCooldown)
				{
					CmdModifyTeamLife(blueTeam.teamId, blueTeam.lives + 1);
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (Instance != this)
		{
			return;
		}

		gameManager.UnregisterLevelChangeCallback(OnSceneChange);
		MyNetworkManager.Instance.UnregisterServerSceneChangeCallback(OnSceneChangeServer);
	}

	void ChangeTeam(PlayerConnection player, short newTeamId)
	{
		// Called on Client OR Server. Setting up teams.

		Team oldTeam = newTeamId == 0 ? GetTeamFromId(1) : GetTeamFromId(0);
		Team newTeam = GetTeamFromId(newTeamId);

		oldTeam.RemovePlayer(player);
		newTeam.AddPlayer(player);
	}

	public void BeginLevel()
	{
		// Called on Server. ???????????????

		// This is only called on server. It is called when "Game" scene is loaded on Server.

		if (isServer)
		{
			CmdBeginLevel();
		}
	}

	[Command]
	void CmdBeginLevel()
	{
		MyNetworkManager.Instance.OnClientLoadedScene += OnClientLoadedScene;
		NetworkServer.Spawn(LevelManager.Instance.pickupManager.gameObject);
		LevelManager.Instance.pickupManager.gameObject.SetActive(true);
	}

	[Command]
	void CmdDoIngameShit()
	{
		foreach (var pc in allPlayerConnections)
		{
			PlayerConnection_MatchData playerMD = pc.GetComponent<PlayerConnection_MatchData>();

			if (playerMD.myAvatar != null && playerMD.myAvatar.isAlive)
			{
				playerMD.DieSilent();
				playerMD.SpawnPlayer();
			}
		}
	}

	void EnterPreGameState()
	{
		matchState = MatchState.InPregame;

		// Set the countdown till the end of pregame.
		countDown = (float)NetworkTime.time + ParameterLibrary.GetFloat(ParameterLibrary.Parameter.PREGAME_DURATION, 60f);
	}

	void EnterInGameState()
	{
		matchState = MatchState.InGame;

		CmdDoIngameShit();
		InGameGUI.Instance.infoScreen.Show(false);
	}

	void EnterPostGame()
	{
		matchState = MatchState.InPostgame;

		countDown = (float)NetworkTime.time + ParameterLibrary.GetFloat(ParameterLibrary.Parameter.POSTGAME_DURATION, 30f);
	}

	[Command]
	void CmdModifyTeamLife(short teamId, int lives)
	{
		GetTeamFromId(teamId).lives = lives;
		GetTeamFromId(teamId).timeForNextLife = 0;
		RpcModifyTeamLife(teamId, lives);
	}

	[ClientRpc]
	void RpcModifyTeamLife(short teamId, int lives)
	{
		GetTeamFromId(teamId).lives = lives;
		GetTeamFromId(teamId).timeForNextLife = 0;
	}

	IEnumerator WaitForGameManager()
	{
		while (gameManager == null)
		{
			yield return null;
			gameManager = GameManager.Instance;
		}

		gameManager.RegisterLevelChangeCallback(OnSceneChange);
	}

	void OnSceneChangeServer(string sceneName)
	{
		switch (sceneName)
		{
			case "Game":
				EnterPreGameState();
				break;
		}
	}

	void OnSceneChange(GameManager.State state)
	{
		switch (gameManager.state)
		{
			case GameManager.State.Game:
				StartCoroutine(WaitForLevelManager());
				break;
		}
	}

	IEnumerator WaitForLevelManager()
	{
		while (levelManager == null)
		{
			yield return null;
			levelManager = LevelManager.Instance;
		}

		if (isServer)
		{
			CmdSetupTeams();
		}
	}

	void OnClientLoadedScene(NetworkConnection conn)
	{
		// Currently is a callback for server when a client has loaded a new level.
		// Now we need to start up client's scene.

		TargetClientLoadedScene(conn, countDown);
	}

	[TargetRpc]
	void TargetClientLoadedScene(NetworkConnection conn, float clock)
	{
		// Locally set up scene for client.

		// Fire up the Level Manager? If it's disabled for some reason?
		LevelManager.Instance.pickupManager.gameObject.SetActive(true);

		// Setup teams
		SetupTeams();

		// Server sent us the time when the pregame will be over.
		// Used only for client side confirmation.
		countDown = clock;
	}
	
	[Command]
	void CmdSetupTeams()
	{
		TeamBase[] bases = new TeamBase[2];
		bases[0] = levelManager.redTeamBase;
		bases[1] = levelManager.blueTeamBase;

		for (int i = 0; i < 2; i++)
		{
			foreach (var spawnable in bases[i].GetSpawnables())
			{
				NetworkServer.Spawn(spawnable.gameObject);
				if (spawnable is HealthEntity he)
				{
					he.isAlive = true;
				}
				if (spawnable is Forcefield ff)
				{
					ff.InitiateHealth(500);
				}
				if (spawnable is Turret t)
				{
					t.InitiateHealth(175);
					int currentTeam = i;
					NotificationManager.Instance.RegisterTurretDeathCallback(t);
					//t.RegisterDeathCallback((att) => { NotificationManager.Instance.OnTurretDestroyed((short)currentTeam); });
				}
				if (spawnable is Headquarters hq)
				{
					hq.InitiateHealth(1000);
					int currentTeam = i;
					hq.Init((short)currentTeam);
					NotificationManager.Instance.RegisterHeadquartersDeathCallback(hq);
					hq.RegisterDeathCallback((att) => { OnHQDestroyed((short)currentTeam); });
					NotificationManager.Instance.RegisterHeadquartersHealthChangeCallback(hq);
					//hq.RegisterHealthChangeCallback((att, def, am) => { NotificationManager.Instance.OnHQAttacked(def, att, am, ref xd); });
				}
			}
		}
		SetupTeams();
	}

	void SetupTeams()
	{
		if (isServer)
		{
			blueTeam.SetBase(levelManager.blueTeamBase, NotificationManager.Instance.AlarmTeamForBaseDamage, NotificationManager.Instance.AlarmTeamForTurretDamage, NotificationManager.Instance.AlarmTeamForHqDamage);
			redTeam.SetBase(levelManager.redTeamBase, NotificationManager.Instance.AlarmTeamForBaseDamage, NotificationManager.Instance.AlarmTeamForTurretDamage, NotificationManager.Instance.AlarmTeamForHqDamage);
		}
		else
		{
			// Alarm callbacks are not needed on clients, since they are called through TargetRpcs
			blueTeam.SetBase(levelManager.blueTeamBase, null, null, null);
			redTeam.SetBase(levelManager.redTeamBase, null, null, null);
		}
	}

	void OnHQDestroyed(short teamId)
	{
		EnterPostGame();
	}

	public void StartNewMatch()
	{
		gameObject.name = "Match Manager";
		matchState = MatchState.InLobby;
		redTeam = new Team("Red", 0, Color.red, 5, 100);
		blueTeam = new Team("Blue", 1, color, 5, 100);
	}

	public void EndMatch()
	{
		Destroy(gameObject);
	}

	public void SetMatchData()
	{
		MatchConfigUI config = FindObjectOfType<MatchConfigUI>();

		gamemode = config.gamemode;
		playerCount = config.playerCount;
		map = config.map;
		friendlyFire = config.friendlyFire;
	}

	public void CheckForReadys()
	{
		bool allAreReady = true;
		foreach (var p in FindObjectsOfType<MyLobbyPlayer>())
		{
			if (p.ready == false)
			{
				allAreReady = false;
				break;
			}
		}
		if (allAreReady && isServer && FindObjectOfType<MyLobbyManager>() != null)
		{
			FindObjectOfType<MyLobbyManager>().EnableStart(true);
		}
		else
		{
			if (FindObjectOfType<MyLobbyManager>() != null)
			{
				FindObjectOfType<MyLobbyManager>().EnableStart(false);
			}
		}
	}

	public void ClientJoined(PlayerConnection player)
	{
		CmdClientJoined(player.gameObject);
	}

	[Command]
	void CmdClientJoined(GameObject playerGO)
	{
		foreach (var pc in FindObjectsOfType<PlayerConnection>())
		{
			pc.SetTeam(pc.teamId);
		}

		PlayerConnection player = playerGO.GetComponent<PlayerConnection>();

		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.Connect, player.name);

		Team team = GetTeamFromId(player.teamId);
		team.AddPlayer(player);

		player.RegisterTeamCallback((newTeam) => { ClientChangeTeam(player, newTeam); });
		player.RegisterNameCallback(ClientChangeName);

		RpcClientJoined(playerGO);
	}

	[ClientRpc]
	void RpcClientJoined(GameObject playerGO)
	{
		PlayerConnection player = playerGO.GetComponent<PlayerConnection>();

		if (isServer)
		{
			return;
		}

		Team team = GetTeamFromId(player.teamId);
		team.AddPlayer(player);

		player.RegisterTeamCallback((newTeam) => { ClientChangeTeam(player, newTeam); });
		player.RegisterNameCallback(ClientChangeName);
	}

	public void ClientLeft(PlayerConnection player)
	{
		CmdClientLeft(player.gameObject);
	}

	[Command]
	void CmdClientLeft(GameObject playerGO)
	{
		PlayerConnection player = playerGO.GetComponent<PlayerConnection>();

		Team team = GetTeamFromId(player.teamId);
		team.RemovePlayer(player);

		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.Disconnect, new HealthEntity.NetworkInfo(player.gameObject));

		player.UnregisterTeamCallback((newTeam) => { ClientChangeTeam(player, newTeam); });
		player.UnregisterNameCallback(ClientChangeName);
	}

	void ClientChangeName(string oldName, string newName)
	{
		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.NameChange, oldName, newName);
	}

	public void ClientChangeTeam(PlayerConnection player, short newTeamId)
	{
		CmdClientChangeTeam(player.gameObject, newTeamId);
	}

	public void PromoteToCaptain(PlayerConnection promotedPlayer, PlayerConnection formerCaptain)
	{

	}

	[Command]
	void CmdClientChangeTeam(GameObject playerGO, short newTeamId)
	{
		PlayerConnection player = playerGO.GetComponent<PlayerConnection>();

		ChangeTeam(player, newTeamId);

		RpcClientChangeTeam(player.gameObject, newTeamId);

		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.TeamChange, player.name, newTeamId);
	}

	[ClientRpc]
	void RpcClientChangeTeam(GameObject playerGO, short newTeamId)
	{
		PlayerConnection player = playerGO.GetComponent<PlayerConnection>();

		ChangeTeam(player, newTeamId);
	}

	/// <summary>
	/// Helper function to get a Team from teamId.
	/// </summary>
	/// <param name="teamId"></param>
	/// <returns></returns>
	public static Team GetTeamFromId(short teamId)
	{
		// Helper function independent of server or client.
		return teamId == 0 ? Instance.redTeam : Instance.blueTeam;
	}

	public enum Gamemode
	{
		Mesa
	}

	public enum PlayerCount
	{
		_2V2,
		_3V3,
		_5V5
	}

	public enum MatchState
	{
		InLobby,
		InPregame,
		InGame,
		InPostgame
	}
}