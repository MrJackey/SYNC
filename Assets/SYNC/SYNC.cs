using System;
using LiteNetLib;
using Sync.Components;
using Sync.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sync {
	public delegate void SYNCPlayerConnectedCallback(int clientNetID);
	public delegate void SYNCPlayerDisconnectedCallback(int clientNetID, DisconnectReason reason);

	public static class SYNC {
		private static int netID;
		internal static int IncrementNetID() => ++netID;

		public static bool IsServer { get; internal set; }
		public static bool IsClient { get; internal set; }

		/// <summary>
		/// Returns the netID assigned to this client or -1 if not connected or is server-only
		/// </summary>
		public static int ClientNetID => IsClient ? SYNCClient.Instance.ClientNetID : -1;

		internal static event Action setupComplete;

		/// <summary>
		/// Invoked whenever a player (excluding myself) has connected to the server
		/// </summary>
		public static event SYNCPlayerConnectedCallback playerConnected;

		/// <summary>
		/// Invoked whenever a player (excluding myself) has disconnected from the server
		/// </summary>
		public static event SYNCPlayerDisconnectedCallback playerDisconnected;

		public static void Host(string password, SYNCSettings settings, SYNCPlayerConnectedCallback onConnect = default) {
			if (IsServer && SYNCServer.Instance.IsRunning)
				throw new InvalidOperationException("[SYNC] Unable to host server, a server is already hosted");

			if (IsClient && SYNCClient.Instance.IsConnected)
				throw new InvalidOperationException("[SYNC] Unable to host server, you are already connected to another one");

			SYNCServer server = SYNCServer.Instance != null
				? SYNCServer.Instance
				: new GameObject("SYNC Server", typeof(SYNCServer)).GetComponent<SYNCServer>();

			server.Host(password, settings, onConnect);
		}

		public static void Connect(string address, int port, string password, SYNCSettings settings, SYNCPlayerConnectedCallback onConnect = default) {
			if (IsClient && SYNCClient.Instance.IsConnected)
				throw new InvalidOperationException("[SYNC] Unable to connect to address, client is already connected to a host");

			SYNCClient client = SYNCClient.Instance != null
				? SYNCClient.Instance
				: new GameObject("SYNC Client", typeof(SYNCClient)).GetComponent<SYNCClient>();

			client.Connect(address, port, password, settings, onConnect);
		}

		public static void Disconnect() {
			if (IsClient)
				SYNCClient.Instance.Disconnect();

			if (IsServer)
				SYNCServer.Instance.Shutdown();
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
			else if (IsClient)
				SYNCClient.Instance.Instantiate(prefab, parentNetID, instantiateInWorldSpace);
		}

		private static void Instantiate_Internal(Object prefab, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			if (IsServer)
				SYNCServer.Instance.Instantiate(prefab, position, rotation, mode, accuracy);
			else if (IsClient)
				SYNCClient.Instance.Instantiate(prefab, position, rotation, mode, accuracy);
		}

		public static void Destroy(Object obj) {
			Destroy(SYNCHelperInternal.GetSYNCIdentity(obj));
		}

		public static void Destroy(SYNCIdentity syncIdentity) {
			if (IsServer)
				SYNCServer.Instance.SendObjectDestroy(syncIdentity);
		}

		internal static void SetupComplete() {
			setupComplete?.Invoke();
		}

		internal static void PlayerConnected(int clientID) {
			playerConnected?.Invoke(clientID);
		}

		internal static void PlayerDisconnected(int clientID, DisconnectReason reason) {
			playerDisconnected?.Invoke(clientID, reason);
		}
	}
}
