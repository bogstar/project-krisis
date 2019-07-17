using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#pragma warning disable CS0618

public class PingCalculator : MonoBehaviour
{
	public Text pingText;

	List<int> pings = new List<int>();
	int framerate;

	private void Update()
	{
		if (NetworkManager.singleton.isNetworkActive)
		{
			framerate = (int)(1 / Time.deltaTime);

			pings.Add(NetworkManager.singleton.client.GetRTT());

			if (pings.Count > framerate)
			{
				for (int i = 0; i < framerate - pings.Count; i++)
				{
					pings.RemoveAt(0);
				}
			}

			int sum = 0;
			foreach (var p in pings)
			{
				sum += p;
			}

			if (pings.Count > 0)
			{
				int avg = sum / pings.Count;
				pingText.text = "FPS: " + framerate + "\nPing: " + avg / 2 + " ms";
			}
		}
	}
}