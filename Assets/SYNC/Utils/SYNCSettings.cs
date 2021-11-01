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

		internal void Apply(NetManager instance) { }
	}
}
