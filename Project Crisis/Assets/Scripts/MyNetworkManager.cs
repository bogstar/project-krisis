using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MyNetworkManager : NetworkManager
{
	public NetworkClient currentClient { get; private set; }

	public static MyNetworkManager Instance;

	public List<string> bannedIps = new List<string>();

	short kickId = (short)MsgType.Highest + 1;
	short versId = (short)MsgType.Highest + 2;
	short serverSceneIsReadyId = (short)MsgType.Highest + 3;
	short clientSceneIsReadydId = (short)MsgType.Highest + 4;

	public System.Action<NetworkConnection> OnClientLoadedScene;

	List<NetworkConnection> kickedPlayers = new List<NetworkConnection>();

	public delegate void SceneChangeHandler(string levelName);
	event SceneChangeHandler OnServerChangeScene;

	public static float networkTime
	{
		get
		{
			return (float)NetworkTime.time;
		}
	}

	public AsyncOperation levelLoad { get; private set; }

	bool loadingLevel = false;


	private new void Awake()
	{
		base.Awake();

		if (Instance != null)
		{
			Destroy(gameObject);
		}
		else
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	private void Start()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	#region NetworkManager Callbacks
	public override void OnServerSceneChanged(string sceneName)
	{
		print("NetMan :: OnServerSceneChanged; param(sceneName): " + sceneName + " " + networkTime);

		base.OnServerSceneChanged(sceneName);

		OnServerChangeScene?.Invoke(sceneName);

		var msg = new StringMessage(sceneName);
		NetworkServer.SendToAll(serverSceneIsReadyId, msg);
	}

	public override void OnClientSceneChanged(NetworkConnection conn)
	{
		print("NetMan :: OnClientSceneChanged; connId: " + conn.connectionId + " " + networkTime);

		base.OnClientSceneChanged(conn);

		// Even though it's called on the client, it is also called on the host.
		// ?? Seems to cause errors if the server sends itself notification that it's
		// loaded a new level.

		// TODO: Maybe needs to be delayed until clients actually finish displaying level?
		if (!NetworkServer.active)
		{
			var newMsg = new EmptyMessage();
			conn.Send(clientSceneIsReadydId, newMsg);
		}
	}

	public override void OnServerAddPlayer(NetworkConnection conn)
	{
		print("NetMan :: OnServerAddPlayer; connId: " + conn.connectionId + " " + networkTime);

		if (IsPlayerAboutToBeKicked(conn))
		{
			if (!GameManager.Instance.debugging)
			{
				return;
			}
		}

		base.OnServerAddPlayer(conn);

		PlayerConnection pc = PlayerConnection.GetPlayerConnectionFromNetworkConnection(conn);

		FindObjectOfType<MatchManager>().ClientJoined(pc);
	}

	public override void OnServerAddPlayer(NetworkConnection conn, NetworkReader extraMessageReader)
	{
		print("NetMan :: OnServerAddPlayer; connId: " + conn.connectionId + "; playerControllerId; extraMessageReader: " + extraMessageReader);

		base.OnServerAddPlayer(conn, extraMessageReader);
	}

	public override void OnServerRemovePlayer(NetworkConnection conn, NetworkIdentity netId)
	{
		print("NetMan :: OnServerRemovePlayer; connId: " + conn.connectionId + "; playerGO: " + netId.netId + " " + networkTime);

		base.OnServerRemovePlayer(conn, netId);
	}

	public override void OnServerError(NetworkConnection conn, int errorCode)
	{
		print("NetMan :: OnServerError; connId: " + conn.connectionId + "; errorCode: " + errorCode + " " + networkTime);

		base.OnServerError(conn, errorCode);
	}

	public override void OnClientError(NetworkConnection conn, int errorCode)
	{
		print("NetMan :: OnClientError; connId: " + conn.connectionId + "; errorCode: " + errorCode + " " + networkTime);

		base.OnClientError(conn, errorCode);
	}

	public override void OnServerConnect(NetworkConnection conn)
	{
		print("NetMan :: OnServerConnect; connId: " + conn.connectionId + " " + networkTime);

		conn.RegisterHandler(versId, (msg) =>
		{
			if (msg.reader.ReadString() != GameManager.gameVersion)
			{
				KickPlayerWithMessage(conn, "Version mismatch.");
			}
			else if (GameManager.Instance.state == GameManager.State.Game)
			{
				KickPlayerWithMessage(conn, "You can't join a game in progress.");
			}
			else
			{
				foreach (var ip in bannedIps)
				{
					if (conn.address == ip)
					{
						KickPlayerWithMessage(conn, "You have been banned from this server.");
						return;
					}
				}

				base.OnServerConnect(conn);
			}
		});
		conn.RegisterHandler(clientSceneIsReadydId, (msg) => 
		{
			print("OnClientLoadedScene :: " + msg.conn + " " + networkTime);
			OnClientLoadedScene?.Invoke(conn);
		});
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
		print("NetMan :: OnClientConnect; connId: " + conn.connectionId + " " + networkTime);

		var newMsg = new Mirror.StringMessage(GameManager.gameVersion);
		conn.Send(versId, newMsg);

		base.OnClientConnect(conn);

		StartCoroutine(WaitForMatchManagerToSpawn(AddPlayerDelegate));
	}

	public override void OnServerDisconnect(NetworkConnection conn)
	{
		print("NetMan :: OnServerDisconnect; connId: " + conn.connectionId + " " + networkTime);

		PlayerConnection pc = PlayerConnection.GetPlayerConnectionFromNetworkConnection(conn);

		FindObjectOfType<MatchManager>().ClientLeft(pc);

		base.OnServerDisconnect(conn);
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		print("NetMan :: OnClientDisconnect; connId: " + conn.connectionId + " " + networkTime);

		base.OnClientDisconnect(conn);

		if (GameManager.Instance.state == GameManager.State.MainMenu)
		{
			if (((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).lobbyManager.gameObject.activeSelf)
			{
				((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).lobbyManager.gameObject.SetActive(false);
			}
		}
	}

	public override void OnServerReady(NetworkConnection conn)
	{
		print("NetMan :: OnServerReady; connId: " + conn.connectionId + " " + networkTime);
		
		base.OnServerReady(conn);

		// - =----------------- DEBUG
		//Vrati samo sve iz pctospawn ovamo
		if (GameManager.Instance.debugging)
		{
			StartCoroutine(WaitForPCToSpawn(conn));
		}
		else
		{
			if (GameManager.Instance.state == GameManager.State.Game)
			{
				var pcs = FindObjectsOfType<PlayerConnection>();
				foreach (var pc in pcs)
				{
					GameObject go = pc.GetComponent<PlayerConnection_MatchData>().myAvatarGameObject;
					if (go != null)
					{
						pc.GetComponent<PlayerConnection_MatchData>().SetAvatar(go);
					}
					pc.GetComponent<PlayerConnection_MatchData>().SeetPlayer();
				}

				foreach (var obj in conn.clientOwnedObjects)
				{
					if (NetworkServer.FindLocalObject(obj).GetComponent<PlayerConnection_MatchData>() != null)
					{
						PlayerConnection_MatchData md = NetworkServer.FindLocalObject(obj).GetComponent<PlayerConnection_MatchData>();
						PlayerConnection_LobbyData ld = md.GetComponent<PlayerConnection_LobbyData>();

						md.enabled = true;
						ld.enabled = false;
						//md.SpawnPlayer();

						break;
					}
				}
			}
		}
	}

	public override void OnClientNotReady(NetworkConnection conn)
	{
		print("NetMan :: OnClientNotReady; connId: " + conn.connectionId + " " + networkTime);

		base.OnClientNotReady(conn);
	}

	public override void OnStartHost()
	{
		print("NetMan :: OnStartHost" + " " + networkTime);

		base.OnStartHost();

		//MatchManager.Instance.StartNewMatch();
	}

	public override void OnStopHost()
	{
		print("NetMan :: OnStopHost" + " " + networkTime);

		base.OnStopHost();

		if (GameManager.Instance.state == GameManager.State.Game)
		{
			print("stopHost" + " " + networkTime);
			networkSceneName = "";
			//ServerChangeScene("Lobby");

		}

		//SceneManager.LoadScene("Lobby");

		//LevelManager.Instance.OnDisconnect(null);
	}

	public override void OnStartClient(NetworkClient client)
	{
		print("NetMan :: OnStartClient; client: " + client + " " + networkTime);

		if (GameManager.Instance.currentSceneManager is Krisis.UI.MainMenuManager)
		{
			client.RegisterHandler(kickId, (a) => { ((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.DisplayMessage(a.reader.ReadString(), "Back"); });
		}
		//client.UnregisterHandler(MsgType.Scene);
		client.RegisterHandler(MsgType.Scene, INeedToLoadLevel);
		client.RegisterHandler(serverSceneIsReadyId, SpawnFinished);

		if (!NetworkServer.active)
		{
			//StartCoroutine(WaitForMatchManagerToSpawn(() => { MatchManager.Instance.StartNewMatch(); }));
		}

		base.OnStartClient(client);
	}

	public override void OnStopClient()
	{
		print("NetMan :: OnStopClient" + " " + networkTime);

		base.OnStopClient();

		if (GameManager.Instance.state == GameManager.State.Game)
		{
			print("stopClient" + " " + networkTime);
			networkSceneName = "";
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}

	public override void OnStartServer()
	{
		print("NetMan :: OnStartServer" + " " + networkTime);

		base.OnStartServer();

		StartCoroutine(WaitForMatchManagerToSpawn(() => { MatchManager.Instance.SetMatchData(); }));
	}

	public override void OnStopServer()
	{
		print("NetMan :: OnStopServer" + " " + networkTime);

		base.OnStopServer();
	}
	#endregion

	IEnumerator WaitForPCToSpawn(NetworkConnection conn)
	{
		if (GameManager.Instance.state == GameManager.State.Game)
		{
			while (FindObjectsOfType<PlayerConnection>() == null || conn.clientOwnedObjects == null)
			{
				print("Waiting for PC" + " " + networkTime);
				yield return null;
			}

			var pcs = FindObjectsOfType<PlayerConnection>();
			foreach (var pc in pcs)
			{
				GameObject go = pc.GetComponent<PlayerConnection_MatchData>().myAvatarGameObject;
				if (go != null)
				{
					pc.GetComponent<PlayerConnection_MatchData>().SetAvatar(go);
				}
				pc.GetComponent<PlayerConnection_MatchData>().SeetPlayer();
			}

			foreach (var obj in conn.clientOwnedObjects)
			{
				if (NetworkServer.FindLocalObject(obj).GetComponent<PlayerConnection_MatchData>() != null)
				{
					PlayerConnection_MatchData md = NetworkServer.FindLocalObject(obj).GetComponent<PlayerConnection_MatchData>();
					PlayerConnection_LobbyData ld = md.GetComponent<PlayerConnection_LobbyData>();

					md.enabled = true;
					ld.enabled = false;
					md.SpawnPlayer();

					break;
				}
			}
		}
	}

	void SpawnFinished(NetworkMessage msg)
	{
		// Called on clients when new scene on server is finished loading.
		// Is called also on host.

		// Let's ignore if it is on host.
		if (!NetworkServer.active)
		{
			print("Spawn Finished " + networkTime);
			levelLoad.allowSceneActivation = true;
		}
	}

	void INeedToLoadLevel(NetworkMessage msg)
	{
		string levelToLoad = msg.reader.ReadString();

		print("NetMan :: INeedToLoadLevel " + levelToLoad + " " + networkTime);

		FindObjectOfType<MyLobbyManager>().loadingScreen.gameObject.SetActive(true);
		levelLoad = SceneManager.LoadSceneAsync(levelToLoad);
		levelLoad.allowSceneActivation = false;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
	{
		print("OnSceneLoaded :: " + networkTime);
		if (scene.name == "Game")
		{
			GameManager.Instance.state = GameManager.State.Game;
			InGameGUI.Instance.loadingScreen.gameObject.SetActive(true);
		}
		else
		{
			GameManager.Instance.state = GameManager.State.MainMenu;
		}
		if (NetworkServer.active)
		{
			OnServerSceneChanged(scene.name);
		}
		if (NetworkClient.active)
		{
			OnClientSceneChanged(client.connection);
		}
		levelLoad = null;
	}

	public void ChangeScene(string scene)
	{
		NetworkServer.SetAllClientsNotReady();

		levelLoad = SceneManager.LoadSceneAsync(scene);
		levelLoad.allowSceneActivation = false;
		levelLoad.completed += (ll) => { loadingLevel = false; };
		loadingLevel = true;

		networkSceneName = scene;
		var msg = new StringMessage(scene);

		foreach (var conn in NetworkServer.connections)
		{
			if (conn.Value.isConnected)
			{
				if (conn.Value.connectionId != client.connection.connectionId)
				{
					NetworkServer.SendToClient(conn.Value.connectionId, (short)MsgType.Scene, msg);
				}
			}
		}
		//ServerChangeScene(scene);

		levelLoad.allowSceneActivation = true;
	}

	public void StartClient_Public(string ipAddress, NetworkMessageDelegate onClientSuccess, NetworkMessageDelegate onClientError, NetworkMessageDelegate onSceneChange, NetworkMessageDelegate onDisconnectMessage)
	{
		networkAddress = ipAddress;
		currentClient = StartClient();

		onClientSuccess += currentClient.handlers[(short)MsgType.Connect];
		onClientSuccess += OnClientStarted;
		currentClient.RegisterHandler(MsgType.Connect, onClientSuccess);

		//currentClient.RegisterHandler(msgId, Blurb);

		//currentClient.RegisterHandler(MsgType.Scene, onSceneChange);
		/*
		currentClient.RegisterHandler(msgId, onDisconnectMessage);

		onClientError += currentClient.handlers[MsgType.Error];
		currentClient.RegisterHandler(MsgType.Error, onClientError);
		*/
	}

	public void StartHost_Public(NetworkMessageDelegate onHostSuccess, NetworkMessageDelegate onHostError, NetworkMessageDelegate onSceneChange)
	{
		currentClient = StartHost();

		if (currentClient == null)
		{
			NetworkMessage msg = new NetworkMessage();
			msg.msgType = (short)MsgType.Error;
			onHostError(msg);
			return;
		}

		onHostSuccess += currentClient.handlers[(short)MsgType.Connect];
		onHostSuccess += OnHostStarted;
		onHostError += currentClient.handlers[(short)MsgType.Error];

		currentClient.RegisterHandler(MsgType.Connect, onHostSuccess);
		currentClient.RegisterHandler(MsgType.Error, onHostError);

		//NetworkMessageDelegate onUnready = currentClient.handlers[MsgType.NotReady];
		//onUnready += UnreadyClient;
		//currentClient.RegisterHandler(MsgType.NotReady, onUnready);
	}

	public void StopHostOrClient()
	{
		StopHost_Public();
		StopClient_Public();
	}

	public void StopHost_Public()
	{
		if (NetworkServer.active)
		{
			StopHost();
		}
	}

	public void StopClient_Public()
	{
		if (NetworkClient.active)
		{
			StopClient();
		}
	}

	void AddPlayerDelegate()
	{
		print("NetMan :: Adding this local player.");
		ClientScene.AddPlayer(client.connection);
	}

	void OnHostStarted(NetworkMessage msg)
	{
		
	}

	void OnClientStarted(NetworkMessage msg)
	{
		
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

	public void KickPlayer(NetworkConnection conn)
	{
		if (NetworkServer.active)
		{
			KickPlayerWithMessage(conn, "You have been kicked from server.");
		}
	}

	public void BanPlayer(NetworkConnection conn)
	{
		if (NetworkServer.active)
		{
			bannedIps.Add(conn.address);
			KickPlayerWithMessage(conn, "You have been banned from server.");
		}
	}

	void KickPlayerWithMessage(NetworkConnection conn, string msgString)
	{
		if (!kickedPlayers.Contains(conn))
		{
			kickedPlayers.Add(conn);
		}

		var msg = new StringMessage(msgString);
		conn.Send(kickId, msg);

		StartCoroutine(DisconnectConnectionAfterDelay(conn, 0.1f));
	}

	IEnumerator DisconnectConnectionAfterDelay(NetworkConnection conn, float delay)
	{
		yield return new WaitForSeconds(delay);

		conn.Disconnect();
		if (kickedPlayers.Contains(conn))
		{
			kickedPlayers.Remove(conn);
		}
	}

	bool IsPlayerAboutToBeKicked(NetworkConnection conn)
	{
		foreach (var c in kickedPlayers)
		{
			if (c == conn)
			{
				return true;
			}
		}

		return false;
	}

	public void RegisterServerSceneChangeCallback(SceneChangeHandler cb)
	{
		OnServerChangeScene += cb;
	}

	public void UnregisterServerSceneChangeCallback(SceneChangeHandler cb)
	{
		OnServerChangeScene -= cb;
	}
}