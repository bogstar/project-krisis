using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MultiplayerMenu : Krisis.UI.MenuPanel
{
	public InputField ipField;
	public InputField localPlayerNameField;

	private void OnEnable()
	{
		if (GameManager.Instance.localPlayerName == "")
		{
			localPlayerNameField.text = GameManager.names[Random.Range(0, GameManager.names.Length)];
			OnNameChange(localPlayerNameField.text);
		}
		else
		{
			localPlayerNameField.text = GameManager.Instance.localPlayerName;
		}
	}

	public void OnButtonPressBackToMain()
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).multiplayerMenu.gameObject.SetActive(false);
	}

	public void OnButtonPressFirstHost()
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).matchConfigMenu.gameObject.SetActive(true);
		OnNameChange(localPlayerNameField.text);
	}

	public void OnButtonPressJoin()
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.DisplayMessage("Connecting to " + ipField.text + "...", "Cancel", CancelJoin);
		OnNameChange(localPlayerNameField.text);

		MyNetworkManager.Instance.StartClient_Public(ipField.text, OnClientConnectSuccess, ConnectionError, GameManager.Instance.ServerWannaChangeLevel, OnDisconnectMessage);
	}

	public void OnDisconnectMessage(NetworkMessage netMsg)
	{
		print(netMsg.reader.ReadString());
	}

	public void OnClientConnectSuccess(NetworkMessage netMsg)
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.Hide();
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).lobbyManager.DisplayLobby();
	}

	public void CancelJoin()
	{
		MyNetworkManager.Instance.StopClient_Public();
	}

	public void ConnectionError(NetworkMessage netMsg)
	{
		((Krisis.UI.MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.DisplayMessage("Error connecting to server.", "OK");
	}

	public void OnNameChange(string newName)
	{
		GameManager.Instance.OnLocalNameChange(localPlayerNameField.text);
	}
}