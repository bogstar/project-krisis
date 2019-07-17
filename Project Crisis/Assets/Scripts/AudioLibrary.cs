using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLibrary : Singleton<AudioLibrary>
{
	public AudioClipInstance[] clipInstances;

	public AudioClip GetRandomClipForOccasion(AudioOccasion occasion)
	{
		AudioClip[] clips = GetClipsForOccasion(occasion);

		return clips[Random.Range(0, clips.Length)];
	}

	public AudioClip[] GetClipsForOccasion(AudioOccasion occasion)
	{
		foreach (var clipInstance in clipInstances)
		{
			if (clipInstance.occasion == occasion)
			{
				return clipInstance.clips;
			}
		}
		return new AudioClip[0];
	}

	[System.Serializable]
	public class AudioClipInstance
	{
		public AudioOccasion occasion;
		public AudioClip[] clips;
	}

	public enum AudioOccasion
	{
		Shoot, Hit, Reload, Explosion, Clank, WeaponRefill, HealthRefill, AttackedBase, AttackedTurret, Death, PulseExplosion,
		MissionBegin30, MissionBegin10, MissionBegin5, MissionBegin4, MissionBegin3, MissionBegin2, MissionBegin1, MissionBegin,
		HqNearlyDead, Hq50percHP, HqUnderAttack, Victory, Defeat, TurretDestroyed
	}
}