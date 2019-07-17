using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEffect : MonoBehaviour
{
	new ParticleSystem particleSystem;

	private void Awake()
	{
		particleSystem = GetComponent<ParticleSystem>();
	}

	public void StartPlaying()
	{
		particleSystem.Play();
		StartCoroutine(IsParticleDonePlaying());
	}

	IEnumerator IsParticleDonePlaying()
	{
		while (particleSystem.IsAlive())
		{
			yield return new WaitForSeconds(.15f);
		}

		Destroy(gameObject);
	}
}