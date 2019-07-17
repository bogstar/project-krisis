using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TeamBase : MonoBehaviour
{
	[Header("References")]
	[SerializeField] Headquarters m_hq;
	[SerializeField] GameObject[] walls;
	[SerializeField] Forcefield[] forcefields;
	[SerializeField] Turret[] turrets;

	[Header("Properties")]
	[SerializeField] Color defaultTeamColor;

	public Headquarters hq { get { return m_hq; } }

	public Vector3 spawnPoint { get { return m_hq.spawnPoint; } }


	public void ColorWalls(Color color)
	{
		foreach (var w in walls)
		{
			if (w.GetComponents<Renderer>() != null)
			{
				Renderer[] rs = w.GetComponentsInChildren<Renderer>();
				foreach (var r in rs)
				{
					r.material.color = color;
				}
			}
		}
	}

	public NetworkBehaviour[] GetSpawnables()
	{
		List<NetworkBehaviour> spawnables = new List<NetworkBehaviour>(forcefields);
		spawnables.AddRange(turrets);
		spawnables.Add(m_hq);
		return spawnables.ToArray();
	}

	public void ActivateForcefields(short teamId, HealthEntity.HealthChangeDelegate AttackedCB)
	{
		foreach (var ff in forcefields)
		{
			ff.gameObject.SetActive(true);
			ff.SetTeam(teamId);
			ff.RegisterHealthChangeCallback(AttackedCB);
		}
	}

	public void ActivateTurrets(short teamId, HealthEntity.HealthChangeDelegate AttackedCB)
	{
		foreach (var t in turrets)
		{
			t.gameObject.SetActive(true);
			t.Setup(teamId);
			t.RegisterHealthChangeCallback(AttackedCB);
		}
	}

	public void SpawnHQ(HealthEntity.HealthChangeDelegate AttackedCB)
	{
		m_hq.gameObject.SetActive(true);
		m_hq.RegisterHealthChangeCallback(AttackedCB);
	}
}