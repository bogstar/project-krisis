using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterSO", menuName = "Crisis/Character")]
public class CharacterScriptableObject : DatabaseEntryBase
{
	public new string name;
	public ModelSetup_Character model;

	[Header("Health Entity Config")]
	public int health;

	[Header("Moving Config")]
	public float moveSpeed;
	public float jumpStrength;

	[Header("Shooting Config")]
	public int weaponSlots;
	public int grenadeSlots;
}