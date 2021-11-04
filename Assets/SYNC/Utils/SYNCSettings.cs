using LiteNetLib;
using Sync.Components;
using UnityEngine;

namespace Sync {
	[CreateAssetMenu(fileName = "new SYNCSettings", menuName = "SYNC/Settings", order = 0)]
	public class SYNCSettings : ScriptableObject {
		[Header("Server")]

		[Header("Shared")]
		public int port;
		public int sendRate;

		[Space]
		public SYNCIdentity[] nonPlayerPrefabs;

		[Header("Debug")]
		public string password = "Debug_key";
		[Range(0, 100)] public int packetLossChance;

		internal void Apply(NetManager instance) {
			if (packetLossChance != 0) {
				instance.SimulatePacketLoss = true;
				instance.SimulationPacketLossChance = packetLossChance;
			}
		}
	}
}
