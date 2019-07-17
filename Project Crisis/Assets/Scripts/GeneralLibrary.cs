using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralLibrary : Singleton<GeneralLibrary>
{
	[Header("Generic Prefabs")]
	public GameObject grenadePrefab;
	public GameObject playerPrefab;
	public GameObject bulletTrailPrefab;

	[Header("Database")]
	[SerializeField] PlayerWeaponScriptableObject[] weapons;
	[SerializeField] LevelScriptableObject[] levels;
	[SerializeField] GrenadeScriptableObject[] grenades;
	[SerializeField] CharacterScriptableObject[] characters;
	
	[SerializeField] PickupGameObject[] pickups;


	public T GetItem<T>(string id) where T : DatabaseEntryBase
	{
		if (id == null || id.Trim() == "")
		{
			return null;
		}

		DatabaseEntryBase[] itemArray = null;

		switch (ScriptableObject.CreateInstance<T>())
		{
			case PlayerWeaponScriptableObject w:
				itemArray = weapons;
				break;
			case LevelScriptableObject l:
				itemArray = levels;
				break;
			case GrenadeScriptableObject g:
				itemArray = grenades;
				break;
			case CharacterScriptableObject c:
				itemArray = characters;
				break;
		}

		if (itemArray == null)
		{
			Debug.LogError("Invalid item type" + typeof(T).ToString());
			return null;
		}

		foreach (var item in itemArray)
		{
			if (item.id == id)
			{
				return Instantiate(item) as T;
			}
		}

		Debug.LogError("No " + typeof(T).ToString() + " with id " + id + " found.");
		return null;
	}

	public T[] GetAllItems<T>() where T : DatabaseEntryBase
	{
		DatabaseEntryBase[] itemArray = null;

		switch (ScriptableObject.CreateInstance<T>())
		{
			case PlayerWeaponScriptableObject w:
				itemArray = weapons;
				break;
			case LevelScriptableObject l:
				itemArray = levels;
				break;
			case GrenadeScriptableObject g:
				itemArray = grenades;
				break;
			case CharacterScriptableObject c:
				itemArray = characters;
				break;
		}

		if (itemArray == null)
		{
			Debug.LogError("Invalid item type" + typeof(T).ToString());
			return null;
		}

		return itemArray.Clone() as T[];
	}

	public GameObject[] GetPickupPrefabs(Pickup.PickupType pt)
	{
		foreach (var p in pickups)
		{
			if (p.pickupType == pt)
			{
				return p.gameObjects;
			}
		}

		Debug.LogError("No pickup with id " + pt  + " found.");
		return null;
	}

	[System.Serializable]
	public struct PickupGameObject
	{
		public Pickup.PickupType pickupType;
		public GameObject[] gameObjects;
	}
}