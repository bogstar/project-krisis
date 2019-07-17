using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargettingReticle : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	RectTransform upperPart;
	[SerializeField]
	RectTransform leftPart;
	[SerializeField]
	RectTransform rightPart;
	[SerializeField]
	RectTransform bottomPart;

	[Header("Settings")]
	[SerializeField]
	float spread = 80;


	public void Show(bool show)
	{
		upperPart.gameObject.SetActive(show);
		leftPart.gameObject.SetActive(show);
		rightPart.gameObject.SetActive(show);
		bottomPart.gameObject.SetActive(show);

		if (show)
		{
			Refresh();
		}
	}

	public void Refresh()
	{
		if (MatchManager.Instance == null || MatchManager.localPlayerConnection == null
			|| MatchManager.localPlayerConnection.GetComponent<PlayerConnection_MatchData>() == null
			|| MatchManager.localPlayerConnection.GetComponent<PlayerConnection_MatchData>().myAvatar == null)
		{
			spread = 80;
		}
		else
		{
			spread = MatchManager.localPlayerConnection.GetComponent<PlayerConnection_MatchData>().myAvatar.playerShoot.reticleSpread;
		}

		upperPart.localPosition = new Vector3(0, spread / 2 + 40, 0);
		bottomPart.localPosition = new Vector3(0, -(spread / 2 + 40), 0);
		leftPart.localPosition = new Vector3(-(spread / 2 + 40), 0, 0);
		rightPart.localPosition = new Vector3(spread / 2 + 40, 0, 0);
	}
}