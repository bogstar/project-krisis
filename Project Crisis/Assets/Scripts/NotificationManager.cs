using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NotificationManager : NetworkBehaviour
{
	public static NotificationManager Instance;

	[Header("Prefabs")]
	[SerializeField]
	GameObject notificationPrefab;

	[Header("Variables")]
	[Tooltip("How long does a notification last in the box in seconds.")]
	[SerializeField]
	float notificationLifetime = 10f;

	int blueTeamHqState = 0;
	int redTeamHqState = 0;

	int announcerState = 0;

	AudioSource alertSource;


	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
		}
		else
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	private void Update()
	{
		if (MatchManager.Instance.matchState == MatchManager.MatchState.InPregame)
		{
			float timeLeftInPregame = MatchManager.Instance.countDown - (float)NetworkTime.time;

			if (timeLeftInPregame <= 31f && announcerState == 0)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin30, .6f, false);
			}
			else if (timeLeftInPregame <= 11f && announcerState == 1)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin10, .6f, false);
			}
			else if (timeLeftInPregame <= 6f && announcerState == 2)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin5, .6f, false);
			}
			else if (timeLeftInPregame <= 5f && announcerState == 3)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin4, .6f, false);
			}
			else if (timeLeftInPregame <= 4f && announcerState == 4)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin3, .6f, false);
			}
			else if (timeLeftInPregame <= 3f && announcerState == 5)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin2, .6f, false);
			}
			else if (timeLeftInPregame <= 2f && announcerState == 6)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin1, .6f, false);
			}
			else if (timeLeftInPregame <= 1f && announcerState == 7)
			{
				announcerState++;
				AudioManager.Instance.PlayAudio(AudioLibrary.AudioOccasion.MissionBegin, .6f, false);
			}
		}

		if (MatchManager.Instance.matchState == MatchManager.MatchState.InGame)
		{
			if (alertSource != null && alertSource.isPlaying)
			{

			}
			else if (alertQueue.Count > 0)
			{
				AudioLibrary.AudioOccasion alert = alertQueue.Dequeue();
				alertSource = AudioManager.Instance.PlayAudio(alert, .7f, false);
			}
		}
	}

	void OnHQAttacked(HealthEntity.NetworkInfo defender, int amount, ref int teamHqState)
	{
		Team defendingTeam = GetTeamFromId(defender.teamId);

		if (amount < 0)
		{
			Headquarters hq = defender.GetHealthEntity() as Headquarters;
			float perc = hq.health / (float)hq.maxHealth;

			if (perc <= .5f && teamHqState == 0)
			{
				foreach (var pc in defendingTeam.players)
				{
					TargetAlertPlayer(pc.connectionToClient, AudioLibrary.AudioOccasion.Hq50percHP);
				}

				teamHqState++;
			}
			if (perc <= .2f && teamHqState == 1)
			{
				foreach (var pc in defendingTeam.players)
				{
					TargetAlertPlayer(pc.connectionToClient, AudioLibrary.AudioOccasion.HqNearlyDead);
				}

				teamHqState++;
			}
		}
	}

	void OnHQDestroyed(short teamId)
	{
		Team losingTeam = GetTeamFromId(teamId);
		Team winningTeam = GetTeamFromId((short)(1 - teamId));

		foreach (var pc in losingTeam.players)
		{
			TargetDisplayVictoryScreen(pc.connectionToClient, "Defeat\n", AudioLibrary.AudioOccasion.Defeat);
		}
		foreach (var pc in winningTeam.players)
		{
			TargetDisplayVictoryScreen(pc.connectionToClient, "Victory\n", AudioLibrary.AudioOccasion.Victory);
		}
	}

	public void OnTurretDestroyed(short teamId)
	{
		Team defendingTeam = GetTeamFromId(teamId);

		foreach (var pc in defendingTeam.players)
		{
			TargetAlertPlayer(pc.connectionToClient, AudioLibrary.AudioOccasion.TurretDestroyed);
		}
	}
	[TargetRpc]
	void TargetDisplayVictoryScreen(NetworkConnection conn, string text, AudioLibrary.AudioOccasion occasion)
	{
		InGameGUI.Instance.infoScreen.Display(text);
		AudioManager.Instance.PlayAudio(occasion, .6f, false);
	}

	float alarmDelay = 10f;
	float blueAlarmElapsed;
	float redAlarmElapsed;

	public void AlarmTeamForHqDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		CmdAlarmTeamForHqDamage(attacker, defender, amount);
	}

	[Command]
	void CmdAlarmTeamForHqDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		short teamId = defender.GetTeamID();
		float alarmElapsed = teamId == 0 ? redAlarmElapsed : blueAlarmElapsed;
		if (Time.time > alarmElapsed)
		{
			if (amount < 0)
			{
				Team teamDefender = GetTeamFromId(defender.GetTeamID());
				foreach (var p in teamDefender.players)
				{
					TargetAlertPlayer(p.connectionToClient, AudioLibrary.AudioOccasion.HqUnderAttack);
				}
				if (teamId == 0)
				{
					redAlarmElapsed = Time.time + alarmDelay;
				}
				else
				{
					blueAlarmElapsed = Time.time + alarmDelay;
				}
			}
		}
	}

	public void AlarmTeamForBaseDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		CmdAlertTeamForBaseDamage(attacker, defender, amount);
	}

	[Command]
	void CmdAlertTeamForBaseDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		short teamId = defender.GetTeamID();
		float alarmElapsed = teamId == 0 ? redAlarmElapsed : blueAlarmElapsed;
		if (Time.time > alarmElapsed)
		{
			if (amount < 0)
			{
				Team teamDefender = GetTeamFromId(teamId);
				foreach (var p in teamDefender.players)
				{
					TargetAlertPlayer(p.connectionToClient, AudioLibrary.AudioOccasion.AttackedBase);
				}
				if (teamId == 0)
				{
					redAlarmElapsed = Time.time + alarmDelay;
				}
				else
				{
					blueAlarmElapsed = Time.time + alarmDelay;
				}
			}
		}
	}

	public void AlarmTeamForTurretDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		CmdAlarmTeamForTurretDamage(attacker, defender, amount);
	}

	[Command]
	void CmdAlarmTeamForTurretDamage(HealthEntity.NetworkInfo attacker, HealthEntity.NetworkInfo defender, int amount)
	{
		short teamId = defender.GetTeamID();
		float alarmElapsed = teamId == 0 ? redAlarmElapsed : blueAlarmElapsed;
		if (Time.time > alarmElapsed)
		{
			if (amount < 0)
			{
				Team teamDefender = GetTeamFromId(defender.GetTeamID());
				foreach (var p in teamDefender.players)
				{
					TargetAlertPlayer(p.connectionToClient, AudioLibrary.AudioOccasion.AttackedTurret);
				}
				if (teamId == 0)
				{
					redAlarmElapsed = Time.time + alarmDelay;
				}
				else
				{
					blueAlarmElapsed = Time.time + alarmDelay;
				}
			}
		}
	}

	Queue<AudioLibrary.AudioOccasion> alertQueue = new Queue<AudioLibrary.AudioOccasion>();

	[TargetRpc]
	void TargetAlertPlayer(NetworkConnection connection, AudioLibrary.AudioOccasion occasion)
	{
		alertQueue.Enqueue(occasion);
	}

	static Team GetTeamFromId(short teamId)
	{
		return MatchManager.GetTeamFromId(teamId);
	}

	[ClientRpc]
	void RpcDisplayNotification(string text, NotificationEvent notEvent)
	{
		if (MatchManager.Instance.matchState == MatchManager.MatchState.InLobby)
		{
			Color color = Color.white;
			switch (notEvent)
			{
				case NotificationEvent.Connect:
					color = Color.green;
					break;
				case NotificationEvent.Disconnect:
					color = Color.red;
					break;
				case NotificationEvent.NameChange:
				case NotificationEvent.TeamChange:
					color = Color.yellow;
					break;
			}
			FindObjectOfType<ChatManager>().DisplaySystemMessage(text, color);
		}
		else
		{
			GameObject prefabToSpawn = notificationPrefab;
			Transform notificationParent = FindObjectOfType<InGameGUI>().notificationHolder;

			Notification notification = Instantiate(prefabToSpawn, notificationParent).GetComponent<Notification>();
			notification.Display(text, notificationLifetime);
		}
	}

	public void DisplayNotification(NotificationEvent notEvent, params object[] parameters)
	{
		string text = "";
		switch (notEvent)
		{
			case NotificationEvent.Connect:
				string nname = (string)parameters[0];
				if (nname == "")
				{
					text = "An unnamed player has joined.";
				}
				else
				{
					text = "Player <b>" + nname + "</b> has joined.";
				}
				break;
			case NotificationEvent.Disconnect:
				HealthEntity.NetworkInfo player = (HealthEntity.NetworkInfo)parameters[0];
				string colorHex = ColorUtility.ToHtmlStringRGBA(MatchManager.GetTeamFromId(player.teamId).teamColor);
				if (player.name == "")
				{
					text = "An unnamed player has left.";
				}
				else
				{
					text = "Player <color=#" + colorHex + "><b>" + player.name + "</b></color> has left.";
				}
				break;
			case NotificationEvent.NameChange:
				string oldName = (string)parameters[0];
				nname = (string)parameters[1];
				if (nname == "" && oldName == "")
				{
					text = "An unnamed player has changed their name to nothing.";
				}
				else if (nname == "" && oldName != "")
				{
					text = "Player <b>" + oldName + "</b> has deleted their name.";
				}
				else if (nname != "" && oldName == "")
				{
					text = "An unnamed player has changed their name to <b>" + nname + "</b>.";
				}
				else
				{
					text = "Player <b>" + oldName + "</b> has changed their name to <b>" + nname + "</b>.";
				}
				break;
			case NotificationEvent.TeamChange:
				nname = (string)parameters[0];
				short newId = (short)parameters[1];
				Team newTeam = MatchManager.GetTeamFromId(newId);
				string newColorHex = ColorUtility.ToHtmlStringRGBA(newTeam.teamColor);
				if (nname == "")
				{
					text = "An unnamed player has changed their team to <color=#" + newColorHex + ">" + newTeam.teamName + "</color>.";
				}
				else
				{
					text = "Player <b>" + nname + "</b> has changed their team to <color=#" + newColorHex + ">" + newTeam.teamName + "</color>.";
				}
				break;
			case NotificationEvent.Kill:
				HealthEntity.NetworkInfo attacker = (HealthEntity.NetworkInfo)parameters[0];
				HealthEntity.NetworkInfo defender = (HealthEntity.NetworkInfo)parameters[1];
				string defenderHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(defender.GetTeamID()).GetColor());
				if (!attacker.HealthEntityExists())
				{
					text = "<color=#" + defenderHex + ">" + defender.GetName() + "</color> has died.";
				}
				else
				{
					string attackerHexx = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(attacker.GetTeamID()).GetColor());
					text = "<color=#" + attackerHexx + ">" + attacker.GetName() + "</color> has killed <color=#" + defenderHex + ">" + defender.GetName() + "</color>.";
				}
				break;
			case NotificationEvent.TurretKill:
				attacker = (HealthEntity.NetworkInfo)parameters[0];
				defender = (HealthEntity.NetworkInfo)parameters[1];
				defenderHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(defender.GetTeamID()).GetColor());
				string attackerHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(attacker.GetTeamID()).GetColor());
				text = "<color=#" + attackerHex + ">" + attacker.GetName() + "</color> has destroyed a <color=#" + defenderHex + ">" + MatchManager.GetTeamFromId(defender.teamId).teamName + " Turret</color>.";
				break;
			case NotificationEvent.ForcefieldKill:
				attacker = (HealthEntity.NetworkInfo)parameters[0];
				defender = (HealthEntity.NetworkInfo)parameters[1];
				defenderHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(defender.GetTeamID()).GetColor());
				attackerHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(attacker.GetTeamID()).GetColor());
				text = "<color=#" + attackerHex + ">" + attacker.GetName() + "</color> has disabled a <color=#" + defenderHex + ">" + MatchManager.GetTeamFromId(defender.teamId).teamName + " Forcefield</color>.";
				break;
			case NotificationEvent.ForcefieldRevive:
				defender = (HealthEntity.NetworkInfo)parameters[0];
				defenderHex = ColorUtility.ToHtmlStringRGB(MatchManager.GetTeamFromId(defender.GetTeamID()).GetColor());
				text = "<color=#" + defenderHex + ">" + MatchManager.GetTeamFromId(defender.teamId).teamName + " Forcefield</color> has appeared.";
				break;
		}
		RpcDisplayNotification(text, notEvent);
	}

	public void RegisterHeadquartersHealthChangeCallback(Headquarters hq)
	{
		switch (hq.GetTeamId())
		{
			case 0:
				hq.RegisterHealthChangeCallback((att, def, am) => { OnHQAttacked(def, am, ref redTeamHqState); });
				break;
			case 1:
				hq.RegisterHealthChangeCallback((att, def, am) => { OnHQAttacked(def, am, ref blueTeamHqState); });
				break;
		}
	}

	public void RegisterHeadquartersDeathCallback(Headquarters hq)
	{
		hq.RegisterDeathCallback((att) => { OnHQDestroyed(hq.GetTeamId()); });
	}

	public void RegisterTurretDeathCallback(Turret t)
	{
		t.RegisterDeathCallback((att) => { OnTurretDestroyed(t.GetTeamId()); });
	}

	public enum NotificationEvent
	{
		Connect,
		Disconnect,
		NameChange,
		TeamChange,
		Kill,
		TurretKill,
		ForcefieldKill,
		ForcefieldRevive
	}
}
 