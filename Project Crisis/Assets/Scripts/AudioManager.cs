using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
	public void PlayReloadAudio(GameObject gameObj, float volume, float reloadDuration)
	{
		float audioDuration = AudioLibrary.Instance.GetRandomClipForOccasion(AudioLibrary.AudioOccasion.Reload).length;
		float ratio = audioDuration / reloadDuration;

		AudioSource source = gameObj.AddComponent<AudioSource>();

		source.clip = AudioLibrary.Instance.GetRandomClipForOccasion(AudioLibrary.AudioOccasion.Reload);

		source.pitch = ratio;
		source.spatialBlend = 1;
		source.volume = volume;

		source.Play();

		Destroy(source, audioDuration * (1 / source.pitch));
	}

	public AudioSource PlayAudio(AudioLibrary.AudioOccasion occasion, float volume, bool randomizePitch)
	{
		AudioSource source = FindObjectOfType<AudioListener>().gameObject.AddComponent<AudioSource>();

		source.clip = AudioLibrary.Instance.GetRandomClipForOccasion(occasion);

		source.pitch = randomizePitch ? Random.Range(.85f, 1.15f) : 1f;
		source.spatialBlend = 0;
		source.volume = volume;

		source.Play();

		Destroy(source, source.clip.length * (1 / source.pitch));

		return source;
	}

	public void PlayAudioOnLocation(AudioLibrary.AudioOccasion occasion, Vector3 location, float volume, bool randomizePitch)
	{
		AudioSource source = new GameObject("Sound Source").AddComponent<AudioSource>();
		source.transform.position = location;

		source.clip = AudioLibrary.Instance.GetRandomClipForOccasion(occasion);

		source.pitch = randomizePitch ? Random.Range(.85f, 1.15f) : 1f;
		source.spatialBlend = 1;
		source.volume = volume;

		source.Play();

		Destroy(source.gameObject, source.clip.length * (1 / source.pitch));
	}

	public void PlayAudioOnObject(AudioLibrary.AudioOccasion occasion, GameObject gameObj, float volume, bool randomizePitch)
	{
		AudioSource source = gameObj.AddComponent<AudioSource>();

		source.clip = AudioLibrary.Instance.GetRandomClipForOccasion(occasion);

		source.pitch = randomizePitch ? Random.Range(.85f, 1.15f) : 1f;
		source.spatialBlend = 1;
		source.volume = volume;

		source.Play();

		Destroy(source, source.clip.length * (1 / source.pitch));
	}
}