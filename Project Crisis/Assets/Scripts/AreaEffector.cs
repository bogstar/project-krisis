using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Collider))]
public class AreaEffector : MonoBehaviour
{
	[Header("Properties")]
	[SerializeField]
	EffectorType effectorType;
	short teamId;

	new Collider collider;

	public void Init(short teamId)
	{
		this.teamId = teamId;
	}

	private void OnTriggerEnter(Collider other)
	{
		Player p = other.GetComponent<Player>();

		if (p == null)
		{
			return;
		}

		if (p.team.teamId != teamId)
		{
			return;
		}

		switch (effectorType)
		{
			case EffectorType.AmmoRefill:
				p.GetComponent<PlayerShoot>().RefillAmmo();
				break;
			case EffectorType.Heal:
				p.RefillHealth();
				break;
		}
	}

	public enum EffectorType
	{
		Heal,
		AmmoRefill
	}
}
