using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Player))]
public class PlayerShoot : HealthEntityShoot
{
	[Header("Player References")]
	[SerializeField]
	Transform weaponCarryPoint;
	[SerializeField]
	Transform scopeViewPoint;
	[SerializeField]
	Transform carryWeaponPoint;

	public PlayerWeaponScriptableObject equippedWeapon
	{
		get
		{
			if (weaponIndex < 0 || weaponIndex >= weapons.Length || weapons[weaponIndex].id == null || weapons[weaponIndex].id == "" || GetWeapon(weapons[weaponIndex].id) == null)
				return null;
			return GetWeapon(weapons[weaponIndex].id);
		}
	}

	public WeaponInfo[] weapons;
	[SyncVar(hook = "OnMyWeaponIndex")]
	public int weaponIndex;

	public GrenadeInfo[] grenades;
	[SyncVar]
	public int grenadeIndex;

	//[SyncVar (hook = "OnMyEquippedWeaponIndex")]
	//public int equippedWeaponIndex = -1;

	new Camera camera { get { return player.camera; } }

	float timeSinceLastGrenade;

	public float reticleSpread;

	bool chargingUpGrenade;

	Player player
	{
		get
		{
			if (m_player == null)
			{
				m_player = GetComponent<Player>();
			}
			return m_player;
		}
	}
	Player m_player;
	
	[System.Serializable]
	public struct GrenadeInfo
	{
		public string id;
		public int count;
	}

	[System.Serializable]
	public struct WeaponInfo
	{
		public string id;
		public int bulletsInClip;
		public int bulletsRemaining;
	}

	protected override void Start()
	{
		base.Start();

		if (isServer)
		{
			CmdAddStartingWeapons();
		}

		player.RegisterDeathCallback(OnDeath);
		targetFov = 60;
	}

	static PlayerWeaponScriptableObject GetWeapon(string id)
	{
		return GeneralLibrary.Instance.GetItem<PlayerWeaponScriptableObject>(id);
	}

	[Command]
	void CmdAddStartingWeapons()
	{
		grenades = new GrenadeInfo[player.characterData.grenadeSlots];
		grenades[0].id = "frag";
		grenades[0].count = 0;
		grenades[1].id = "pulse";
		grenades[1].count = 0;

		weapons = new WeaponInfo[player.characterData.weaponSlots];
		RpcAddStartingWeapons(grenades, weapons);

		RpcAddWeapon("pistol", 0);
	}

	[ClientRpc]
	void RpcAddStartingWeapons(GrenadeInfo[] grenadeInfo, WeaponInfo[] weaponInfo)
	{
		grenades = grenadeInfo;
		weapons = weaponInfo;
	}

	public void SpreadReticle(float amount)
	{
		if (equippedWeapon != null)
		{
			reticleSpread += 20f * amount;
			reticleSpread = Mathf.Clamp(reticleSpread, float.MinValue, equippedWeapon.reticleSpread * 2.5f);
		}
	}

	void OnDeath(HealthEntity.NetworkInfo attacker)
	{
		player.UnregisterDeathCallback(OnDeath);
		UnequipWeapon();
		weapons = new WeaponInfo[0];
		if (FindObjectOfType<TargettingReticle>() != null)
		{
			FindObjectOfType<TargettingReticle>().Show(false);
		}
	}

	public float reticleShrinkSpeed = 10f;
	float reticleTargetSize;

	void HandleReticleSpread()
	{
		if (equippedWeapon == null)
		{
			return;
		}

		if (!player.playerMovement.GetComponent<CharacterController>().isGrounded)
		{
			reticleTargetSize = int.MaxValue;
		}
		else if (Mathf.Abs(player.playerMovement.moveDir.x) > 0 || Mathf.Abs(player.playerMovement.moveDir.x) > 0)
		{
			reticleTargetSize = equippedWeapon.reticleSpread * 2.5f;
		}
		else
		{
			reticleTargetSize = equippedWeapon.reticleSpread;
		}

		float sign = Mathf.Sign(reticleTargetSize - reticleSpread);

		float reticleDelta = sign * Time.deltaTime * reticleShrinkSpeed;
		reticleSpread += reticleDelta;
		reticleSpread = Mathf.Clamp(reticleSpread, equippedWeapon.reticleSpread * .25f, equippedWeapon.reticleSpread * 3f);

		InGameGUI.Instance.targetingReticle.Refresh();
	}

