using SYNC.Components;
using UnityEngine;

namespace SYNC {
	public static class SYNC {
		private static int netID;
		internal static int NextNetID => ++netID;

		public static bool IsServer { get; internal set; }
		public static bool IsClient { get; internal set; }
		public static void Instantiate(Object prefab) {
			if (IsServer) {
				SYNCServer.Instance.SendObjectInstantiate(prefab, new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0), Quaternion.identity);
			}
		}

		public static void Destroy() {
			if (IsServer) {
				SYNCServer.Instance.SendObjectDestroy(GameObject.FindObjectOfType<SYNCIdentity>());
			}
		}
	}
}
