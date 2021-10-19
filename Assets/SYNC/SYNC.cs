using SYNC.Components;
using SYNC.Utils;
using UnityEngine;

namespace SYNC {
	public static class SYNC {
		private static int netID;
		internal static int NextNetID => ++netID;

		public static bool IsServer { get; internal set; }
		public static bool IsClient { get; internal set; }
		public static void Instantiate(Object prefab) {
			InstantiateInternal(prefab, Vector3.zero, Quaternion.identity, SYNCInstantiateMode.Standard, SYNCFloatAccuracy.Half);
		}

		public static void Instantiate(Object prefab, Vector3 position, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			InstantiateInternal(prefab, position, Quaternion.identity, SYNCInstantiateMode.PositionOnly, accuracy);
		}

		public static void Instantiate(Object prefab, Quaternion rotation, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			InstantiateInternal(prefab, Vector3.zero, rotation, SYNCInstantiateMode.RotationOnly, accuracy);
		}

		public static void Instantiate(Object prefab, Vector3 position, Quaternion rotation, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			InstantiateInternal(prefab, position, rotation, SYNCInstantiateMode.PositionAndRotation, accuracy);
		}

		private static void InstantiateInternal(Object prefab, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			if (IsServer)
				SYNCServer.Instance.SendObjectInstantiate(prefab, position, rotation, mode, accuracy);
		}

		public static void Destroy() {
			if (IsServer) {
				SYNCServer.Instance.SendObjectDestroy(GameObject.FindObjectOfType<SYNCIdentity>());
			}
		}
	}
}
