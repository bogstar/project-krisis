using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MatchConfigUI : Krisis.UI.MenuPanel
{
	public Dropdown gamemodeDropdown;
	public Dropdown playerCountDropdown;
	public Dropdown mapDropdown;
	public Toggle friendlyFireToggle;

	public MatchManager.Gamemode gamemode;
	public MatchManager.PlayerCount playerCount;
	public string map;
	public bool friendlyFire;

	public Button hostButton;

	LevelScriptableObject[] allLevels;


	private void Start()
	{
		//hostButton.interactable = false;
		gamemodeDropdown.ClearOptions();
		playerCountDropdown.ClearOptions();
		mapDropdown.ClearOptions();
		friendlyFireToggle.isOn = false;
		ToggleChangeFriendlyFire(false);

		List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();
		foreach (var item in System.Enum.GetNames(typeof(MatchManager.Gamemode)))
		{
			optionDataList.Add(new Dropdown.OptionData(item));
		}
		gamemodeDropdown.AddOptions(optionDataList);
		optionDataList.Clear();
		allLevels = GeneralLibrary.Instance.GetAllItems<LevelScriptableObject>();
		foreach (var item in allLevels)
		{
			optionDataList.Add(new Dropdown.OptionData(item.name));
		}
		mapDropdown.AddOptions(optionDataList);
		optionDataList.Clear();
		DropdownChangeMap(0);
	}

	public void DropdownChangeGamemode(int newSelection)
	{
		gamemode = (MatchManager.Gamemode)newSelection;
	}

	public void DropdownChangeMap(int newSelection)
	{
		playerCountDropdown.ClearOptions();
		map = allLevels[newSelection].id;
		List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();
		foreach (var item in GeneralLibrary.Instance.GetItem<LevelScriptableObject>(map).playerCountsAvailable)
		{
			string label = "";
			switch (item)
			{
				case LevelScriptableObject.PlayerCount._2V2:
					label = "2v2";
					break;
				case LevelScriptableObject.PlayerCount._3V3:
					label = "3v3";
					break;
				default:
					label = "5v5";
					break;
			}
			optionDataList.Add(new Dropdown.OptionData(label));
		}
		playerCountDropdown.AddOptions(optionDataList);
		if (GeneralLibrary.Instance.GetItem<LevelScriptableObject>(map).scene == null)
		{
			//hostButton.interactable = false;
		}
		else
		{
			//hostButton.interactable = true;
		}
		DropdownChangePlayerCount(0);
	}

	public void DropdownChangePlayerCount(int newSelection)
	{
		playerCount = (MatchManager.PlayerCount)newSelection;
	}

	public void ToggleChangeFriendlyFire(bool toggle)
	{
		friendlyFire = toggle;
	}

	public void OnButtonPressBackToMultiplayer()
	{
		gameObject.SetActive(false);
	}

	public void OnButtonPressSecondHost()
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.DisplayMessage("Creating host at localhost...", "Cancel", CancelHost);
		MyNetworkManager.Instance.StartHost_Public(OnHostSuccessful, OnHostError, GameManager.Instance.ServerWannaChangeLevel);
	}

	void OnHostSuccessful(NetworkMessage message)
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).lobbyManager.DisplayLobby();
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.Hide();
	}

	void OnHostError(NetworkMessage message)
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.DisplayMessage("Error creating host.", "OK");
	}

	public void CancelHost()
	{
		MyNetworkManager.Instance.StopHost_Public();
	}
}