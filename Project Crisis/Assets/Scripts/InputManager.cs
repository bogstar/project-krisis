using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : Singleton<InputManager>
{
	public bool lockCamera = false;
	public State state;


	bool showCursor;


	private void Start()
	{
		state = State.Free;
	}

	private void Update()
	{
		if (state == State.Chat)
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				chatCallback?.Invoke();
				//ExitChatMode();
			}
		}

		if (GameManager.Instance.state == GameManager.State.MainMenu)
		{
			return;
		}

		if (MatchManager.Instance == null)
		{
			return;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (CanOpenMainMenu())
			{
				InGameGUI.Instance.PressedESC();
			}
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			if (CanChangeTeams())
			{
				InGameGUI.Instance.PressedT();
			}
		}

		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (CanOpenScoreBoard())
			{
				InGameGUI.Instance.TabDown();
			}
			else
			{
				InGameGUI.Instance.TabUp();
			}
		}

		if (Input.GetKeyUp(KeyCode.Tab))
		{
			InGameGUI.Instance.TabUp();
		}
	}

	public void ShowCursor(bool show)
	{
		switch (show)
		{
			case false:
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
				showCursor = false;
				break;
			default:
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				showCursor = true;
				break;
		}
	}

	System.Action chatCallback;

	public void EnterChatMode(System.Action cb)
	{
		state = State.Chat;
		chatCallback = cb;
	}

	public void ExitChatMode()
	{
		state = State.Free;
		chatCallback = null;
	}

	bool CanOpenMainMenu()
	{
		return true;
	}

	bool CanChangeTeams()
	{
		return true;
	}

	bool CanOpenScoreBoard()
	{
		if (lockCamera)
		{
			return false;
		}

		return true;
	}

	bool CanChargeUpGrenade()
	{
		return true;
	}

	public enum State
	{
		Free,
		Chat
	}
}