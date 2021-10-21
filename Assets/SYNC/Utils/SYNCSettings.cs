using LiteNetLib;
using Sync.Components;
using UnityEngine;

namespace Sync {
	[CreateAssetMenu(fileName = "new SYNCSettings", menuName = "SYNC/Settings", order = 0)]
	public class SYNCSettings : ScriptableObject {
		[Header("Server")]
		public short tickRate;

		[Header("Shared")]
		public int port;

		[Space]
		public SYNCIdentity[] nonPlayerPrefabs;

		[Header("Debug")]
		public string password = "Debug_key";

		internal void Apply(NetManager instance) { }
	}
}
