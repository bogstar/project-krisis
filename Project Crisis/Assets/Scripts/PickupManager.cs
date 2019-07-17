using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PickupManager : NetworkBehaviour
{
	[Header("Prefabs")]
	[SerializeField]
	GameObject pickupPrefab;

	[Header("References")]
	[SerializeField]
	Transform pickupLocs;

	[Header("Variables")]
	[SerializeField]
	float spawnCD;

	float lastSpawnTime;
	Dictionary<Pickup, Vector3> activePickups = new Dictionary<Pickup, Vector3>();
	List<Vector3> emptyPickupLocs = new List<Vector3>();


	public override void OnStartServer()
	{
		foreach (Transform child in pickupLocs)
		{
			emptyPickupLocs.Add(child.position);
		}

		normalCd = spawnCD;
		spawnCD = 0;
		lastSpawnTime = Time.time + spawnCD;
	}

	private void OnEnable()
	{
		StartCoroutine(SetToNormal());
	}

	float normalCd;

	private void Update()
	{
		if (!isServer)
		{
			return;
		}

		if (Time.time > lastSpawnTime)
		{
			SpawnPickup();
		}
	}

	IEnumerator SetToNormal()
	{
		yield return new WaitForSeconds(1);
		spawnCD = normalCd;
	}

	void SpawnPickup()
	{
		List<Vector3> availableLocs = new List<Vector3>(emptyPickupLocs);

		for (int i = availableLocs.Count - 1; i > -1; i--)
		{
			Collider[] colliders = Physics.OverlapSphere(availableLocs[i], 5f);
			foreach (var c in colliders)
			{
				if (c.GetComponent<HittableArea>() != null)
				{
					availableLocs.RemoveAt(i);
					break;
				}
			}
		}

		if (availableLocs.Count < 1)
		{
			lastSpawnTime = Time.time + spawnCD;
			return;
		}

		Vector3 pickedLoc = availableLocs[Random.Range(0, availableLocs.Count)];
		emptyPickupLocs.Remove(pickedLoc);

		CmdSpawnPickup(pickedLoc);
	}

	void OnPickup(Pickup pickup)
	{
		if (activePickups.ContainsKey(pickup))
		{
			Vector3 pickupLocation = activePickups[pickup];
			emptyPickupLocs.Add(pickupLocation);
		}
	}

	[Command]
	void CmdSpawnPickup(Vector3 location)
	{
		lastSpawnTime = Time.time + spawnCD;

		Pickup.PickupType pickupType = (Pickup.PickupType)Random.Range(0, System.Enum.GetValues(typeof(Pickup.PickupType)).Length);

		Pickup pickup = Instantiate(pickupPrefab).GetComponent<Pickup>();
		string weaponId = "";
		if(pickupType == Pickup.PickupType.Weapon)
		{
			int randomWeapon = Random.Range(0, 2);
			switch (randomWeapon)
			{
				case 0:
					weaponId = "assault";
					break;
				case 1:
					weaponId = "sniper";
					break;
			}
		}
		pickup.Setup(pickupType, weaponId, OnPickup);
		NetworkServer.Spawn(pickup.gameObject);

		activePickups.Add(pickup, location);
		RpcSpawnPickup(pickup.gameObject, location);
	}

	[ClientRpc]
	void RpcSpawnPickup(GameObject pickupGO, Vector3 position)
	{
		Pickup pickup = pickupGO.GetComponent<Pickup>();
		pickup.transform.position = position;
		pickup.transform.SetParent(transform);
	}
}