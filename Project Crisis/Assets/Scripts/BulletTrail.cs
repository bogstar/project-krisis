using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
	Vector3 direction;
	float speed;
	float thickness;
	float lifetimeCounter = 0;

	public float lifetime;
	public float stickyTime;

	new LineRenderer renderer;

	Vector3 frontPoint;
	Vector3 aftPoint;

	bool isAttached;

	bool hasHit;

	Transform firePoint;

	short teamId;

	private void Start()
	{
		renderer = GetComponent<LineRenderer>();
		renderer.positionCount = 2;
	}

	private void Update()
	{
		if (lifetimeCounter > lifetime)
		{
			Destroy(gameObject);
		}

		float moveDelta = speed * Time.deltaTime;

		if (!hasHit)
		{
			Ray ray = new Ray(frontPoint, direction);
			RaycastHit hit;

			if (Physics.SphereCast(ray, thickness, out hit, moveDelta))
			{
				if (hit.transform.GetComponentInParent<Forcefield>() == null || hit.transform.GetComponentInParent<Forcefield>().teamId != teamId)
				{
					frontPoint = hit.point;
					hasHit = true;
				}
				else
				{
					frontPoint += direction * moveDelta;
				}
			}
			else
			{
				frontPoint += direction * moveDelta;
			}
		}

		if (isAttached && lifetimeCounter > stickyTime || firePoint == null)
		{
			isAttached = false;
		}

		if (isAttached)
		{
			aftPoint = firePoint.position;
		}
		else
		{
			Vector3 aftDir = (frontPoint - aftPoint).normalized;
			if (moveDelta > Vector3.Distance(aftPoint, frontPoint))
			{
				aftPoint = frontPoint;
				lifetimeCounter = lifetime;
			}
			else
			{
				aftPoint += aftDir * moveDelta;
			}
		}

		renderer.SetPosition(0, frontPoint);
		renderer.SetPosition(1, aftPoint);

		lifetimeCounter += Time.deltaTime;
	}

	public void SetDestination(Transform firePoint, Vector3 destination, float speed, float thickness, short teamId)
	{
		lifetimeCounter = 0;
		this.firePoint = firePoint;
		aftPoint = firePoint.position;
		frontPoint = firePoint.position;
		isAttached = true;
		hasHit = false;
		direction = (destination - firePoint.position).normalized;
		this.speed = speed;
		this.thickness = thickness;
		this.teamId = teamId;
	}
}