	[SyncVar]
	bool m_lookingDownScope;

	public bool lookingDownScope { get { return m_lookingDownScope; } }
	Vector3 localScopeViewPoint;

	private void Update()
	{
		if (!hasAuthority)
		{
			return;
		}

		if (!player.isAlive)
		{
			return;
		}

		HandleReticleSpread();

		if (InGameGUI.Instance.lockCamera)
		{
			return;
		}

		bool check = false;

		if (equippedWeapon != null)
		{
			if (equippedWeapon.fireType == PlayerWeaponScriptableObject.FireType.Automatic)
			{
				check = Input.GetMouseButton(0);
			}
			else
			{
				check = Input.GetMouseButtonDown(0);
			}

			if (Input.GetMouseButton(1))
			{
				if (!m_lookingDownScope && !player.playerMovement.jumping)
				{
					InGameGUI.Instance.targetingReticle.Show(false);
					m_lookingDownScope = true;
					targetFov = 60 / equippedWeapon.zoomWhenDownScoping;
					CmdLookDownScope(true);
				}
			}
			else
			{
				if (m_lookingDownScope)
				{
					InGameGUI.Instance.targetingReticle.Show(true);
					m_lookingDownScope = false;
					targetFov = 60;
					CmdLookDownScope(false);
				}
			}

			if (check)
			{
				FireLocally();
			}
		}

		if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
		{
			ChangeWeapon(Input.mouseScrollDelta.y > 0 ? true : false);
		}

		if (chargingUpGrenade)
		{
			grenadeThrowMultiplier += Time.deltaTime;
			grenadeThrowMultiplier = Mathf.Clamp(grenadeThrowMultiplier, .75f, 3f);
		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			grenadeIndex = 0;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			grenadeIndex = 1;
		}

		if (Input.GetKeyDown(KeyCode.F) && CanChargeUpGrenade())
		{
			chargingUpGrenade = true;
		}
		else if (Input.GetKeyUp(KeyCode.F) && CanThrowGrenade())
		{
			ThrowGrenadeLocally();
			chargingUpGrenade = false;
			grenadeThrowMultiplier = 0;
		}
		else if (!Input.GetKey(KeyCode.F))
		{
			chargingUpGrenade = false;
		}

		if (Input.GetKeyDown(KeyCode.R) && CanReload() && !m_isReloading && GetBulletsInClip() < equippedWeapon.bulletsPerClip)
		{
			ReloadLocally();
		}
	}

