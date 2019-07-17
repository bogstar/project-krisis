using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Krisis.UI
{
	public class MainMenuManager : BaseSceneManager
	{
		public MainMenu mainMenuPanel;
		public SettingsMenu settingsPanel;
		public MultiplayerMenu multiplayerMenu;
		public MatchConfigUI matchConfigMenu;
		public MyLobbyManager lobbyManager;
		public ConnectingModal connectingModal;
	}
}