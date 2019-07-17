using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
	public Text nameLabel;

	public void UpdateText(string newText)
	{
		nameLabel.text = newText;
	}
}