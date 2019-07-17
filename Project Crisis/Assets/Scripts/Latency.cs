using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Diagnostics;

#pragma warning disable CS0618

namespace Krisis.PlayerConnection
{
	[RequireComponent(typeof(PlayerConnection_MatchData))]
	public class Latency : NetworkBehaviour
	{
		List<int> latencyEntriesAvg = new List<int>();

		List<int> latencyEntries = new List<int>();
		[SerializeField]
		float refreshTime = 1f;
		float timeRemaining;

		[SyncVar]
		int m_latency;

		public int averageLatency;

		public int latency
		{
			get
			{
				return Mathf.Max(1, m_latency);
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			if (!isServer)
			{
				return;
			}

			//StartCoroutine(XD());
		}

		IEnumerator XD()
		{
			while (true)
			{
				yield return new WaitForSeconds(1);

				timer = new Stopwatch();
				timer.Start();
				RpcAskForPing();
			}
		}

		private void Update()
		{
			//CalculateLatency();
		}

		[ClientRpc]
		void RpcAskForPing()
		{
			CmdRespondPing();
		}

		[Command]
		void CmdRespondPing()
		{
			timer.Stop();
			print(netId + " " + timer.Elapsed.TotalMilliseconds);
		}

		Stopwatch timer;

		int lastrtt = 0;
		float lastRtttime = 0f;
		int sameShits = 0;

		void CalculateLatency()
		{
			if (!isServer)
			{
				return;
			}

			timeRemaining -= Time.deltaTime;

			if (NetworkManager.singleton.isNetworkActive)
			{
				//int rtt = MyNetworkManager.Instance.client.GetRTT();
				//int framerate = (int)(1 / Time.deltaTime);
				
				/*
				if (lastrtt != rtt || sameShits >= 40)
				{
					sameShits = 0;
					latencyEntries.Add(rtt);
				}
				else
				{
					sameShits++;
				}

				if (latencyEntries.Count > 20)
				{
					for (int i = 0; i < latencyEntries.Count - 20; i++)
					{
						latencyEntries.RemoveAt(0);
					}
				}

				int sum = 0;
				foreach (var p in latencyEntries)
				{
					sum += p;
				}

				if (latencyEntries.Count > 0)
				{
					if (timeRemaining <= 0)
					{
						int avg = sum / latencyEntries.Count;
						m_latency = avg / 2;
						m_latency = Mathf.Clamp(m_latency, 1, m_latency);
						SetLatency(m_latency);
						timeRemaining = refreshTime;
					}
				}*/

				//lastrtt = rtt;
			}
		}

		void SetLatency(int lat)
		{
			m_latency = lat;

			latencyEntriesAvg.Add(lat);
			int numberOfLats = (int)(10 / ((refreshTime == 0) ? (float)1 : refreshTime));
			if (latencyEntriesAvg.Count > numberOfLats)
			{
				for (int i = 0; i < latencyEntriesAvg.Count - numberOfLats; i++)
				{
					latencyEntriesAvg.RemoveAt(0);
				}
			}

			if (latencyEntriesAvg.Count > 0)
			{
				int sum = 0;
				int n = latencyEntriesAvg.Count;
				for (int i = 0; i < latencyEntriesAvg.Count; i++)
				{
					sum += latencyEntriesAvg[i];
				}
				averageLatency = sum / n;
			}
		}
	}
}