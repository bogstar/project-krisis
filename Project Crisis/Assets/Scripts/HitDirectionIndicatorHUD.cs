using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitDirectionIndicatorHUD : MonoBehaviour
{
	Vector3 originalPosition;
	Transform player;
	Image image;
	float innateOffsetDeg;
	float endLife;
	float lifeTime = 2f;
	float startLife;
	int startAlpha = 212;

	private void Awake()
	{
		image = GetComponentInChildren<Image>();
	}

	private void Update()
	{
		if (player == null)
		{
			Destroy(gameObject);
			return;
		}

		float percentage = Mathf.InverseLerp(startLife, endLife, Time.time);
		float alpha = Mathf.Lerp(startAlpha, 0f, percentage);

		if (percentage >= 1f)
		{
			Destroy(gameObject);
			return;
		}
		else
		{
			image.color = new Color(image.color.r, image.color.g, image.color.b, alpha / 255);
		}

		Vector3 playerDir = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
		Vector3 dirToShooter = (originalPosition - player.position).normalized;
		float angle = Vector3.SignedAngle(dirToShooter, playerDir, Vector3.up);
		transform.localEulerAngles = new Vector3(0, 0, angle + innateOffsetDeg);
	}

	public void Show(Transform targetPlayerTransform, Vector3 shooterPosition, float durationMutliplier = 1)
	{
		startLife = Time.time;
		endLife = lifeTime * durationMutliplier + Time.time;
		this.player = targetPlayerTransform;
		originalPosition = shooterPosition;
		innateOffsetDeg = Random.Range(-15f, 15f);

		Vector3 playerDir = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
		Vector3 dirToShooter = (originalPosition - player.position).normalized;
		float angle = Vector3.SignedAngle(dirToShooter, playerDir, Vector3.up);
		transform.localEulerAngles = new Vector3(0, 0, angle + innateOffsetDeg);
	}
}