	private void FixedUpdate()
	{
		if (m_lookingDownScope && CanLookDownScope())
		{
			Vector3 localScopeViewPoint = camera.transform.InverseTransformPoint(scopeViewPoint.position);
			localScopeViewPoint = new Vector3(localScopeViewPoint.x, equippedWeapon.yScopeValue, localScopeViewPoint.z);
			weaponCarryPoint.position = Vector3.SmoothDamp(weaponCarryPoint.position, camera.transform.TransformPoint(localScopeViewPoint), ref weaponVelocity, smooth / 20);
		}
		else
		{
			weaponCarryPoint.position = Vector3.SmoothDamp(weaponCarryPoint.position, carryWeaponPoint.position, ref weaponVelocity, smooth / 20);
		}

		camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, targetFov, ref zoomVelocity, (smooth * 2) / 20);
	}

	public float targetFov = 60;
	public float smooth;
	Vector3 weaponVelocity;
	float zoomVelocity;
	float grenadeThrowMultiplier;

	[Command]
	void CmdLookDownScope(bool look)
	{
		m_lookingDownScope = look;
	}

	void ChangeWeapon(bool increasing)
	{
		if (hasAuthority)
		{
			if (!isServer)
			{
				m_lookingDownScope = false;

				if (reloadingCoroutine != null)
				{
					m_isReloading = false;
					StopCoroutine(reloadingCoroutine);
				}

				if (increasing)
				{
					weaponIndex++;
					weaponIndex %= player.characterData.weaponSlots;
				}
				else
				{
					weaponIndex--;
					if (weaponIndex < 0)
						weaponIndex = player.characterData.weaponSlots - 1;
				}
			}

			EquipWeapon(weaponIndex);
			CmdChangeWeapon(increasing);
		}
	}

	[Command]
	void CmdChangeWeapon(bool increasing)
	{
		m_lookingDownScope = false;

		if (increasing)
		{
			weaponIndex++;
			weaponIndex %= player.characterData.weaponSlots;
		}
		else
		{
			weaponIndex--;
			if (weaponIndex < 0)
				weaponIndex = player.characterData.weaponSlots - 1;
		}

		if (reloadingCoroutine != null)
		{
			m_isReloading = false;
			StopCoroutine(reloadingCoroutine);
		}
	}

	Coroutine reloadingCoroutine;

	void SetFirePointForWeapon()
	{
		if (equippedWeapon == null)
		{
			firePoint = transform;
		}
		else
		{
			firePoint = Instantiate(equippedWeapon.model, weaponCarryPoint).GetComponent<WeaponModelSetup>().firePoint;
		}
		
		if (hasAuthority)
		{
			InGameGUI.Instance.targetingReticle.Show(true);
		}
	}

	void EquipWeapon(int index)
	{
		UnequipWeapon();

		if (index > -1)
		{
			SetFirePointForWeapon();
		}
		else
		{
			firePoint = transform;
		}
	}

	public void UnequipWeapon()
	{
		if (firePoint == null)
		{
			return;
		}

		if (hasAuthority)
		{
			InGameGUI.Instance.targetingReticle.Show(false);
		}

		if (weaponCarryPoint.childCount > 0)
		{
			for (int i = weaponCarryPoint.childCount - 1; i > -1; i--)
			{
				Destroy(weaponCarryPoint.GetChild(i).gameObject);
			}
		}
	}

	public void AddWeaponPublic(string weaponId)
	{
		CmdAddWeapon(weaponId, -1);
	}

	[Command]
	void CmdAddWeapon(string weaponId, int index)
	{
		RpcAddWeapon(weaponId, index);
	}

	[ClientRpc]
	void RpcAddWeapon(string weaponId, int index)
	{
		AddWeapon(weaponId, index);
	}

	/// <summary>
	/// Add weapon to player.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="index"></param>
	void AddWeapon(string id, int index = -1)
	{
		int realIndex = weaponIndex;

		if (index < 0 || index > weapons.Length - 1)
		{
			for (int i = 0; i < player.characterData.weaponSlots; i++)
			{
				if (weapons[i].id == null || weapons[i].id == "")
				{
					realIndex = i;
					break;
				}
			}
		}

		PlayerWeaponScriptableObject weapon = GetWeapon(id);

		weapons[realIndex].id = id;
		weapons[realIndex].bulletsRemaining = weapon.bulletsPerClip * (weapon.maxClips - 1);
		weapons[realIndex].bulletsInClip = weapon.bulletsPerClip;

		EquipWeapon(realIndex);
		OnMyWeaponIndex(realIndex);
	}

	void ThrowGrenadeLocally()
	{
		if (Time.time <= timeSinceLastGrenade)
		{
			return;
		}

		if (grenades[grenadeIndex].count <= 0)
		{
			return;
		}

		Vector3 throwForce = camera.gameObject.transform.forward * 10f * grenadeThrowMultiplier;
		Vector3 throwTorque = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
		float throwStrength = Mathf.InverseLerp(.75f, 3f, grenadeThrowMultiplier);

		GrenadeThrowInfo grenadeThrowInfo = new GrenadeThrowInfo();
		grenadeThrowInfo.throwPosition = firePoint.position;
		grenadeThrowInfo.throwForce = throwForce;
		grenadeThrowInfo.throwTorque = throwTorque;
		grenadeThrowInfo.grenadeThrowStrength = throwStrength;

		if (!isServer)
		{
			GameObject grenadeGO = Instantiate(GeneralLibrary.Instance.grenadePrefab);
			Grenade grenade = grenadeGO.GetComponent<Grenade>();
			grenade.Init(GeneralLibrary.Instance.GetItem<GrenadeScriptableObject>(grenades[grenadeIndex].id));
			grenade.transform.position = firePoint.position;
			grenade.rigidbody.AddForce(throwForce, ForceMode.Impulse);
			grenade.rigidbody.AddTorque(throwTorque, ForceMode.Impulse);
			grenade.Arm(player, throwStrength);

			grenades[grenadeIndex].count--;
			timeSinceLastGrenade = Time.time + 1f;
		}

		CmdThrowGrenade(grenadeThrowInfo);
	}

	public struct GrenadeThrowInfo
	{
		public GameObject grenadeGO;
		public Vector3 throwPosition;
		public Vector3 throwForce;
		public Vector3 throwTorque;
		public float grenadeThrowStrength;
		public float grenadeTimestamp;
	}

	[Command]
	void CmdThrowGrenade(GrenadeThrowInfo grenadeThrowInfo)
	{
		if (Time.time <= timeSinceLastGrenade)
		{
			return;
		}

		if (grenades[grenadeIndex].count <= 0)
		{
			return;
		}

		GameObject grenadeGO = Instantiate(GeneralLibrary.Instance.grenadePrefab);
		grenadeGO.GetComponent<NetworkIdentity>().serverOnly = true;
		Grenade grenade = grenadeGO.GetComponent<Grenade>();
		grenade.Init(GeneralLibrary.Instance.GetItem<GrenadeScriptableObject>(grenades[grenadeIndex].id));
		grenade.transform.position = grenadeThrowInfo.throwPosition;
		grenade.rigidbody.AddForce(grenadeThrowInfo.throwForce, ForceMode.Impulse);
		grenade.rigidbody.AddTorque(grenadeThrowInfo.throwTorque, ForceMode.Impulse);

		grenadeThrowInfo.grenadeTimestamp = grenade.Arm(player, grenadeThrowInfo.grenadeThrowStrength);

		NetworkServer.Spawn(grenadeGO);

		//grenadeThrowInfo.grenadeGO = grenadeGO;
		grenades[grenadeIndex].count--;
		timeSinceLastGrenade = Time.time + 1f;
		RpcThrowGrenade(grenadeThrowInfo);
	}

	[ClientRpc]
	void RpcThrowGrenade(GrenadeThrowInfo grenadeThrowInfo)
	{
		if (isServer || hasAuthority)
		{
			return;
		}

		GameObject grenadeGO = Instantiate(GeneralLibrary.Instance.grenadePrefab);
		Grenade grenade = grenadeGO.GetComponent<Grenade>();
		grenade.Init(GeneralLibrary.Instance.GetItem<GrenadeScriptableObject>(grenades[grenadeIndex].id));
		grenade.transform.position = grenadeThrowInfo.throwPosition;
		grenade.rigidbody.AddForce(grenadeThrowInfo.throwForce, ForceMode.Impulse);
		grenade.rigidbody.AddTorque(grenadeThrowInfo.throwTorque, ForceMode.Impulse);

		grenade.Arm(player);
		grenade.AdjustExplosionTimestamp(grenadeThrowInfo.grenadeTimestamp);
	}

	void ReloadLocally()
	{
		if (!m_isReloading && GetBulletsInClip() < equippedWeapon.bulletsPerClip && GetBulletsRemaining() > 0)
		{
			// We will reload in Command.
			if (!isServer)
			{
				reloadingCoroutine = StartCoroutine(Reload(equippedWeapon.reloadTime));
			}

			AudioManager.Instance.PlayReloadAudio(firePoint.gameObject, .5f, equippedWeapon.reloadTime);
			CmdReload();
		}
	}

	[Command]
	void CmdReload()
	{
		if (m_isReloading)
		{
			Debug.Log("PlayerShoot :: CmdReload: Player wanted to reload while still reloading.");
			m_isReloading = true;
			return;
		}

		if (GetBulletsInClip() >= equippedWeapon.bulletsPerClip)
		{
			Debug.Log("PlayerShoot :: CmdReload: Player wanted to reload while being at max bullets in clip.");
			//weapons[weaponIndex] = equippedWeapon.bulletsPerClip;
			return;
		}

		if (GetBulletsRemaining() <= 0)
		{
			Debug.LogWarning("PlayerShoot :: CmdReload: Player wanted to reload while having 0 remaining ammo. Possible Hack?");
			//m_bulletsRemaining = 0;
			return;
		}

		// TODO: Do some checks to see if player can reload?

		RpcReload();
		reloadingCoroutine = StartCoroutine(Reload(equippedWeapon.reloadTime - player.owner.playerConnection.latency.averageLatency / 1000));
	}

	[ClientRpc]
	void RpcReload()
	{
		// We don't wanna reload again if we are the one who reloaded.
		if (hasAuthority)
		{
			return;
		}

		AudioManager.Instance.PlayReloadAudio(firePoint.gameObject, .5f, equippedWeapon.reloadTime);
	}

	IEnumerator Reload(float reloadTime)
	{
		m_isReloading = true;
		weapons[weaponIndex].bulletsRemaining += weapons[weaponIndex].bulletsInClip;
		weapons[weaponIndex].bulletsInClip = 0;

		yield return new WaitForSeconds(reloadTime);

		m_isReloading = false;
		int bulletsToReload = Mathf.Min(equippedWeapon.bulletsPerClip, weapons[weaponIndex].bulletsRemaining);
		weapons[weaponIndex].bulletsInClip = bulletsToReload;
		weapons[weaponIndex].bulletsRemaining -= bulletsToReload;
	}

	void FireLocally()
	{
		if (!CanShoot())
		{
			return;
		}

		Shoot_Base();
		reticleSpread += 80;
		GetComponent<PlayerMovement>().TriggerRecoil(.1f);
	}

	public void RefillAmmo()
	{
		if (hasAuthority)
		{
			CmdRefillAmmo();
		}
	}

	[Command]
	public void CmdRefillGrenades()
	{
		RpcRefillGrenades();
	}

	[ClientRpc]
	void RpcRefillGrenades()
	{
		grenades[0].count = 4;
		grenades[1].count = 4;
		AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.WeaponRefill, gameObject, .4f, false);
	}

	[Command]
	public void CmdRefillAmmo()
	{
		RpcRefillAmmo();
	}

	[ClientRpc]
	void RpcRefillAmmo()
	{
		for (int i = 0; i < weapons.Length; i++)
		{
			PlayerWeaponScriptableObject weapon = GetWeapon(weapons[i].id);
			if (weapon == null)
			{
				continue;
			}
			weapons[i].bulletsRemaining = weapon.bulletsPerClip * weapon.maxClips - weapons[i].bulletsInClip;
		}

		AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.WeaponRefill, gameObject, .4f, false);
	}

	// Hook
	void OnMyWeaponIndex(int newIndex)
	{
		if (weaponIndex == newIndex)
		{
			return;
		}

		weaponIndex = newIndex;
		EquipWeapon(newIndex);
	}

	bool CanChargeUpGrenade()
	{
		return true;
	}

	bool CanThrowGrenade()
	{
		return true;
	}

	bool CanReload()
	{
		if (isReloading)
			return false;

		return true;
	}

	bool CanLookDownScope()
	{
		return true;
	}

	protected override bool CanShoot()
	{
		if (chargingUpGrenade)
		{
			return false;
		}

		if (Time.time <= timeSinceLastFire)
		{
			return false;
		}

		if (GetBulletsInClip() <= 0)
		{
			return false;
		}

		return true;
	}

	protected override float GetFireRate()
	{
		return equippedWeapon.fireRate;
	}

	protected override bool LookingDownScope()
	{
		return lookingDownScope;
	}

	protected override float GetReticleSpread()
	{
		return reticleSpread;
	}

	protected override float GetBulletThickness()
	{
		return equippedWeapon.bulletThickness;
	}

	protected override float GetRange()
	{
		return equippedWeapon.range;
	}

	protected override float GetBulletTrailSpeed()
	{
		return equippedWeapon.bulletTrailSpeed;
	}

	protected override float GetDamage()
	{
		return equippedWeapon.damage;
	}

	protected override int GetBulletsInClip()
	{
		return weapons[weaponIndex].bulletsInClip;
	}

	protected override float GetMyMaxMultiplier(float multiplier)
	{
		return multiplier;
	}

	protected override int GetBulletsRemaining()
	{
		return weapons[weaponIndex].bulletsRemaining;
	}

	protected override void DeductOneBullet()
	{
		weapons[weaponIndex].bulletsInClip--;
	}

	protected override Ray GetRay()
	{
		float reticle;

		if (LookingDownScope())
		{
			reticle = 0;
		}
		else
		{
			reticle = GetReticleSpread();
		}

		float ratio = reticle / canvasHeight;
		int squareSize = (int)(Screen.height * ratio);
		
		int xRand = Random.Range(Screen.width / 2 - squareSize / 2, Screen.width / 2 + squareSize / 2 + 1);
		int yRand = Random.Range(Screen.height / 2 - squareSize / 2, Screen.height / 2 + squareSize / 2 + 1);

		Ray ray = player.camera.ScreenPointToRay(new Vector3(xRand, yRand, 0));

		return ray;
	}
}