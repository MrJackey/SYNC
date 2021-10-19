using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using SYNC.Messages;
using SYNC.Utils;
using UnityEngine;

namespace SYNC.Components {
	[DefaultExecutionOrder(-2)]
	internal sealed class SYNCServer : MonoBehaviour, INetEventListener {
		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _debugMode;

		private NetManager _server;
		private NetPacketProcessor _packetProcessor = new NetPacketProcessor();

		private SYNCTickTimer tickTimer;
		private Dictionary<int, SYNCIdentity> _registeredPrefabs = new Dictionary<int, SYNCIdentity>();
		private uint _serverTick;

		internal Dictionary<int, SYNCIdentity> SyncIdentities { get; } = new Dictionary<int, SYNCIdentity>();
		internal static SYNCServer Instance { get; private set; }

		private void Awake() {
			if (Instance == null) {
				Instance = this;
				SYNC.IsServer = true;
			}
			else {
				Debug.LogWarning("[SYNC] Multiple servers detected, destroying last created", gameObject);
				Destroy(this);
			}
		}

		private void Start() {
			foreach (SYNCIdentity syncIdentity in SYNCHelperInternal.FindExistingIdentities()) {
				syncIdentity.NetID = SYNC.NextNetID;
				SyncIdentities.Add(syncIdentity.NetID, syncIdentity);
			}

			if (_settings != null)
				RegisterPrefabs();

			InitializeNetwork();
		}

		private void RegisterPrefabs() {
			foreach (SYNCIdentity prefab in _settings.nonPlayerPrefabs)
				_registeredPrefabs.Add(prefab.GetInstanceID(), prefab);
		}

		private void InitializeNetwork() {
			_server = new NetManager(this);
			if (_settings != null)
				_settings.Apply(_server);

			if (_debugMode)
				_server.Start(_settings != null ? _settings.port : 5000);

			tickTimer = new SYNCTickTimer(_settings ? _settings.tickRate : (short)60);

			SYNCHelperInternal.RegisterNestedTypes(_packetProcessor);
		}

		private void Update() {
			_server.PollEvents();

			if (tickTimer.Elapsed) {
				_serverTick++;
				SendServerState();
				tickTimer.Restart();
			}
		}

		private void SendServerState() {
			TransformPack[] syncTransforms = SYNCTransformHandler.GetData();
			const DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable;

			foreach (NetPeer peer in _server.ConnectedPeerList) {
				int maxPacketSize = peer.GetMaxSinglePacketSize(deliveryMethod) - SYNCServerStateMsg.HeaderSize;

				List<SYNCPacket<TransformPack>> packets = SYNCHelperInternal.DividePacksIntoPackets(syncTransforms, maxPacketSize);
				foreach (SYNCPacket<TransformPack> packet in packets)
					_packetProcessor.Send(peer, new SYNCServerStateMsg {tick = _serverTick, SYNCTransforms = packet.Content}, deliveryMethod);
			}
		}

		internal void SendObjectInstantiate(Object prefab, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			int prefabID = prefab.GetInstanceID();
			if (!_registeredPrefabs.TryGetValue(prefabID, out SYNCIdentity obj)) {
				Debug.LogError($"[SERVER] Trying to instantiate an object which is not registered: {prefab.name}", prefab);
				return;
			}

			SYNCIdentity syncComponent = Instantiate(obj, position, rotation);
			syncComponent.NetID = SYNC.NextNetID;
			SyncIdentities.Add(syncComponent.NetID, syncComponent);

			if (SYNC.IsClient)
				SYNCClient.Instance.SyncIdentities.Add(syncComponent.NetID, syncComponent);

			InstantiatePack pack = new InstantiatePack(
				position,
				rotation,
				(SYNCInstantiateOptions)((ushort)mode | (ushort)accuracy)
			);

			SYNCObjectInstantiateMsg msg = new SYNCObjectInstantiateMsg {NetID = syncComponent.NetID, PrefabID = prefabID, Info = pack};

			_packetProcessor.Send(_server, msg, DeliveryMethod.ReliableOrdered);
		}

		internal void SendObjectDestroy(SYNCIdentity obj) {
			SYNCObjectDestroyMsg msg = new SYNCObjectDestroyMsg {NetID = obj.NetID};
			Destroy(SyncIdentities[msg.NetID].gameObject);
			_packetProcessor.Send(_server, msg, DeliveryMethod.ReliableOrdered);
		}

		private void OnDestroy() {
			_server?.Stop();
		}

		#region Network Callbacks
		public void OnPeerConnected(NetPeer peer) {
			Debug.Log("[SERVER] New peer connected: " + peer.EndPoint);
			_packetProcessor.Send(peer, new SYNCClientRegisterNetIDMsg {ClientNetID = peer.Id}, DeliveryMethod.ReliableOrdered);

			foreach (NetPeer connectedPeer in _server.ConnectedPeerList) {
				if (connectedPeer.Id == peer.Id) continue;

				_packetProcessor.Send(connectedPeer, new SYNCClientJoinedMsg {ClientNetID = peer.Id}, DeliveryMethod.ReliableOrdered);
			}
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Debug.Log($"[SERVER] Peer disconnected {peer.EndPoint}, info: " + disconnectInfo.Reason);
		}

		public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
			Debug.Log($"[SERVER] NetworkError: {socketError}");
		}

		public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
			_packetProcessor.ReadAllPackets(reader, peer);
		}

		public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

		public void OnConnectionRequest(ConnectionRequest request) {
			request.AcceptIfKey("sample_app");
		}
		#endregion
	}
}
