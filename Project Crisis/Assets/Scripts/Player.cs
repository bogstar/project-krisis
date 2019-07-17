using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class Player : HealthEntity
{
	public PlayerConnection_MatchData owner;
	public LookAtCamera myDisplay;
	public PlayerHUD myHud;

	public CharacterScriptableObject characterData;

	public MeshRenderer graphics;

	public new Camera camera;

	public Team team { get { return owner?.team; } }

	public PlayerShoot playerShoot { get; private set; }
	public PlayerMovement playerMovement { get; private set; }

	HUD hud;


	void Start()
	{
		GetComponent<CharacterController>().detectCollisions = false;
		RegisterHealthChangeCallback(OnHealthChangePlayer);

		if (!hasAuthority)
		{
			camera.enabled = false;
			camera.gameObject.tag = "Untagged";
			myDisplay.ToggleFollow(true);
		}

		playerShoot = GetComponent<PlayerShoot>();
		playerMovement = GetComponent<PlayerMovement>();
	}

	public void Init(CharacterScriptableObject characterSO)
	{
		characterData = characterSO;

		ModelSetup_Character model = Instantiate(characterSO.model.gameObject, transform).GetComponent<ModelSetup_Character>();
		model.name = "Graphics";
		GetComponent<Animator>().Rebind();

		LoadHittables();

		graphics = model.mesh;
		if (hasAuthority)
		{
			graphics.enabled = false;
		}
	}

	public void DestroyAfterWhile(float time)
	{
		StartCoroutine(CheckForDeathAnimation());
		myHud.gameObject.SetActive(false);
		Destroy(graphics.gameObject, time);
		Destroy(gameObject, time);
	}

	IEnumerator CheckForDeathAnimation()
	{
		while (!GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death_Idle"))
		{
			yield return null;
		}

		graphics.transform.SetParent(null);
	}

	private void Update()
	{
		if (hasAuthority)
		{
			if (hud != null)
			{
				hud.UpdateHUD();
			}
		}
	}

	void OnHealthChangePlayer(NetworkInfo attacker, NetworkInfo defender, int amount)
	{
		if (!hasAuthority)
		{
			return;
		}

		if (amount < 0)
		{
			if (attacker.GetName() == "Grenade")
			{
				HitDirectionIndicatorHUD indicator = Instantiate(InGameGUI.Instance.hitIndicatorPrefab, GameObject.Find("GUI").transform).GetComponent<HitDirectionIndicatorHUD>();
				indicator.Show(transform, attacker.GetPosition(), 2f);
				AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.Clank, transform.gameObject, 1f, true);
			}
		}
	}
	
	public override void OnStartClient()
	{
		base.OnStartClient();
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();

		hud = FindObjectOfType<HUD>();

		camera.enabled = true;
		camera.gameObject.tag = "MainCamera";
		camera.gameObject.AddComponent<AudioListener>();
		myDisplay.gameObject.SetActive(false);
		hud.SetPlayer(this);
		InGameGUI.Instance.ClearScreen();
		LevelManager.Instance.EnableOverlookCamera(false);
		//StartCoroutine(WaitForOwner());

		if (graphics != null)
		{
			graphics.enabled = false;
		}
	}

	IEnumerator WaitForOwner()
	{
		while (true)
		{
			if (owner != null)
			{
				break;
			}

			yield return null;
		}
	}
	
	public void RefillHealth()
	{
		CmdRefillHealth();
	}
	
	[Command]
	void CmdRefillHealth()
	{
		m_health = maxHealth;
		RpcRefillHealth();
	}

	[ClientRpc]
	void RpcRefillHealth()
	{
		AudioManager.Instance.PlayAudioOnObject(AudioLibrary.AudioOccasion.HealthRefill, gameObject, .4f, false);
	}
	
	public void RefreshMe()
	{
		ChangeMaterialColor(team.teamColor);
		ChangeName(owner.playerConnection.name);
		gameObject.name = "Player (" + owner.playerConnection.name + ")";

		Forcefield[] ffs = FindObjectsOfType<Forcefield>();
		foreach (var ff in ffs)
		{
			if (ff.teamId == team.teamId)
			{
				CharacterController cc = GetComponent<CharacterController>();

				ff.IgnoreCollisions(cc);
			}
		}

		HittableArea[] hittables = GetComponentsInChildren<HittableArea>();
		foreach (var hit in hittables)
		{
			hit.SetHealthEntity(this);
		}
	}

	public void ChangeName(string newName)
	{
		myHud.UpdateText(newName);
		myDisplay.ToggleFollow(true);
	}

	void ChangeMaterialColor(Color newColor)
	{
		graphics.materials[2].color = newColor;
		transform.Find("Camera Pivot").Find("Hand_R").GetComponent<MeshRenderer>().material.color = newColor;
		transform.Find("Camera Pivot").Find("Hand_L").GetComponent<MeshRenderer>().material.color = newColor;
	}

	public override short GetTeamId()
	{
		return team.teamId;
	}
	
	public override NetworkConnection GetNetworkConnectionToClient()
	{
		return owner.playerConnection.connectionToClient;
	}
}