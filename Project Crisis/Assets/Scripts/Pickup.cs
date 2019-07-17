using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

#pragma warning disable CS0618

public class Pickup : NetworkBehaviour
{
	[Header("References")]
	[SerializeField]
	Transform graphicsTransform;

	[Header("Variables")]
	[SerializeField]
	float rotationSpeed = 20f;

	[SyncVar (hook = "OnPickupType")]
	PickupType pickupType;
	[SyncVar]
	string weaponId;

	System.Action<Pickup> OnPickUp;


	void Update()
	{
		transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + rotationSpeed * Time.deltaTime, 0f);
	}

	private void OnTriggerEnter(Collider other)
	{
		Player p = other.GetComponent<Player>();

		if (p == null)
		{
			return;
		}

		switch (pickupType)
		{
			case PickupType.Weapon:
				AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.WeaponRefill, transform.position, .6f, false);
				break;
			case PickupType.Health:
				AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.HealthRefill, transform.position, .6f, false);
				break;
		}
		Destroy(gameObject);

		if (isServer)
		{
			CmdOnPlayerEnter(p.gameObject);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();

		OnPickupType(pickupType);
	}

	public void Setup(PickupType type, string weaponId, System.Action<Pickup> onPickUpCb)
	{
		this.weaponId = weaponId;
		this.pickupType = type;
		this.OnPickUp = onPickUpCb;
	}

	[Command]
	void CmdOnPlayerEnter(GameObject pGO)
	{
		Player p = pGO.GetComponent<Player>();

		if (p == null)
		{
			return;
		}

		switch (pickupType)
		{
			case PickupType.Ammo:
				p.GetComponent<PlayerShoot>().CmdRefillAmmo();
				break;
			case PickupType.Health:
				p.RefillHealth();
				break;
			case PickupType.Weapon:
				p.GetComponent<PlayerShoot>().AddWeaponPublic(weaponId);
				break;
			case PickupType.Grenade:
				p.GetComponent<PlayerShoot>().CmdRefillGrenades();
				break;
		}

		OnPickUp?.Invoke(this);
		RpcOnPlayerEnter(pGO);
	}

	[ClientRpc]
	void RpcOnPlayerEnter(GameObject pGO)
	{
		if (pGO.GetComponent<Player>().hasAuthority)
		{
			return;
		}
		switch (pickupType)
		{
			case PickupType.Weapon:
				AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.WeaponRefill, transform.position, .6f, false);
				break;
			case PickupType.Health:
				AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.HealthRefill, transform.position, .6f, false);
				break;
		}
		Destroy(gameObject);
	}

	// ---------------------- hook ----------
	void OnPickupType(PickupType type)
	{
		for (int i = graphicsTransform.childCount - 1; i > -1; i--)
		{
			Destroy(graphicsTransform.GetChild(i).gameObject);
		}

		switch (pickupType)
		{
			case PickupType.Weapon:
				switch (weaponId)
				{
					case "assault":
						Instantiate(GeneralLibrary.Instance.GetPickupPrefabs(type)[0], graphicsTransform);
						break;
					case "sniper":
						Instantiate(GeneralLibrary.Instance.GetPickupPrefabs(type)[1], graphicsTransform);
						break;
				}
				
				break;
			default:
				Instantiate(GeneralLibrary.Instance.GetPickupPrefabs(type)[0], graphicsTransform);
				break;
		}

		this.pickupType = type;
	}

	public enum PickupType
	{
		Weapon, Ammo, Health, Grenade
	}
}
