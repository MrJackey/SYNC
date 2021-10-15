using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using SYNC.Messages;
using SYNC.Utils;
using UnityEngine;

namespace SYNC.Components {
	internal sealed class SYNCClient : MonoBehaviour, INetEventListener {
		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _debugMode;

		private NetManager _client;
		private NetPacketProcessor _packetProcessor = new NetPacketProcessor();
		private int _clientNetID;

		internal static SYNCClient Instance { get; private set; }
		internal NetPeer Server => _client.FirstPeer;
		public bool IsConnected => _client.FirstPeer is {ConnectionState: ConnectionState.Connected};

		private void Awake() {
			if (Instance == null) {
				Instance = this;
			}
			else {
				Debug.LogWarning("[SYNC] Multiple clients detected, destroying last created", gameObject);
				Destroy(this);
			}
		}

		private void Start() {
			if (!SYNC.IsServer)
				SYNCTransformHandler.Initialize();

			InitializeNetwork();
		}

		private void InitializeNetwork() {
			_client = new NetManager(this);

			if (_settings != null)
				_settings.Apply(_client);

			_client.Start();

			if (_debugMode)
				_client.Connect("127.0.0.1", _settings != null ? _settings.port : 5000, "sample_app");

			SYNCHelperInternal.RegisterNestedTypes(_packetProcessor);

			_packetProcessor.SubscribeReusable<SYNCClientRegisterNetIDMsg, NetPeer>(OnRegisterNetID);
			_packetProcessor.SubscribeReusable<SYNCClientJoinedMsg, NetPeer>(OnClientJoined);
			_packetProcessor.SubscribeReusable<SYNCClientDisconnectMsg, NetPeer>(OnClientDisconnect);
			_packetProcessor.SubscribeReusable<SYNCServerStateMsg, NetPeer>(OnNewServerState);
		}

		private void Update() {
			_client.PollEvents();
		}

		private void OnDestroy() {
			_client?.Stop();
		}

		#region Message Callbacks
		private void OnNewServerState(SYNCServerStateMsg msg, NetPeer _) {
			SYNCTransformHandler.ApplyData(msg.SYNCTransforms);
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
