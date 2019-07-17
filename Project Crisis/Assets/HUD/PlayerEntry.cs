using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Krisis.UI
{
	public class PlayerEntry : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		Text playerName;
		[SerializeField]
		Text playerTeam;
		[SerializeField]
		Text playerLatency;

		public void SetEntry(string name, string team, int latency)
		{
			playerName.text = name;
			playerTeam.text = team;
			playerLatency.text = latency.ToString();
		}
	}

}