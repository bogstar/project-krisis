using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618

public class Bullet : NetworkBehaviour
{
	/*
	new Collider collider;

	short teamId;

	int damage;
	float range;
	Vector3 velocity;

	float dstTraveled;

	private void Start()
	{
		collider = GetComponent<Collider>();
	}

	private void Update ()
	{
		Vector3 moveDelta = velocity * Time.deltaTime;
		float moveDeltaDst = moveDelta.magnitude;

		Ray ray = new Ray(transform.position, velocity.normalized);
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, moveDeltaDst, ~0, QueryTriggerInteraction.Collide))
		{
			if (isServer)
			{
				IDamageable damageable = hit.transform.gameObject.GetComponent<IDamageable>();
				if (damageable != null)
				{
					damageable.TakeDamage(damage);
				}
			}

			if (hit.transform.GetComponent<Forcefield>() != null)
			{
				short ffTeamId = hit.transform.GetComponent<Forcefield>().teamId;
				if (teamId == ffTeamId)
				{
					Physics.IgnoreCollision(hit.collider, collider);
				}
			}
			else
			{
				Destroy(gameObject);
			}
		}

		dstTraveled += moveDeltaDst;
		if (dstTraveled > range)
		{
			Destroy(gameObject);
		}
		transform.position += moveDelta;
	}

	public void Fire(short teamId, int damage, Vector3 velocity, float range)
	{
		this.damage = damage;
		this.velocity = velocity;
		this.range = range;
	}*/
}
