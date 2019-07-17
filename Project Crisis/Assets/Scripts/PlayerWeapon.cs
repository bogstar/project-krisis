using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerWeapon
{
	public PlayerWeaponScriptableObject weaponData;

	public PlayerWeapon(PlayerWeaponScriptableObject so)
	{
		weaponData = so;
	}
}