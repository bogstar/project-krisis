using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameGUI : Singleton<InGameGUI>
{
	[SerializeField]
	Image damageIndicator;

	public LoadingScreen loadingScreen;

	public GameObject hitIndicatorPrefab;
	public Transform changeTeamPanel;
	public Transform escapeMenu;
	public Transform scoreMenu;
	public InformationScreen infoScreen;
	public GameObject spawnPanel;
	public InputField nameInputField;
	public Dropdown teamDropdown;
	public TargettingReticle targetingReticle;
	public Transform notificationHolder;

	public Text livesOresText;

	public bool lockCamera;


	private void Start()
	{
		InputManager.Instance.ShowCursor(true);
		LevelManager.Instance.EnableOverlookCamera(true);
		targetingReticle.gameObject.SetActive(false);

		teamDropdown.ClearOptions();

		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		options.Add(new Dropdown.OptionData(MatchManager.Instance.redTeam.teamName));
		options.Add(new Dropdown.OptionData(MatchManager.Instance.blueTeam.teamName));

		teamDropdown.AddOptions(options);
	}

	public void DisplayChangeTeamPanel(bool display)
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		if (display)
		{
			teamDropdown.value = localPlayer.teamId;
			nameInputField.text = localPlayer.name;
		}
		changeTeamPanel.gameObject.SetActive(display);
	}

	public void OnButton_Back()
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		escapeMenu.gameObject.SetActive(false);
		if (localPlayer.GetComponent<PlayerConnection_MatchData>().isPlayerAlive)
		{
			InputManager.Instance.ShowCursor(false);
			targetingReticle.gameObject.SetActive(true);
			lockCamera = false;
		}
	}

	public void OnButton_Disconnect()
	{
		LevelManager.Instance.EnableOverlookCamera(true);
		escapeMenu.gameObject.SetActive(false);
		InputManager.Instance.ShowCursor(true);
		GameManager.Instance.BackToMainMenu();
	}

	public void OnButton_Suicide()
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		localPlayer.GetComponent<PlayerConnection_MatchData>().Die(HealthEntity.NetworkInfo.nobody);
		escapeMenu.gameObject.SetActive(false);
		InputManager.Instance.ShowCursor(false);
		lockCamera = false;
		targetingReticle.gameObject.SetActive(false);
	}

	public void DisplaySpawnPanel(bool display)
	{
		spawnPanel.SetActive(display);
	}

	public void OnButton_ChangeTeam()
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		DisplayChangeTeamPanel(false);
		InputManager.Instance.ShowCursor(false);
		lockCamera = false;
		LevelManager.Instance.EnableOverlookCamera(false);
		targetingReticle.gameObject.SetActive(true);

		if (localPlayer.GetComponent<PlayerConnection_MatchData>().playerConnection.teamId != (short)teamDropdown.value)
		{
			localPlayer.GetComponent<PlayerConnection_MatchData>().Die(HealthEntity.NetworkInfo.nobody);
			localPlayer.GetComponent<PlayerConnection_MatchData>().playerConnection.SetTeam((short)teamDropdown.value);

			escapeMenu.gameObject.SetActive(false);
			InputManager.Instance.ShowCursor(false);
			lockCamera = false;
			targetingReticle.gameObject.SetActive(false);
		}

		if (localPlayer.GetComponent<PlayerConnection_MatchData>().playerConnection.name != nameInputField.text)
		{
			localPlayer.GetComponent<PlayerConnection_MatchData>().playerConnection.SetName(nameInputField.text);
		}
	}

	public void PressedT()
	{
		if (!escapeMenu.gameObject.activeSelf && !changeTeamPanel.gameObject.activeSelf)
		{
			if (!changeTeamPanel.gameObject.activeSelf)
			{
				DisplayChangeTeamPanel(true);
				InputManager.Instance.ShowCursor(true);
				lockCamera = true;
				targetingReticle.gameObject.SetActive(false);
			}
		}
	}

	public void PressedESC()
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		if (escapeMenu.gameObject.activeSelf)
		{
			escapeMenu.gameObject.SetActive(false);

			if (changeTeamPanel.gameObject.activeSelf)
			{
				return;
			}

			if (localPlayer.GetComponent<PlayerConnection_MatchData>().isPlayerAlive)
			{
				InputManager.Instance.ShowCursor(false);
				lockCamera = false;
				targetingReticle.gameObject.SetActive(true);
			}
		}
		else if (changeTeamPanel.gameObject.activeSelf)
		{
			DisplayChangeTeamPanel(false);

			if (localPlayer.GetComponent<PlayerConnection_MatchData>().isPlayerAlive)
			{
				InputManager.Instance.ShowCursor(false);
				lockCamera = false;
				targetingReticle.gameObject.SetActive(true);
			}
		}
		else
		{
			escapeMenu.gameObject.SetActive(true);

			targetingReticle.gameObject.SetActive(false);
			lockCamera = true;
			changeTeamPanel.gameObject.SetActive(false);
			InputManager.Instance.ShowCursor(true);
		}
	}

	public void TabDown()
	{
		scoreMenu.gameObject.SetActive(true);
	}

	public void TabUp()
	{
		scoreMenu.gameObject.SetActive(false);
	}

	public void OnButton_Spawn()
	{
		PlayerConnection localPlayer = MatchManager.localPlayerConnection;

		localPlayer.GetComponent<PlayerConnection_MatchData>().SpawnPlayer();
	}

	public void ClearScreen()
	{
		targetingReticle.gameObject.SetActive(true);
		lockCamera = false;
		InputManager.Instance.ShowCursor(false);
		DisplaySpawnPanel(false);
	}

	public void DisplayDeadScreen()
	{
		InputManager.Instance.ShowCursor(true);
		DisplaySpawnPanel(true);
		LevelManager.Instance.EnableOverlookCamera(true);
	}

	public void OnButton_Quit()
	{
		GameManager.QuitGame();
	}

	public void ChangeSensitivity(float newSens)
	{
		GameManager.SetMouseSensitivity(newSens);
	}
}