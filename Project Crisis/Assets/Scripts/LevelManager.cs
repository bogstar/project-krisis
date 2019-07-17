using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Krisis.PlayerConnection;

public class LevelManager : BaseSceneManager
{
	public static LevelManager Instance;

	public LevelScriptableObject levelScriptableObject;

	public TeamBase redTeamBase;
	public TeamBase blueTeamBase;

	public Vector3 redTeamSpawnPoint { get { return redTeamBase.spawnPoint; } }
	public Vector3 blueTeamSpawnPoint { get { return blueTeamBase.spawnPoint; } }

	public Transform pickupManager;
	[SerializeField]
	Camera overlookCamera;

	private void Awake()
	{
		Instance = this;

		Instantiate(GameManager.Instance.inGameGuiPrefab).name = "GUI";
	}

	public void EnableOverlookCamera(bool enable)
	{
		switch (enable)
		{
			case true:
				overlookCamera.gameObject.AddComponent<AudioListener>();
				break;
			default:
				Destroy(overlookCamera.gameObject.GetComponent<AudioListener>());
				break;
		}
		overlookCamera.gameObject.SetActive(enable);
	}
}