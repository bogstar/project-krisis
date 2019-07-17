using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Forcefield : HealthEntity
{
	[Header("Forcefield References")]
	[SerializeField]
	Collider[] blockerColliders;
	[SerializeField]
	Renderer[] visuals;

	[Header("Forcefield Variables")]
	[SerializeField]
	int regenPerSecond;
	[SerializeField]
	float onReviveHealthPercGain;
	[SerializeField]
	float deathDuration;
	[SerializeField]
	[Range(0, 255)]
	int startingOpacityAlpha;

	bool isDead;
	float deathTime;
	float regenTick = .25f;
	int regenPerTick;

	[SyncVar]
	short m_teamId;

	public short teamId { get { return m_teamId; } }


	private void Start()
	{
		foreach (var col in blockerColliders)
		{
			col.isTrigger = false;
		}

		LoadHittables();

		EnableVisuals(true);
		EnableColliders(true);
		EnableHittables(true);
		SetRegen(regenPerSecond);
	}

	float lastRegenTick;

	private void Update()
	{
		if (isServer)
		{
			CmdTickRegen();
		}
	}

	[Command]
	void CmdTickRegen()
	{
		if (!isAlive)
		{
			if (Time.time > deathTime)
			{
				Revive();
			}
		}
		else
		{
			if (Time.time > lastRegenTick)
			{
				TakeHealing(regenPerTick);
				lastRegenTick = Time.time + regenTick;
			}
		}
	}

	void EnableVisuals(bool enable)
	{
		foreach (var v in visuals)
		{
			v.enabled = enable;
		}
	}

	void SetVisualsAlpha(float alpha)
	{
		foreach (var v in visuals)
		{
			v.material.color = new Color(v.material.color.r, v.material.color.g, v.material.color.b, alpha);
		}
	}

	void EnableColliders(bool enable)
	{
		foreach (var col in blockerColliders)
		{
			col.enabled = enable;
		}
	}

	public void SetRegen(int regenPerSec)
	{
		int tick = (int)(1 / regenTick);
		regenPerTick = regenPerSec / tick;
	}

	void OnHealthHook(int newHealth)
	{
		float percHealth = health / (float)maxHealth;
		float newAlpha = Mathf.Lerp(50, startingOpacityAlpha, percHealth);
		SetVisualsAlpha(newAlpha / 255);
	}

	void Revive()
	{
		isAlive = true;
		TakeHealing((int)(onReviveHealthPercGain * maxHealth));
		RpcEnableForcefield(true);
		RpcDisplayReviveNotification();
	}

	void RpcDisplayReviveNotification()
	{
		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.ForcefieldRevive, new NetworkInfo(gameObject));
	}

	public void SetTeam(short teamId)
	{
		CmdSetTeam(teamId);
	}
	
	[Command]
	void CmdSetTeam(short teamId)
	{
		m_teamId = teamId;
	}

	public void IgnoreCollisions(Collider[] colsExt)
	{
		foreach (var colExt in colsExt)
		{
			IgnoreCollisions(colExt);
		}
	}

	public void IgnoreCollisions(Collider colExt)
	{
		foreach (var colInt in blockerColliders)
		{
			Physics.IgnoreCollision(colInt, colExt);
		}
	}

	protected override void Die(NetworkInfo attacker)
	{
		base.Die(attacker);

		deathTime = Time.time + deathDuration;
		RpcEnableForcefield(false);
	}

	void RpcDie(NetworkInfo attacker)
	{
		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.ForcefieldKill, attacker, new NetworkInfo(gameObject));
	}

	[ClientRpc]
	void RpcEnableForcefield(bool enable)
	{
		EnableVisuals(enable);
		EnableColliders(enable);
		EnableHittables(enable);
	}

	public override short GetTeamId()
	{
		return teamId;
	}
}