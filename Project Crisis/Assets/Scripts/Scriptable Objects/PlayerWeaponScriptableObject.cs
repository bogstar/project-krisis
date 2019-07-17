using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerWeaponSO", menuName = "Crisis/Weapon")]
public class PlayerWeaponScriptableObject : DatabaseEntryBase
{
	public new string name;
	public GameObject model;
	public FireType fireType;
	public float damage;
	public int bulletsPerClip;
	public int maxClips;
	public float zoomWhenDownScoping;

	public float range;

	public float yScopeValue;
	public float fireRate;
	public float bulletTrailSpeed;
	public float bulletThickness;
	public int reticleSpread;
	
	public float reloadTime;

	public enum FireType
	{
		Automatic, SemiAutomatic, Shotgun
	}
}