using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

#pragma warning disable CS0618

public class PlayerConnection_MatchData : NetworkBehaviour
{
	/*[SyncVar(hook = "OnMyTeam")]
	public short teamId;*/
	[SyncVar(hook = "OnMyAvatarGameObject")]
	public GameObject myAvatarGameObject;

	public Player myAvatar { get; private set; }
	public PlayerConnection playerConnection { get; private set; }
	LevelManager levelManager { get { return LevelManager.Instance; } }

	public Team team { get { return MatchManager.GetTeamFromId(playerConnection.teamId); } }

	public bool isPlayerAlive
	{
		get
		{
			if (myAvatar == null || myAvatar.isAlive == false)
				return false;
			return true;
		}
	}

	private void OnEnable()
	{
		playerConnection = GetComponent<PlayerConnection>();

		// If this is the moment of joining where this PC had already spawned, let's see if
		// the PC has an avatar.
		if (myAvatarGameObject != null)
		{
			// Avatar exists -- refresh SyncVars.
			OnMyAvatarGameObject(myAvatarGameObject);
		}

		if (hasAuthority)
		{
			//MatchManager.Instance.localPlayerConnection = playerConnection;
			//playerConnection.SetTeam(0);
			InGameGUI.Instance.DisplaySpawnPanel(true);
			InGameGUI.Instance.loadingScreen.gameObject.SetActive(false);
		}

		playerConnection.RegisterNameCallback(OnNameChange);
	}

	private void OnDisable()
	{
		playerConnection.UnregisterNameCallback(OnNameChange);
	}

	// PlayerConnection has appeared on the server and has NOT received authority yet!
	// This could be either a new PC, or a PC existing on the server.
	public override void OnStartClient()
	{
		// Start all the goodies needed on the PC.
		base.OnStartClient();
	}

	void OnNameChange(string oldName, string newName)
	{
		if (myAvatar != null)
		{
			myAvatar.RefreshMe();
		}
	}

	// PlayerConnection has gained authority, meaning it is only called on that particular client.
	public override void OnStartAuthority()
	{
		// Do the goodies needed for gaining authority.
		base.OnStartAuthority();

		//levelManager.OnConnect(this);
	}

	// PlayerConnection has disconnected.
	private void OnDestroy()
	{
		if (hasAuthority)
		{
			//levelManager.OnDisconnect(this);
		}
	}

	// Spawn player from the button.
	// TODO: This isnt a proper spawning method.
	public void SpawnPlayer()
	{
		CmdSpawnPlayer();
		CmdSeetPlayer();
	}

	/*
	[Command]
	void CmdModifyPlayer(string newName, short newId)
	{
		MatchManager.Instance.ClientChangeName(name, newName);
		MatchManager.Instance.ClientChangeTeam(this, newId);
		name = newName;
		teamId = newId;
	}*/

	public void Die(HealthEntity.NetworkInfo attackerInfo)
	{
		CmdDie(attackerInfo);
	}

	public void DieSilent()
	{
		CmdDieSilent();
	}

	[Command]
	void CmdDieSilent()
	{
		if (myAvatar != null)
		{
			myAvatar.isAlive = false;

			NetworkServer.Destroy(myAvatar.gameObject);
		}
	}

	[Command]
	void CmdDie(HealthEntity.NetworkInfo attackerInfo)
	{
		if (myAvatar != null)
		{
			HealthEntity.NetworkInfo defender = new HealthEntity.NetworkInfo(myAvatar.gameObject);
			myAvatar.isAlive = false;

			if (MatchManager.Instance.matchState == MatchManager.MatchState.InPregame)
			{
				StartCoroutine(CountDownDead(2f));
			}
			else if (MatchManager.Instance.matchState == MatchManager.MatchState.InGame)
			{
				StartCoroutine(CountDownDead(10f));
			}
			
			RpcDie(attackerInfo, defender);
		}
	}

	IEnumerator CountDownDead(float time)
	{
		yield return new WaitForSeconds(time);

		myAvatar.GetComponent<NetworkIdentity>().RemoveClientAuthority(myAvatar.GetNetworkConnectionToClient());
		RpcCountDownDead();
		TargetDie(connectionToClient);
	}

	[ClientRpc]
	void RpcCountDownDead()
	{
		Destroy(myAvatar.camera.gameObject);
		myAvatar.playerShoot.enabled = false;
		myAvatar.playerMovement.enabled = false;
		myAvatar.graphics.enabled = true;
		myAvatar = null;
	}

