﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Sync.Handlers;
using Sync.Messages;
using Sync.Utils;
using UnityEngine;

namespace Sync.Components {
	[DefaultExecutionOrder(-1)]
	internal sealed class SYNCClient : MonoBehaviour, INetEventListener {
		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _debugMode;

		private NetManager _client;
		private NetPacketProcessor _packetProcessor = new NetPacketProcessor();
		private int _clientNetID;
		private Dictionary<int, SYNCIdentity> _registeredPrefabs = new Dictionary<int, SYNCIdentity>();

		private uint _lastReceivedServerTick = 0;

		internal static SYNCClient Instance { get; private set; }
		internal Dictionary<int, SYNCIdentity> SyncIdentities { get; } = new Dictionary<int, SYNCIdentity>();
		internal NetPeer Server => _client.FirstPeer;
		public bool IsConnected => _client.FirstPeer is {ConnectionState: ConnectionState.Connected};

		private void Awake() {
			if (Instance == null) {
				Instance = this;
				SYNC.IsClient = true;
			}
			else {
				Debug.LogWarning("[SYNC] Multiple clients detected, destroying last created", gameObject);
				Destroy(this);
			}

			foreach (SYNCIdentity syncIdentity in SYNCHelperInternal.FindExistingIdentities()) {
				if (!SYNC.IsServer)
					syncIdentity.NetID = SYNC.NextNetID;

				SyncIdentities.Add(syncIdentity.NetID, syncIdentity);
			}

			if (_debugMode)
				if (_settings != null)
					InitializeNetwork("127.0.0.1", _settings.port, _settings.password);
				else
					InitializeNetwork("127.0.0.1", 5000, "Debug_key");
		}

		private void InitializeNetwork(string address, int port, string password) {
			if (_settings == null) {
				Debug.LogError("[SERVER] Does not have access to a settings object", gameObject);
				return;
			}

			_client = new NetManager(this);

			RegisterPrefabs();
			_settings.Apply(_client);

			_client.Start();

			_client.Connect(address, port, password);

			SYNCHelperInternal.RegisterNestedTypes(_packetProcessor);

			_packetProcessor.SubscribeReusable<SYNCClientRegisterNetIDMsg, NetPeer>(OnRegisterNetID);
			_packetProcessor.SubscribeReusable<SYNCClientJoinedMsg, NetPeer>(OnClientJoined);
			_packetProcessor.SubscribeReusable<SYNCClientDisconnectMsg, NetPeer>(OnClientDisconnect);
			_packetProcessor.SubscribeReusable<SYNCServerStateMsg, NetPeer>(OnNewServerState);
			_packetProcessor.SubscribeReusable<SYNCObjectInstantiateMsg, NetPeer>(OnObjectInstantiate);
			_packetProcessor.SubscribeReusable<SYNCObjectDestroyMsg, NetPeer>(OnObjectDestroy);
		}

		private void RegisterPrefabs() {
			foreach (SYNCIdentity prefab in _settings.nonPlayerPrefabs)
				_registeredPrefabs.Add(prefab.GetInstanceID(), prefab);
		}

		private void Update() {
			_client.PollEvents();
		}

		private void OnDestroy() {
			_client?.Stop();
			SYNC.IsClient = false;
		}

		internal void Connect(string address, int port, string password, SYNCSettings settings, Action onConnect) {
			_settings = settings;

			StartCoroutine(CoConnectToHost(address, port, password, onConnect));
		}

		private IEnumerator CoConnectToHost(string address, int port, string password, Action onConnect) {
			InitializeNetwork(address, port, password);

			yield return new WaitUntil(() => _client.IsRunning);
			onConnect?.Invoke();
		}

		internal void SendRPC(int netID, int behaviourID, string methodName, object[] args) {
			ObjectPack[] parameters = args.Select(arg => new ObjectPack(arg)).ToArray();
			_packetProcessor.Send(Server, new SYNCRPCMsg() {NetID = netID, BehaviourID = behaviourID, MethodName = methodName, Parameters = parameters}, DeliveryMethod.ReliableOrdered);
		}

		#region Message Callbacks
		private void OnNewServerState(SYNCServerStateMsg msg, NetPeer _) {
			// Skip old packages arriving late
			if (msg.tick >= _lastReceivedServerTick) {
				SYNCTransformHandler.ApplyData(msg.SYNCTransforms);
				_lastReceivedServerTick = msg.tick;
			}
		}

		private void OnObjectInstantiate(SYNCObjectInstantiateMsg msg, NetPeer _) {
			if (!_registeredPrefabs.TryGetValue(msg.PrefabID, out SYNCIdentity obj)) {
				Debug.LogError($"[CLIENT] Received an instantiate message with unknown id: {msg.PrefabID}");
				return;
			}

			if (!SYNC.IsServer) {
				SYNCIdentity syncComponent = Instantiate(obj, msg.Info.Position, msg.Info.Rotation);
				syncComponent.NetID = msg.NetID;
				SyncIdentities.Add(msg.NetID, syncComponent);
			}
		}

		private void OnObjectDestroy(SYNCObjectDestroyMsg msg, NetPeer _) {
			if (!SYNC.IsServer)
				Destroy(SyncIdentities[msg.NetID].gameObject);
			SyncIdentities.Remove(msg.NetID);
		}

		private void OnRegisterNetID(SYNCClientRegisterNetIDMsg msg, NetPeer _) {
			Debug.Log($"[CLIENT] Connected with ClientNetID: {msg.ClientNetID}");
			_clientNetID = msg.ClientNetID;
		}

		private void OnClientJoined(SYNCClientJoinedMsg msg, NetPeer _) {
		}

		private void OnClientDisconnect(SYNCClientDisconnectMsg msg, NetPeer _) {
		}
		#endregion

		#region Network Callbacks
		public void OnPeerConnected(NetPeer peer) {
			Debug.Log($"[CLIENT] Successfully connected to {peer.EndPoint}");
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Debug.Log("[CLIENT] Disconnected because " + disconnectInfo.Reason);
		}

		public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
			Debug.Log("[CLIENT] Error received: " + socketError);
		}

		public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
			_packetProcessor.ReadAllPackets(reader, peer);
		}

		public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

		public void OnConnectionRequest(ConnectionRequest request) { }
		#endregion
	}
}
