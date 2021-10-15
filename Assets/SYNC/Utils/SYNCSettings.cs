using LiteNetLib;
using SYNC.Components;
using UnityEngine;

namespace SYNC {
	[CreateAssetMenu(fileName = "new SYNCSettings", menuName = "SYNC/Settings", order = 0)]
	internal class SYNCSettings : ScriptableObject {
		public short tickRate;
		public int port;

		[Space]
		public SYNCIdentity[] nonPlayerPrefabs;

		public void Apply(NetManager instance) { }
	}
}
