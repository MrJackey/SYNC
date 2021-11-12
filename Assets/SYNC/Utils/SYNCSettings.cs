using LiteNetLib;
using Sync.Components;
using UnityEngine;

namespace Sync {
	[CreateAssetMenu(fileName = "new SYNCSettings", menuName = "SYNC/Settings", order = 0)]
	public class SYNCSettings : ScriptableObject {
		[Tooltip("List of all prefabs which can be instantiated and replicated to clients")]
		public SYNCIdentity[] nonPlayerPrefabs;

		[Header("Settings")]
		[Tooltip("The rate at which the server sends updates to all connected clients (in seconds)")]
		public int serverSendRate = 20;

		[Tooltip("Port used when setting up and connecting to a server")]
		public int port;

		[Tooltip("How many clients are allowed to be connected to the same server instance, -1 if infinite")]
		public int maxAllowedClients = 4;

		[Tooltip("Maximum delay until a package is sent (milliseconds)")]
		public int sendPeriod = 15;

		[Tooltip("Interval for latency detection and checking connection (milliseconds)")]
		public int pingInterval = 1000;

		[Tooltip("Delay until a connection closes when a client/server stops receiving packets (milliseconds)")]
		public int disconnectTimeout = 5000;

		[Tooltip("Delay between connection attempts (milliseconds)")]
		public int reconnectDelay = 500;

		[Tooltip("Maximum connection attempts before a client disconnects")]
		public int maxConnectAttempts = 10;

		[Header("Debug")]
		public string password = "Debug_key";

		[Space]
		[Tooltip("Turn on to simulate packet loss (Requires DEBUG mode")]
		public bool simulatePacketLoss;

		[Range(0, 100), Tooltip("When simulating packet loss, drop a random amount of packets (percentage)")]
		public int packetLossChance;

		[Space]
		[Tooltip("Turn on to simulate latency (Requires DEBUG mode")]
		public bool simulateLatency;

		[Min(0), Tooltip("When simulating latency, hold packets for a minimum time")]
		public int minLatency = 30;

		[Min(0), Tooltip("When simulating latency, hold packets for a maximum time")]
		public int maxLatency = 100;

		internal void Apply(NetManager instance) {
			instance.UpdateTime = sendPeriod;
			instance.PingInterval = pingInterval;
			instance.DisconnectTimeout = disconnectTimeout;

			instance.ReconnectDelay = reconnectDelay;
			instance.MaxConnectAttempts = maxConnectAttempts;

			instance.SimulatePacketLoss = simulatePacketLoss;
			if (simulatePacketLoss) {
				instance.SimulatePacketLoss = true;
				instance.SimulationPacketLossChance = packetLossChance;
			}

			instance.SimulateLatency = simulateLatency;
			if (simulateLatency) {
				instance.SimulationMinLatency = minLatency;
				instance.SimulationMaxLatency = maxLatency;
			}
		}
	}
}
