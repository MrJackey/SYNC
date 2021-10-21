using System;
using SYNC.Components;
using SYNC.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SYNC {
	public static class SYNC {
		private static int netID;
		internal static int NextNetID => ++netID;

		public static bool IsServer { get; internal set; }
		public static bool IsClient { get; internal set; }

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
