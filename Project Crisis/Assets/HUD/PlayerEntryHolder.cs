using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Krisis.PlayerConnection;

namespace Krisis.UI
{
	public class PlayerEntryHolder : MonoBehaviour
	{
		[Header("Prefabs")]
		[SerializeField]
		GameObject entryPrefab;

		private void Update()
		{
			var objs = FindObjectsOfType<PlayerConnection_MatchData>();
			for (int i = transform.childCount - 1; i > -1; i--)
			{
				Destroy(transform.GetChild(i).gameObject);
			}
			foreach (var o in objs)
			{
				PlayerEntry e = Instantiate(entryPrefab, transform).GetComponent<PlayerEntry>();
				e.SetEntry(o.playerConnection.name, "<color=#" + ColorUtility.ToHtmlStringRGBA(o.team.GetColor()) + ">" + o.team.teamName + "</color>", o.GetComponent<Latency>().latency);
			}
		}
	}
}