using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>
{
	public GameObject bulletStoneImpactPrefab;
	public GameObject bulletBloodImpactPrefab;
	public GameObject bulletForcefieldImpactPrefab;
	public GameObject explosionPrefab;
	public GameObject pulseExplosionPrefab;

	public void PlayParticle(Vector3 position, Vector3 normal, ParticleType type)
	{
		GameObject prefab = bulletBloodImpactPrefab;
		switch (type)
		{
			case ParticleType.Stone:
				prefab = bulletStoneImpactPrefab;
				break;
			case ParticleType.Explosion:
				prefab = explosionPrefab;
				break;
			case ParticleType.Forcefield:
				prefab = bulletForcefieldImpactPrefab;
				break;
			case ParticleType.PulseExplosion:
				prefab = pulseExplosionPrefab;
				break;
		}

		ParticleEffect particle = Instantiate(prefab).GetComponent<ParticleEffect>();
		particle.transform.position = position;
		Vector3 lookAtDir = position + normal;
		particle.transform.LookAt(lookAtDir);
		particle.StartPlaying();
	}

	public enum ParticleType
	{
		Stone, Blood, Explosion, PulseExplosion, Forcefield
	}
}