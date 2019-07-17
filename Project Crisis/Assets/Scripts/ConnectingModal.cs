using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectingModal : MonoBehaviour
{
	public Text message;
	public Button button;

	public void DisplayMessage(string message, string button, UnityEngine.Events.UnityAction btnClbk)
	{
		this.message.text = message;
		this.button.transform.GetChild(0).GetComponent<Text>().text = button;
		this.button.onClick.RemoveAllListeners();
		this.button.onClick.AddListener(Hide);
		this.button.onClick.AddListener(btnClbk);
		Show();
	}

	public void DisplayMessage(string message, string button)
	{
		this.message.text = message;
		this.button.transform.GetChild(0).GetComponent<Text>().text = button;
		this.button.onClick.RemoveAllListeners();
		this.button.onClick.AddListener(Hide);
		Show();
	}

	public void Show()
	{
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}