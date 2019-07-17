using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TurretShoot : HealthEntityShoot
{
	[Header("Turret References")]
	[SerializeField]
	Transform bodyRotator;
	[SerializeField]
	Transform cameraPivot;
	[SerializeField]
	Transform shootPointCalculator;

	[Header("Turret Variables")]
	[SerializeField]
	protected float range;
	[SerializeField]
	protected float fireRate;
	[SerializeField]
	protected float damage;
	[SerializeField]
	protected float spread;

	Transform target;


	private void Update()
	{
		LookAtTarget();

		if (isServer)
		{
			CmdRaycastForTarget();
		}
	}

	[Command]
	void CmdRaycastForTarget()
	{
		Collider[] colliders = Physics.OverlapSphere(transform.position, GetRange());

		if (colliders.Length < 1)
		{
			this.target = null;
		}

		foreach (var c in colliders)
		{
			if (c.GetComponent<Player>() != null && c.GetComponent<Player>().GetTeamId() != myHealthEntity.GetTeamId() && c.GetComponent<Player>().isAlive)
			{
				Ray ray = new Ray(shootPointCalculator.position, (c.transform.position + new Vector3(0, 1.5f, 0)) - shootPointCalculator.position);
				Debug.DrawRay(ray.origin, ray.direction * GetRange());
				RaycastHit[] hits = Physics.RaycastAll(ray, GetRange());

				bool hitADamageable = false;
				RaycastHit? validHit = null;

				HittableArea target = null;

				if (hits.Length > 0)
				{
					System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
				}

				foreach (var hit in hits)
				{
					// Let's see if we hit ourselves.
					if (hit.collider.transform.name == "Fire Point")
					{
						continue;
					}

					target = hit.collider.GetComponent<HittableArea>();

					// Let's see if we hit a player.
					if (target != null)
					{
						hitADamageable = true;
						validHit = hit;
						break;
					}

					// Did we hit a trigger?
					if (hit.collider.isTrigger)
					{
						continue;
					}

					// Did we hit a character controller?
					if (hit.collider is CharacterController)
					{
						continue;
					}

					// Did we hit friendly a forcefield forcefield?
					if (hit.transform.GetComponent<Forcefield>() != null && hit.transform.GetComponent<Forcefield>().teamId == myHealthEntity.GetTeamId())
					{
						continue;
					}

					// We hit a background object.
					validHit = hit;
					break;
				}

				// We can see an enemy!
				if (hitADamageable)
				{
					this.target = validHit.Value.transform;
					Shoot_Base();

					break;
				}
			}
			else
			{
				this.target = null;
			}
		}
	}

	void RpcUpdateTarget(GameObject target)
	{
		SetTarget(target.transform);
	}

	void LookAtTarget()
	{
		if (target == null)
		{
			return;
		}

		bodyRotator.LookAt(new Vector3(target.position.x, bodyRotator.position.y, target.position.z));
		cameraPivot.LookAt(target.position + new Vector3(0, 1.5f, 0));
	}

	void SetTarget(Transform target)
	{
		this.target = target;
	}

	void UnsetTarget()
	{
		target = null;
		bodyRotator.LookAt(Vector3.zero);
		cameraPivot.LookAt(Vector3.zero);
	}

	protected override bool CanShoot()
	{
		return true;
	}

	protected override float GetFireRate()
	{
		return fireRate;
	}

	protected override bool LookingDownScope()
	{
		return false;
	}

	protected override float GetReticleSpread()
	{
		return spread;
	}

	protected override float GetBulletThickness()
	{
		return .01f;
	}

	protected override float GetRange()
	{
		return range;
	}

	protected override float GetBulletTrailSpeed()
	{
		return 1000f;
	}

	protected override float GetDamage()
	{
		return damage;
	}

	protected override int GetBulletsInClip()
	{
		return 1;
	}

	protected override float GetMyMaxMultiplier(float multiplier)
	{
		return Mathf.Clamp(multiplier, multiplier, 1f);
	}

	protected override Ray GetRay()
	{
		float variation = GetReticleSpread() / 1000f;

		float xRand = Random.Range(-variation, variation);
		float yRand = Random.Range(-variation, variation);

		Vector3 point = new Vector3(xRand, yRand, 0.4f);

		GameObject pointGO = new GameObject();
		pointGO.transform.SetParent(shootPointCalculator);
		pointGO.transform.localPosition = point;

		Ray ray = new Ray(shootPointCalculator.position, pointGO.transform.position - shootPointCalculator.position);

		Destroy(pointGO);

		return ray;
	}

	protected override int GetBulletsRemaining()
	{
		return 1;
	}

	protected override void DeductOneBullet()
	{
		return;
	}
}