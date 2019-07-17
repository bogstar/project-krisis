using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkTransform))]
public class Grenade : NetworkBehaviour
{
	public new Rigidbody rigidbody { get; private set; }
	public GrenadeScriptableObject grenadeData { get; private set; }

	Collider[] colliders;

	bool armed = false;

	public float explosionTimestamp;
	Player thrower;

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		if (!armed)
		{
			return;
		}

		if (NetworkTime.time > explosionTimestamp)
		{
			armed = false;
			AudioManager.Instance.PlayAudioOnLocation(grenadeData.audioClip, transform.position, 1f, true);
			ParticleManager.Instance.PlayParticle(transform.position, Vector3.up, grenadeData.explosionParticle);

			if (NetworkServer.active)
			{
				CmdExplode();
			}
			else
			{
				Destroy(gameObject, 1f);
			}

			RenderInvisible();
		}
	}

	public void Init(GrenadeScriptableObject grenadeSO)
	{
		grenadeData = grenadeSO;
		Instantiate(grenadeData.model, transform).name = "Graphics";
		colliders = GetComponentsInChildren<Collider>();
	}

	private void RenderInvisible()
	{
		foreach (var r in GetComponentsInChildren<Renderer>())
		{
			r.enabled = false;
		}
	}

	public void AdjustExplosionTimestamp(float timestamp)
	{
		explosionTimestamp = timestamp;
	}

	public float Arm(Player thrower, float throwStrength = -1)
	{
		this.thrower = thrower;
		if (throwStrength >= 0)
		{
			throwStrength = Mathf.Clamp01(throwStrength);
			float tickerDuration = Mathf.Lerp(grenadeData.minTimer, grenadeData.maxTimer, throwStrength);
			AdjustExplosionTimestamp((float)NetworkTime.time + tickerDuration);
		}
		
		armed = true;

		Forcefield[] ffs = FindObjectsOfType<Forcefield>();
		foreach (var ff in ffs)
		{
			if (ff.teamId == thrower.team.teamId)
			{
				ff.IgnoreCollisions(colliders);
			}
		}

		return explosionTimestamp;
	}

	[Command]
	void CmdExplode()
	{
		if (NetworkTime.time <= explosionTimestamp)
		{
			Debug.LogWarning("Grenade :: CmdExplode: Trying to explode without time being up.");
		}

		Collider[] colliders = Physics.OverlapSphere(transform.position, grenadeData.range);
		Dictionary<HealthEntity, float> listOfClosestHittablesWithDst = new Dictionary<HealthEntity, float>();

		foreach (var c in colliders)
		{
			if (c.GetComponent<HittableArea>() != null)
			{
				HealthEntity he = c.GetComponent<HittableArea>().healthEntity;

				Vector3 closestPoint = c.ClosestPoint(transform.position);

				Ray ray = new Ray(transform.position, (closestPoint - transform.position).normalized);
				RaycastHit[] hits = Physics.RaycastAll(ray, grenadeData.range);

				bool hitADamageable;
				HittableArea pp;

				RaycastHit? validHit = HealthEntityShoot.DetermineValidHit(hits, new List<Collider>(this.colliders), null, thrower.team.teamId, out hitADamageable, out pp);

				if (!hitADamageable)
				{
					continue;
				}

				float distance = validHit.Value.distance;

				// We already hit this player. Let's see if this collider is closer than the previous.
				if (listOfClosestHittablesWithDst.ContainsKey(he))
				{
					float currentSmallestDst = listOfClosestHittablesWithDst[he];
					if (distance < currentSmallestDst)
					{
						listOfClosestHittablesWithDst[he] = distance;
					}
				}
				else
				{
					listOfClosestHittablesWithDst.Add(he, distance);
				}
			}
		}

		foreach (var kvp in listOfClosestHittablesWithDst)
		{
			HealthEntity he = kvp.Key;
			float dst = kvp.Value;

			float ratio = (grenadeData.range - dst) / grenadeData.range;
			float finalDamage = grenadeData.damageCurve.Evaluate(ratio) * grenadeData.damage;

			HealthEntity.NetworkInfo attacker = new HealthEntity.NetworkInfo(gameObject);

			he.TakeDamage((int)finalDamage, attacker);
		}

		Destroy(gameObject);
	}
}
