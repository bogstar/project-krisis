using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour
{
	public Text text;

	public void SetMessage(string message)
	{
		text.text = message;
	}
}