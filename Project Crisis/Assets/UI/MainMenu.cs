using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Krisis.UI
{
	public class MainMenu : MenuPanel
	{
		public override void Hide() { }
		public override void Show() { }

		private void Start()
		{
			((MainMenuManager)GameManager.Instance.currentSceneManager).connectingModal.Hide();
		}

		public void Button_Multiplayer()
		{
			((MainMenuManager)GameManager.Instance.currentSceneManager).multiplayerMenu.Show();
		}

		public void Button_Settings()
		{
			((MainMenuManager)GameManager.Instance.currentSceneManager).settingsPanel.Show();
		}

		public void Button_Quit()
		{
			GameManager.QuitGame();
		}
	}
}