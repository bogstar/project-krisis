using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Headquarters : HealthEntity
{
	[SerializeField] short teamId;
	[SerializeField] Transform explosionLocs;
	[SerializeField] float explosionsRarity;
	[SerializeField] Transform m_spawnPoint;
	[SerializeField] AreaEffector armory;
	[SerializeField] AreaEffector hospital;

	public Vector3 spawnPoint { get { return m_spawnPoint.position; } }

	private void Start()
	{
		LoadHittables();
	}

	public void Init(short teamId)
	{
		armory.Init(teamId);
		hospital.Init(teamId);
	}

	void RpcDie()
	{
		StartCoroutine(Explode());
	}

	IEnumerator Explode()
	{
		while (true)
		{
			yield return new WaitForSeconds(explosionsRarity);

			Vector3 explosionPos = explosionLocs.GetChild(Random.Range(0, explosionLocs.childCount)).position;

			ParticleManager.Instance.PlayParticle(explosionPos, Vector3.up, ParticleManager.ParticleType.Explosion);
			AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Explosion, explosionPos, .6f, false);
		}
	}

	protected override bool CanTakeDamageInPregame()
	{
		return false;
	}

	protected override bool IsImmuneToFriendlyFire()
	{
		return true;
	}

	public override short GetTeamId()
	{
		return teamId;
	}
}