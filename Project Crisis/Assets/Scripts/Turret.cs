using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

#pragma warning disable CS0618

public class Turret : HealthEntity
{
	[Header("References")]
	[SerializeField]
	Renderer[] renderersToColor;
	[SerializeField]
	GameObject smokeyBoi;
	[SerializeField]
	GameObject verySmokeyBoi;
	[SerializeField]
	Transform explosionLocation;

	public short teamId { get { return m_teamId; } }

	short m_teamId = 0;
	float timeSinceLastFire;
	Transform target;


	void Start()
	{
		smokeyBoi.SetActive(false);
		verySmokeyBoi.SetActive(false);
	}

	public void Setup(short teamId)
	{
		LoadHittables();

		this.m_teamId = teamId;

		foreach (var r in renderersToColor)
		{
			r.material.color = MatchManager.GetTeamFromId(teamId).teamColor;
		}
	}

	void OnHealthHook(int newHealth)
	{
		if (health / (float)maxHealth <= .5f)
		{
			smokeyBoi.SetActive(true);
		}
		if (health / (float)maxHealth <= .25f)
		{
			verySmokeyBoi.SetActive(true);
		}
	}

	void RpcDie(NetworkInfo attacker)
	{
		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.TurretKill, attacker, new NetworkInfo(gameObject));
		AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Explosion, explosionLocation.position, .7f, false);
		ParticleManager.Instance.PlayParticle(explosionLocation.position, Vector3.up, ParticleManager.ParticleType.Explosion);
		Destroy(gameObject);
	}

	public override short GetTeamId()
	{
		return teamId;
	}

	protected override bool CanTakeDamageInPregame()
	{
		return false;
	}

	protected override bool IsImmuneToFriendlyFire()
	{
		return true;
	}
}