using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGrenadeSO", menuName = "Crisis/Grenade")]
public class GrenadeScriptableObject : DatabaseEntryBase
{
	public new string name;
	public GameObject model;

	public float damage;
	public float range;
	public AnimationCurve damageCurve;

	public float maxTimer;
	public float minTimer;

	public AudioLibrary.AudioOccasion audioClip;
	public ParticleManager.ParticleType explosionParticle;
}