using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Team
{
	public string teamName;
	public short teamId;
	public Color teamColor;
	public List<PlayerConnection> players = new List<PlayerConnection>();

	public TeamBase teamBase;

	public int lives;
	public int ores;

	public float timeForNextLife;


	public void SetBase(TeamBase teamBase, HealthEntity.HealthChangeDelegate underAttackBaseCB, HealthEntity.HealthChangeDelegate underAttackTurretCB, HealthEntity.HealthChangeDelegate underAttackHqCB)
	{
		this.teamBase = teamBase;
		this.teamBase.SpawnHQ(underAttackHqCB);
		this.teamBase.ColorWalls(teamColor);
		this.teamBase.ActivateForcefields(teamId, underAttackBaseCB);
		this.teamBase.ActivateTurrets(teamId, underAttackTurretCB);
	}

	public Team(string teamName, short teamId, Color teamColor, int lives, int ores)
	{
		this.teamName = teamName;
		this.teamId = teamId;
		this.teamColor = teamColor;
		this.lives = lives;
		this.ores = ores;
	}

	public Color GetColor()
	{
		return teamColor;
	}

	public void AddPlayer(PlayerConnection p)
	{
		if (!players.Contains(p))
		{
			players.Add(p);
		}
	}

	public void RemovePlayer(PlayerConnection p)
	{
		if (players.Contains(p))
		{
			players.Remove(p);
		}
	}
}