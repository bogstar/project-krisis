using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

#pragma warning disable CS0618

public abstract class HealthEntityShoot : NetworkBehaviour
{
	[Header("Health Entity References")]
	[SerializeField]
	protected Transform firePoint;

	[SyncVar]
	protected bool m_isReloading;

	public bool isReloading { get { return m_isReloading; } }

	protected HealthEntity myHealthEntity;

	protected float timeSinceLastFire;

	protected abstract float GetFireRate();
	protected abstract bool LookingDownScope();
	protected abstract float GetReticleSpread();
	protected abstract float GetBulletThickness();
	protected abstract float GetRange();
	protected abstract float GetBulletTrailSpeed();
	protected abstract float GetDamage();
	protected abstract int GetBulletsInClip();
	protected abstract int GetBulletsRemaining();
	protected abstract void DeductOneBullet();
	protected abstract float GetMyMaxMultiplier(float multiplier);
	protected abstract bool CanShoot();
	protected abstract Ray GetRay();

	protected float canvasHeight;

	protected virtual void Start()
	{
		myHealthEntity = GetComponent<HealthEntity>();
		canvasHeight = FindObjectOfType<InGameGUI>().GetComponent<CanvasScaler>().referenceResolution.y;
	}

	protected void Shoot_Base()
	{
		if (!CanShoot())
		{
			return;
		}

		if (Time.time <= timeSinceLastFire)
		{
			return;
		}

		if (GetBulletsInClip() <= 0)
		{
			return;
		}

		timeSinceLastFire = Time.time + GetFireRate();

		Ray ray = GetRay();
		
		RaycastHit[] hits = Physics.SphereCastAll(ray, GetBulletThickness(), GetRange());

		GameObject trailGO = Instantiate(GeneralLibrary.Instance.bulletTrailPrefab);
		BulletTrail trail = trailGO.GetComponent<BulletTrail>();

		AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.Shoot, firePoint.gameObject, .3f, true);

		RaycastHit? validHit = DetermineValidHit(hits, new List<Collider>(), myHealthEntity, myHealthEntity.GetTeamId(), out bool hitADamageable, out HittableArea target);

		// By default we fire in the direction of ray. Everything is set to 0.
		Vector3 finalTargetPoint = ray.origin + ray.direction * GetRange();
		GameObject hitTarget = null;
		float damageMultiplier = 0f;

		float maxBulletSpeed = GetBulletTrailSpeed();
		float minBulletSpeed = 10f;
		float weaponMaxRange = Mathf.Clamp(GetRange(), .1f, float.MaxValue);
		float calculatedBulletSpeed = maxBulletSpeed;

		if (validHit.HasValue)
		{
			float distanceToTarget = validHit.Value.distance;
			float perc = distanceToTarget / GetRange();
			calculatedBulletSpeed = Mathf.Lerp(minBulletSpeed, GetBulletTrailSpeed(), perc);
		}

		// We hit something that can be damaged. We will notify the server.
		if (hitADamageable)
		{
			AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.Hit, firePoint.gameObject, .6f, true);

