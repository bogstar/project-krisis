using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	RectTransform progressBar;
	[SerializeField]
	Text loadingLabel;

	[Header("Settings")]
	[SerializeField]
	float progressBarWidth = 300;


    void Update()
    {
		AsyncOperation levelLoad = MyNetworkManager.Instance.levelLoad;

		if (levelLoad == null)
		{
			return;
		}

		float percentDone = levelLoad.progress;

		loadingLabel.text = Mathf.RoundToInt(percentDone * 100).ToString() + "%";

		float loadingBarWidth = progressBarWidth * percentDone;

		progressBar.sizeDelta = new Vector2(loadingBarWidth, progressBar.sizeDelta.y);
	}
}
