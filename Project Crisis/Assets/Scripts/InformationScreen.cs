using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InformationScreen : MonoBehaviour
{
	[Header("References")]
	[SerializeField] Text label;

	public void Display(string text)
	{
		Show(true);
		label.text = text;
	}

	public void Show(bool show)
	{
		gameObject.SetActive(show);
	}

	public string GetCurrentText()
	{
		return label.text;
	}
}