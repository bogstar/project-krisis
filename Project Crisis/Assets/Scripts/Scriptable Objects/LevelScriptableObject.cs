using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelSO", menuName = "Crisis/Level")]
public class LevelScriptableObject : DatabaseEntryBase
{
	public new string name;
	public Object scene;
	public Gamemode[] gamemodesAvailable;
	public PlayerCount[] playerCountsAvailable;

	public enum Gamemode
	{
		Mesa
	}

	public enum PlayerCount
	{
		_2V2,
		_3V3,
		_5V5
	}

	public enum Map
	{
		SourRush
	}
}