			finalTargetPoint = validHit.Value.point;
			hitTarget = target.healthEntity.gameObject;
			damageMultiplier = target.damageMultiplier;
			AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Hit, validHit.Value.point, .6f, true);

			// If we're hit, don't spray us with particles.
			// This only works for turrets. Maybe needs redesign?
			if (!hitTarget.GetComponent<HealthEntity>().hasAuthority || !hitTarget.GetComponent<NetworkIdentity>().localPlayerAuthority)
			{
				ParticleManager.ParticleType particleType = target.particleType;
				ParticleManager.Instance.PlayParticle(validHit.Value.point, validHit.Value.normal, particleType);
			}
			else
			{
				HitDirectionIndicatorHUD indicator = Instantiate(InGameGUI.Instance.hitIndicatorPrefab, GameObject.Find("GUI").transform).GetComponent<HitDirectionIndicatorHUD>();
				indicator.Show(hitTarget.transform, transform.position);
			}
		}
		// We hit background. Just inform the server that we hit somewhere.
		else if (validHit.HasValue)
		{
			finalTargetPoint = validHit.Value.point;
			AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Clank, validHit.Value.point, .6f, true);
			ParticleManager.Instance.PlayParticle(validHit.Value.point, validHit.Value.normal, ParticleManager.ParticleType.Stone);
		}

		FireInfo fireInfo = new FireInfo(finalTargetPoint, validHit.HasValue ? validHit.Value.normal : default(Vector3), hitTarget, target == null ? ParticleManager.ParticleType.Stone : target.particleType, GetMyMaxMultiplier(damageMultiplier), GetBulletTrailSpeed(), !validHit.HasValue);

		DeductOneBullet();

		CmdFire(fireInfo);
		trail.SetDestination(firePoint, finalTargetPoint, calculatedBulletSpeed, GetBulletThickness(), myHealthEntity.GetTeamId());

		// We will decrease this amount in Command.
		if (!isServer)
		{
			//m_bulletsInClip--;
		}

		//player.playerMovement.TriggerRecoil(Mathf.Clamp(equippedWeapon.fireRate * 2, 0, .1f));
		//reticleSpread += 10;

		//CmdFire(camera.gameObject.transform.forward, firePoint.position);
	}

	[Command]
	void CmdFire(FireInfo fireInfo)
	{
		if (GetBulletsInClip() <= 0)
		{
			//Debug.LogWarning("PlayerShoot :: CmdFire(GameObjectHit): Player " + connectionToClient.connectionId + " tried to fire without ammo. Possible hack?");

			//m_bulletsInClip = 0;
			return;
		}

		if (!hasAuthority)
		{
			DeductOneBullet();
		}

		if (fireInfo.hitEntityGO != null)
		{
			/*
			target = hit.GetComponent<PlayerHittableArea>();

			if (target == null)
			{
				Debug.LogError("PlayerShoot :: CmdFire(GameObjectHit): Somehow, we have sent a IDamageable-less target to server.");
				return;
			}*/

			// TODO: Maybe somekind of check for hacks?
			HealthEntity.NetworkInfo attacker = new HealthEntity.NetworkInfo(myHealthEntity.gameObject);

			if (MatchManager.Instance.friendlyFire == true || fireInfo.hitEntityGO.GetComponent<HealthEntity>().GetTeamId() != myHealthEntity.GetTeamId())
			{
				fireInfo.hitEntityGO.GetComponent<HealthEntity>().TakeDamage((int)(GetDamage() * fireInfo.damageMultiplier), attacker);
			}
			//target.TakeDamage(weapon.damage);
		}

		RpcFire(fireInfo);
	}

	[ClientRpc]
	protected void RpcFire(FireInfo fireInfo)
	{
		// We don't wanna shoot again if we are the one who fired.
		if ((!localPlayerAuthority && isServer) || hasAuthority)
		{
			return;
		}

		DeductOneBullet();

		// We hit something that can be damaged. We will notify the server.
		if (fireInfo.hitEntityGO != null)
		{
			AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Hit, fireInfo.hitPoint, .6f, true);

			// If we're hit, don't spray us with particles.
			if (!fireInfo.hitEntityGO.GetComponent<HealthEntity>().hasAuthority || !fireInfo.hitEntityGO.GetComponent<NetworkIdentity>().localPlayerAuthority)
			{
				ParticleManager.ParticleType particleType = fireInfo.particleType;
				ParticleManager.Instance.PlayParticle(fireInfo.hitPoint, fireInfo.hitNormal, particleType);
			}
			else
			{
				HitDirectionIndicatorHUD indicator = Instantiate(InGameGUI.Instance.hitIndicatorPrefab, GameObject.Find("GUI").transform).GetComponent<HitDirectionIndicatorHUD>();
				indicator.Show(fireInfo.hitEntityGO.transform, transform.position);
			}
		}
		// We hit background. Just inform the server that we hit somewhere.
		else if (!fireInfo.noHit)
		{
			AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Clank, fireInfo.hitPoint, .6f, true);
			ParticleManager.Instance.PlayParticle(fireInfo.hitPoint, fireInfo.hitNormal, ParticleManager.ParticleType.Stone);
		}

		AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.Shoot, firePoint.gameObject, .3f, true);

		float distanceToTarget = Vector3.Distance(firePoint.position, fireInfo.hitPoint);

		float perc = distanceToTarget / GetRange();
		float calculatedBulletSpeed = Mathf.Lerp(10f, GetBulletTrailSpeed(), perc);

		GameObject trailGO = Instantiate(GeneralLibrary.Instance.bulletTrailPrefab);
		BulletTrail trail = trailGO.GetComponent<BulletTrail>();

		trail.SetDestination(firePoint, fireInfo.hitPoint, calculatedBulletSpeed, GetBulletThickness(), myHealthEntity.GetTeamId());
	}

	public static RaycastHit? DetermineValidHit(RaycastHit[] hits, List<Collider> collidersToIgnore, HealthEntity healthEntity, short teamId, out bool hitADamageable, out HittableArea target)
	{
		hitADamageable = false;
		RaycastHit? validHit = null;

		target = null;

		if (hits.Length > 0)
		{
			System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
		}

		foreach (var hit in hits)
		{
			Collider collider = hit.collider;
			target = collider.GetComponent<HittableArea>();

			// Let's see if we hit a player. eg, ourselves first!
			if (target != null)
			{
				// Did we hit ourselves?
				if (healthEntity != null && target.healthEntity == healthEntity)
				{
					continue;
				}

				// Did we hit our own forcefield?
				if (target.healthEntity is Forcefield && ((Forcefield)target.healthEntity).teamId == teamId)
				{
					continue;
				}

				// We hit another player then.
				hitADamageable = true;
				validHit = hit;
				break;
			}

			// We need to ignore some colliders.
			if (collidersToIgnore.Contains(collider))
			{
				continue;
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

			// Did we hit our own forcefield?
			if (hit.collider.GetComponentInParent<Forcefield>() != null && hit.collider.GetComponentInParent<Forcefield>().teamId == teamId)
			{
				continue;
			}

			// We hit a background object.
			validHit = hit;
			break;
		}

		return validHit;
	}

	public struct FireInfo
	{
		public Vector3 hitPoint;
		public Vector3 hitNormal;
		public GameObject hitEntityGO;
		public bool noHit;
		public ParticleManager.ParticleType particleType;
		public float damageMultiplier;
		public float bulletSpeed;

		public FireInfo(Vector3 hitPoint, Vector3 hitNormal, GameObject hitEntityGO, ParticleManager.ParticleType particleType, float damageMultiplier, float bulletSpeed, bool noHit)
		{
			this.hitPoint = hitPoint;
			this.hitNormal = hitNormal;
			this.hitEntityGO = hitEntityGO;
			this.particleType = particleType;
			this.damageMultiplier = damageMultiplier;
			this.bulletSpeed = bulletSpeed;
			this.noHit = noHit;
		}
	}
}