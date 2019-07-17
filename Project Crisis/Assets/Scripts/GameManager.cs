using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : Singleton<GameManager>
{
	public static string gameVersion = "3";

	public GameObject inGameGuiPrefab;
	public GameObject lobbyEntryPrefab;

	public bool debugging;

	public static string[] names = { "Jeremy", "Janna", "Kha'Zix", "Cho'Gath", "Pippin", "Merry", "James Bond" };

	public string localPlayerName;
	public string level;

	public BaseSceneManager currentSceneManager;

	public State state;

	public float mouseSensitivity = -1;

	event System.Action<State> OnSceneLoad;


	protected override void Awake()
	{
		base.Awake();

		currentSceneManager = FindObjectOfType<BaseSceneManager>();
	}

	private void Start()
	{
		// Game start. Load Game Scene.
		state = (SceneManager.GetActiveScene().name == "Game") ? State.Game : State.MainMenu;

		SceneManager.sceneLoaded += OnSceneLoaded;

		#region DEBUG
		if (debugging)
		{
			MyNetworkManager.Instance.StartHost_Public(null, null, null);
		}
		else
		{
			if (SceneManager.GetActiveScene().name == "Game")
				SceneManager.LoadScene("Lobby");
		}
		#endregion
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.buildIndex == 1)
		{
			state = State.Game;

			MatchManager.Instance.BeginLevel();
		}
		else
		{
			state = State.MainMenu;
			
			currentSceneManager = FindObjectOfType<Krisis.UI.MainMenuManager>();
			
		}

		OnSceneLoad?.Invoke(state);
	}

	/// <summary>
	/// Only real way to quit game.
	/// </summary>
	public static void QuitGame()
	{
		Application.Quit();
	}

	public static void SetMouseSensitivity(float newSensitivity)
	{
		Instance.mouseSensitivity = newSensitivity;
		PlayerPrefs.SetFloat("mouseSensitivity", newSensitivity);
	}

	public static float GetMouseSensitivity()
	{
		if (Instance.mouseSensitivity < 0)
		{
			Instance.mouseSensitivity = PlayerPrefs.GetFloat("mouseSensitivity", ParameterLibrary.GetFloat(ParameterLibrary.Parameter.MOUSE_SENSITIVITY_DEFAULT, 1f));
		}

		return Instance.mouseSensitivity;
	}

	public void OnButtonPressStartGame()
	{
		MyNetworkManager.Instance.ChangeScene(level);
	}

	public void OnLocalNameChange(string newName)
	{
		localPlayerName = newName;
	}

	public void BackToMainMenu()
	{
		MyNetworkManager.Instance.StopHostOrClient();
	}

	public void ServerWannaChangeLevel(NetworkMessage msg)
	{
		string levelName = msg.reader.ReadString();
		print(levelName);

		if (state == State.Game)
		{
			if (levelName == "Lobby")
			{
				SceneManager.LoadScene(levelName);
			} 
		}
		else
		{
			if (levelName == "Game")
			{
				print(MyNetworkManager.Instance.currentClient.connection.isReady);

				NetworkMessage msgg = new NetworkMessage();
				msgg.msgType = (short)MsgType.NotReady;
				msgg.conn = MyNetworkManager.Instance.currentClient.connection;
				print(msgg.conn);

				print(MyNetworkManager.Instance.currentClient.connection.isReady);
				SceneManager.LoadScene(levelName);
			}
		}
	}

	#region Public callback registration methods
	public void RegisterLevelChangeCallback(System.Action<State> cb)
	{
		OnSceneLoad += cb;
	}

	public void UnregisterLevelChangeCallback(System.Action<State> cb)
	{
		OnSceneLoad -= cb;
	}
	#endregion

	#region ENUMS
	public enum State
	{
		MainMenu,
		Game
	}
	#endregion
}
