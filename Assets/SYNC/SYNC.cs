using System;
using Sync.Components;
using Sync.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sync {
	public static class SYNC {
		private static int netID;

		public static bool IsServer { get; internal set; }
		public static bool IsClient { get; internal set; }

		internal static int GetNextNetID() => ++netID;

		public static void Host(string password, SYNCSettings settings, Action onConnect = default) {
			if (IsServer && SYNCServer.Instance.IsRunning) {
				Debug.LogWarning("[SYNC] Unable to host server, a server is already hosted");
				return;
			}

			SYNCServer server = new GameObject("SYNC Server", typeof(SYNCServer)).GetComponent<SYNCServer>();
			server.Host(password, settings, onConnect);
		}

		public static void Connect(string address, int port, string password, SYNCSettings settings, Action onConnect = default) {
			if (IsClient && SYNCClient.Instance.IsConnected) {
				Debug.LogWarning("[SYNC] Unable to connect to address, client is already connected to a host");
				return;
			}

			SYNCClient client = new GameObject("SYNC Client", typeof(SYNCClient)).GetComponent<SYNCClient>();

			client.Connect(address, port, password, settings, onConnect);
		}

		public static void Instantiate(Object prefab) {
			Instantiate_Internal(prefab, Vector3.zero, Quaternion.identity, SYNCInstantiateMode.Standard, SYNCFloatAccuracy.Half);
		}

		public static void Instantiate(Object prefab, Vector3 position, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			Instantiate_Internal(prefab, position, Quaternion.identity, SYNCInstantiateMode.PositionOnly, accuracy);
		}

		public static void Instantiate(Object prefab, Quaternion rotation, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			Instantiate_Internal(prefab, Vector3.zero, rotation, SYNCInstantiateMode.RotationOnly, accuracy);
		}

		public static void Instantiate(Object prefab, Vector3 position, Quaternion rotation, SYNCFloatAccuracy accuracy = SYNCFloatAccuracy.Half) {
			Instantiate_Internal(prefab, position, rotation, SYNCInstantiateMode.PositionAndRotation, accuracy);
		}

		public static void Instantiate(Object prefab, Object parent, bool instantiateInWorldSpace = false) {
			SYNCIdentity syncIdentity = SYNCHelperInternal.GetSYNCIdentity(parent);

			if (syncIdentity == default) {
				Debug.LogError($"[SYNC] Instantiate parent does not have a SYNCIdentity {parent.name}", parent);
				return;
			}

			Instantiate(prefab, syncIdentity.NetID, instantiateInWorldSpace);
		}

		public static void Instantiate(Object prefab, int parentNetID, bool instantiateInWorldSpace = false) {
			if (IsServer)
				SYNCServer.Instance.Instantiate(prefab, parentNetID, instantiateInWorldSpace);
		}

		private static void Instantiate_Internal(Object prefab, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			if (IsServer)
				SYNCServer.Instance.Instantiate(prefab, position, rotation, mode, accuracy);
		}

		public static void Destroy() {
			if (IsServer) {
				SYNCServer.Instance.SendObjectDestroy(GameObject.FindObjectOfType<SYNCIdentity>());
			}
		}
	}
}
