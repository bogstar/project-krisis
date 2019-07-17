using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	Text textLabel;

	float expiryTime;


	private void Update()
	{
		if (Time.time > expiryTime)
		{
			Destroy(gameObject);
		}
	}

	public void Display(string text, float lifetime)
	{
		textLabel.text = text;
		expiryTime = Time.time + lifetime;
	}
}