using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using SYNC.Messages;
using SYNC.Utils;
using UnityEngine;

namespace SYNC.Components {
	internal sealed class SYNCServer : MonoBehaviour, INetEventListener {
		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _debugMode;

		private NetManager _server;
		private NetPacketProcessor _packetProcessor = new NetPacketProcessor();

		private SYNCTickTimer tickTimer;

		internal static SYNCServer Instance { get; private set; }

		private void Awake() {
			if (Instance == null) {
				Instance = this;
			}
			else {
				Debug.LogWarning("[SYNC] Multiple servers detected, destroying last created", gameObject);
				Destroy(this);
			}
		}

		private void Start() {
			SYNCTransformHandler.Initialize();
			InitializeNetwork();
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
				SendServerState();
				tickTimer.Restart();
			}
		}

		private void SendServerState() {
			SYNCServerStateMsg msg = new SYNCServerStateMsg {
				SYNCTransforms = SYNCTransformHandler.GetData(),
			};

			_packetProcessor.Send(_server, msg, DeliveryMethod.ReliableSequenced);
		}

		private void OnDestroy() {
			_server?.Stop();
		}

		#region Network Callbacks
		public void OnPeerConnected(NetPeer peer) {
			Debug.Log("[SERVER] New peer connected: " + peer.EndPoint);
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