	[ClientRpc]
	void RpcDie(HealthEntity.NetworkInfo attackerInfo, HealthEntity.NetworkInfo defenderInfo)
	{
		NotificationManager.Instance.DisplayNotification(NotificationManager.NotificationEvent.Kill, attackerInfo, defenderInfo);
		AudioManager.Instance.PlayAudioOnLocation(AudioLibrary.AudioOccasion.Death, myAvatar.transform.position, 1f, false);
		defenderInfo.gameObject.GetComponent<NetworkTransform>().enabled = false;
		defenderInfo.gameObject.GetComponents<NetworkTransformChild>()[0].enabled = false;
		defenderInfo.gameObject.GetComponents<NetworkTransformChild>()[1].enabled = false;
		if (myAvatar != null)
		{
			myAvatar.DestroyAfterWhile(30f);
			myAvatar.playerShoot.UnequipWeapon();
			myAvatar.GetComponent<CharacterController>().enabled = false;
			myAvatar.GetComponent<Animator>().SetTrigger("Death");
		}
	}

	[TargetRpc]
	void TargetDie(NetworkConnection conn)
	{
		InGameGUI.Instance.DisplayDeadScreen();
	}

	[ClientRpc]
	public void RpcModifyLives(int newAmount)
	{
		team.lives = newAmount;
	}

	public void SetAvatar(GameObject go)
	{
		RpcSetAvatar(go);
	}

	[ClientRpc]
	void RpcSetAvatar(GameObject go)
	{
		myAvatarGameObject = go;
	}

	public void SeetPlayer()
	{
		CmdSeetPlayer();
	}

	[Command]
	void CmdSeetPlayer()
	{
		OnMyAvatarGameObject(myAvatarGameObject);
		RpcSeetPlayer();
	}

	[ClientRpc]
	void RpcSeetPlayer()
	{
		PlayerConnection_LobbyData ld = GetComponent<PlayerConnection_LobbyData>();

		enabled = true;
		ld.enabled = false;
	}

	[Command]
	void CmdSpawnPlayer()
	{
		if (isPlayerAlive)
		{
			return;
		}

		if (team.lives < 1)
		{
			return;
		}

		if (MatchManager.Instance.matchState == MatchManager.MatchState.InGame)
		{
			team.lives--;
			RpcModifyLives(team.lives);
		}

		Player newAvatar = Instantiate(GeneralLibrary.Instance.playerPrefab).GetComponent<Player>();
		string charr = Random.Range(0, 2) == 0 ? "scout" : "heavy";
		CharacterScriptableObject charData = GeneralLibrary.Instance.GetItem<CharacterScriptableObject>(charr);
		newAvatar.Init(charData);
		newAvatar.InitiateHealth(charData.health);
		NetworkServer.SpawnWithClientAuthority(newAvatar.gameObject, connectionToClient);

		newAvatar.gameObject.transform.position = team.teamId == 0 ? levelManager.redTeamSpawnPoint : levelManager.blueTeamSpawnPoint;
		newAvatar.gameObject.transform.LookAt(team.teamId == 0 ? Vector3.right : -Vector3.right);

		myAvatarGameObject = newAvatar.gameObject;
		myAvatar = newAvatar;
		myAvatar.RegisterDeathCallback(Die);
		myAvatar.owner = this;
		myAvatar.isAlive = true;

		//LevelManager.Instance.OnButton_Spawn();

		//myAvatarGameObject.transform.position = team.teamId == 0 ? levelManager.redTeamSpawn.position : levelManager.blueTeamSpawn.position;

		RpcSetPlayer(myAvatarGameObject, charr);
		RpcSetThisPostion(myAvatarGameObject.transform.position, newAvatar.gameObject.transform.rotation);
		
		//playerConnection.SetName(name);
	}

	void ModifyPlayer()
	{
		if (myAvatar != null)
		{
			myAvatar.RefreshMe();
		}
	}

	[ClientRpc]
	void RpcSetThisPostion(Vector3 newpos, Quaternion newRot)
	{
		myAvatar.transform.position = newpos;
		myAvatar.transform.rotation = newRot;
		myAvatar.RefreshMe();
	}

	[ClientRpc]
	void RpcSetPlayer(GameObject avatarGameObject, string character)
	{
		myAvatar = avatarGameObject.GetComponent<Player>();
		if (!isServer)
		{
			myAvatar.Init(GeneralLibrary.Instance.GetItem<CharacterScriptableObject>(character));
		}
		myAvatar.owner = this;
		myAvatar.myDisplay.ToggleFollow(true);

		ModifyPlayer();
	}

	// ------- SyncVar Hooks  ------------------------------------

	// avatarGameObject hook
	void OnMyAvatarGameObject(GameObject newAvatarGO)
	{
		myAvatarGameObject = newAvatarGO;
		if (myAvatarGameObject != null && myAvatarGameObject.GetComponent<Player>() != null)
		{
			Player newAvatar = newAvatarGO.GetComponent<Player>();
			myAvatar = newAvatar;
			myAvatar.owner = this;
			myAvatar.RefreshMe();
		}
	}
}