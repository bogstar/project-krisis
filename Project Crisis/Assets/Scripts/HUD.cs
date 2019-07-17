using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
	public Text ammoLabel;
	public Text grenadeLabel;
	public Text healthLabel;
	public Text fpsCounter;
	public Image healthGreen;
	public float healthPixels;

	public Text screenText;

	Player player;

	float timeout;

	string grenade1N;
	string grenade2N;

	public void SetPlayer(Player player)
	{
		this.player = player;
		//grenade1N = GeneralLibrary.Instance.GetItem<GrenadeScriptableObject>(player.playerShoot.grenades[0].id).name;
		//grenade2N = GeneralLibrary.Instance.GetItem<GrenadeScriptableObject>(player.playerShoot.grenades[1].id).name;
	}

	public void UpdateHUD()
	{
		if(timeout > 0.5f)
		{
			timeout = 0;
			fpsCounter.text = "FPS: " + Mathf.Round(1 / Time.deltaTime).ToString();
		}

		timeout += Time.deltaTime;

		if (player == null || player.playerShoot == null || player.isAlive == false)
		{
			return;
		}

		grenadeLabel.text = "Grenades " + grenade1N + "s: "
			+ player.playerShoot.grenades[0].count + " " + grenade2N
			+ "s: " + player.playerShoot.grenades[1].count;
		ammoLabel.text = "Ammo: " + player.playerShoot.weapons[player.playerShoot.weaponIndex].bulletsInClip + "/"
			+ player.playerShoot.weapons[player.playerShoot.weaponIndex].bulletsRemaining;
		healthLabel.text = player.health + "/" + player.maxHealth;
		float healthPercentage = player.health / (float)player.maxHealth;
		healthGreen.rectTransform.sizeDelta = new Vector2(healthPercentage * healthPixels, healthGreen.rectTransform.sizeDelta.y);
		screenText.text = "Lives: " + player.team.lives + "\nOres: " + player.team.ores;
	}
}
