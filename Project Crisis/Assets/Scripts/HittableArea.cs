using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HittableArea : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	Collider[] m_colliders;
	[SerializeField]
	ParticleManager.ParticleType m_particleType;

	public Collider[] colliders { get { return m_colliders; } }
	public ParticleManager.ParticleType particleType { get { return m_particleType; } }

	public HealthEntity healthEntity { get; private set; }
	public float damageMultiplier { get { return m_damageMultiplier; } }

	[SerializeField]
	float m_damageMultiplier;
	
	public void SetHealthEntity(HealthEntity he)
	{
		healthEntity = he;
	}

	public void SetCollidersToTriggers(bool set)
	{
		foreach (var col in m_colliders)
		{
			col.isTrigger = set;
		}
	}

	public void EnableHittables(bool enable)
	{
		foreach (var col in m_colliders)
		{
			col.enabled = enable;
		}
	}
}
