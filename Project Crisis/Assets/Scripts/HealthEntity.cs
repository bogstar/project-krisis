using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

#pragma warning disable CS0618

public abstract class HealthEntity : NetworkBehaviour
{
	public int health { get { return m_health; } }
	public int maxHealth { get { return m_maxHealth; } }

	protected HittableArea[] hittables;
	
	[SyncVar (hook = "OnHealthHook_Base")]
	protected int m_health;
	[SyncVar]
	protected int m_maxHealth;

	[SyncVar]
	public bool isAlive;
	
	protected event System.Action<NetworkInfo> OnDeath;
	
	public delegate void HealthChangeDelegate(NetworkInfo attacker, NetworkInfo defender, int amount);

	[SyncEvent]
	protected event HealthChangeDelegate EventHealthChange;

	public virtual void TakeHealing(int amount)
	{
		CmdTakeHealing_Base(amount);
	}

	public void InitiateHealth(int health)
	{
		m_maxHealth = health;
		m_health = health;
	}

	protected virtual void LoadHittables()
	{
		hittables = GetComponentsInChildren<HittableArea>();
		foreach (var hittable in hittables)
		{
			hittable.SetHealthEntity(this);
		}
	}

	[Command]
	protected virtual void CmdTakeHealing_Base(int amount)
	{
		if (!isAlive)
		{
			return;
		}

		amount = Mathf.Clamp(amount, 0, amount);

		m_health += amount;
		if (EventHealthChange != null)
		{
			NetworkInfo attacker = NetworkInfo.nobody;
			NetworkInfo defender = new NetworkInfo(gameObject);
			EventHealthChange(attacker, defender, amount);
		}
		if (health > maxHealth)
		{
			m_health = maxHealth;
		}

		SendMessage("CmdTakeHealing", new object[] { amount }, SendMessageOptions.DontRequireReceiver);
	}

	public virtual void TakeDamage(int amount, NetworkInfo attacker)
	{
		CmdTakeDamage_Base(amount, attacker);
	}

	[Command]
	protected virtual void CmdTakeDamage_Base(int amount, NetworkInfo attacker)
	{
		// Can't take damage if we are dead.
		if (!isAlive)
		{
			return;
		}

		// Can we take damage if we are in pregame?
		if (!CanTakeDamageInPregame() && MatchManager.Instance.matchState == MatchManager.MatchState.InPregame)
		{
			return;
		}

		// Are we actually immune to friendly fire OR friendly fire is off?
		if (attacker.teamId == GetTeamId() && (IsImmuneToFriendlyFire() || !MatchManager.Instance.friendlyFire))
		{
			return;
		}

		amount = Mathf.Clamp(amount, 0, amount);

		m_health -= amount;
		
		if (EventHealthChange != null)
		{
			NetworkInfo defender = new NetworkInfo(gameObject);
			EventHealthChange(attacker, defender, -amount);
		}
		
		if (health <= 0)
		{
			m_health = 0;
			Die(attacker);
		}

		SendMessage("CmdTakeDamage", new object[] { amount, attacker }, SendMessageOptions.DontRequireReceiver);
	}

	protected virtual void Die(NetworkInfo attacker)
	{
		RpcDie_Base(attacker);
		OnDeath?.Invoke(attacker);
	}

	protected virtual bool CanTakeDamageInPregame() { return true; }
	protected virtual bool IsImmuneToFriendlyFire() { return false; }
	public virtual short GetTeamId() { return -1; }
	public virtual NetworkConnection GetNetworkConnectionToClient() { return null; }

	[ClientRpc]
	protected virtual void RpcDie_Base(NetworkInfo attacker)
	{
		isAlive = false;
		EnableHittables(false);

		SendMessage("RpcDie", attacker, SendMessageOptions.DontRequireReceiver);
	}

	public void RegisterDeathCallback(System.Action<NetworkInfo> cb)
	{
		OnDeath += cb;
	}

	public void UnregisterDeathCallback(System.Action<NetworkInfo> cb)
	{
		OnDeath -= cb;
	}

	public void RegisterHealthChangeCallback(HealthChangeDelegate cb)
	{
		EventHealthChange += cb;
	}

	public void UnregisterHealthChangeCallback(HealthChangeDelegate cb)
	{
		EventHealthChange -= cb;
	}

	private void OnHealthHook_Base(int newHealth)
	{
		m_health = newHealth;
		SendMessage("OnHealthHook", newHealth, SendMessageOptions.DontRequireReceiver);
	}

	protected void EnableHittables(bool enable)
	{
		foreach (var hit in hittables)
		{
			hit.EnableHittables(enable);
		}
	}

	public struct NetworkInfo
	{
		public GameObject gameObject;
		public string name;
		public short teamId;
		public Vector3 position;

		public NetworkInfo(GameObject gameObject)
		{
			this.gameObject = gameObject;
			this.teamId = -1;
			this.name = "Grenade";
			this.position = gameObject == null ? Vector3.zero : gameObject.transform.position;

			HealthEntity he = (gameObject != null) ? gameObject.GetComponent<HealthEntity>() : null;
			if (he != null)
			{
				if (he is Turret)
				{
					name = "Defense Turret";
				}
				else if (he is Player)
				{
					name = ((Player)he).owner.playerConnection.name;
				}
				else if (he is Forcefield)
				{
					name = "Forcefield";
				}

				teamId = he.GetTeamId();
			}
		}

		public static NetworkInfo nobody
		{
			get
			{
				return new NetworkInfo(null);
			}
		}

		public HealthEntity GetHealthEntity()
		{
			HealthEntity he = gameObject.GetComponent<HealthEntity>();
			if (he == null)
			{
				return null;
			}

			return he;
		}

		public string GetName()
		{
			return name;
		}

		public Vector3 GetPosition()
		{
			return position;
		}

		public short GetTeamID()
		{
			if (teamId == -1)
			{
				throw new System.Exception("Health Entity doesn't exit.");
			}

			return teamId;
		}

		public bool HealthEntityExists()
		{
			if (teamId == -1)
			{
				return false;
			}

			return true;
		}
	}
}