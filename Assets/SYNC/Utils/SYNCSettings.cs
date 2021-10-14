using LiteNetLib;
using UnityEngine;

namespace SYNC {
	[CreateAssetMenu(fileName = "new SYNCSettings", menuName = "SYNC/Settings", order = 0)]
	internal class SYNCSettings : ScriptableObject {
		public int port;

		public void Apply(NetManager instance) { }
	}
}
