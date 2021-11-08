using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Sync.Handlers;
using Sync.Messages;
using Sync.Packs;
using Sync.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Server")]
	[DefaultExecutionOrder(-2)]
	internal sealed class SYNCServer : MonoBehaviour, INetEventListener {
		internal static SYNCServer Instance { get; private set; }

		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _startOnAwake;
		[SerializeField] private bool _debugMode;

		private NetManager _server;
		private NetPacketProcessor _packetProcessor = new NetPacketProcessor();

		private SYNCTickTimer tickTimer;
		private Dictionary<int, SYNCIdentity> _registeredPrefabs = new Dictionary<int, SYNCIdentity>();
		private uint _serverTick;
		private string _password;

		internal Dictionary<int, SYNCIdentity> SyncIdentities { get; } = new Dictionary<int, SYNCIdentity>();
		internal bool IsRunning => _server is {IsRunning: true};

		private void Awake() {
			if (Instance == null) {
				Instance = this;
				SYNC.IsServer = true;
			}
			else {
				Debug.LogWarning("[SYNC] Multiple servers detected, destroying last created", gameObject);
				Destroy(this);
			}

			foreach (SYNCIdentity syncIdentity in SYNCHelperInternal.FindExistingIdentities()) {
				syncIdentity.AssignNetID(SYNC.GetNextNetID());
				SyncIdentities.Add(syncIdentity.NetID, syncIdentity);
			}

			if (_startOnAwake)
				if (_settings != null)
					InitializeNetwork(_settings.port, _settings.sendRate);
				else
					InitializeNetwork(5000, 60);
		}

		private void InitializeNetwork(int port, int sendRate) {
			if (_settings == null) {
				Debug.LogError("[SERVER] Does not have access to a settings object", gameObject);
				return;
			}

			_server = new NetManager(this);

			RegisterPrefabs();
			_settings.Apply(_server);

			_server.Start(port);

			tickTimer = new SYNCTickTimer(sendRate);

			SYNCHelperInternal.RegisterNestedTypes(_packetProcessor);

			_packetProcessor.SubscribeReusable<SYNCRPCMsg>(OnRPC);
		}

		private void RegisterPrefabs() {
			foreach (SYNCIdentity prefab in _settings.nonPlayerPrefabs)
				_registeredPrefabs.Add(prefab.GetInstanceID(), prefab);
		}

		private void Update() {
			_server.PollEvents();

			if (tickTimer.Elapsed) {
				_serverTick++;
				SendServerState();
				tickTimer.Restart();
			}
		}

		private void OnDestroy() {
			_server?.Stop();
			SYNC.IsServer = false;
		}

		internal void Host(string password, SYNCSettings settings, SYNCPlayerConnectedCallback onConnect) {
			_settings = settings;
			_password = password;

			InitializeNetwork(settings.port, settings.sendRate);
			StartCoroutine(CoConnectToSelf(password, settings, onConnect));
		}

		private IEnumerator CoConnectToSelf(string password, SYNCSettings settings, SYNCPlayerConnectedCallback onConnect) {
			yield return new WaitUntil(() => _server.IsRunning);

			SYNC.Connect("127.0.0.1", _settings.port, password, settings, onConnect);
		}

		private void OnRPC(SYNCRPCMsg msg) {
			SyncIdentities[msg.NetID].ExecuteRPC(msg);
		}

		#region Message Senders
		private void SendServerState() {
			TransformPack[] syncTransforms = SYNCTransformHandler.GetData();
			AnimatorPack[] syncAnimators = SYNCAnimatorHandler.GetData();
			IdentityVarsPack[] identityVars = SYNCVarHandler.GetData();
			const DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable;

			foreach (NetPeer peer in _server.ConnectedPeerList) {
				int maxPacketSize = peer.GetMaxSinglePacketSize(deliveryMethod)
				                    - sizeof(ulong) // The NetPacketProcessor adds an ulong hash of 8 bytes onto its own writer
				                    - 2 // Not sure where these 2 bytes are being added to the writer
				                    - SYNCServerStateMsg.HeaderSize;

				List<SYNCPacket<TransformPack>> transformPackets = SYNCHelperInternal.DividePacksIntoPackets(syncTransforms, maxPacketSize);
				foreach (SYNCPacket<TransformPack> packet in transformPackets)
					_packetProcessor.Send(peer, new SYNCServerStateMsg {tick = _serverTick, SYNCTransforms = packet.Content, SYNCAnimators = new AnimatorPack[0], SYNCVars = new IdentityVarsPack[0]}, deliveryMethod);

				List<SYNCPacket<AnimatorPack>> animatorPackets = SYNCHelperInternal.DividePacksIntoPackets(syncAnimators, maxPacketSize);
				foreach (SYNCPacket<AnimatorPack> packet in animatorPackets)
					_packetProcessor.Send(peer, new SYNCServerStateMsg {tick = _serverTick, SYNCTransforms = new TransformPack[0], SYNCAnimators = packet.Content, SYNCVars = new IdentityVarsPack[0]}, deliveryMethod);

				List<SYNCPacket<IdentityVarsPack>> varPackets = SYNCHelperInternal.DividePacksIntoPackets(identityVars, maxPacketSize);
				foreach (SYNCPacket<IdentityVarsPack> packet in varPackets)
					_packetProcessor.Send(peer, new SYNCServerStateMsg {tick = _serverTick, SYNCTransforms = new TransformPack[0], SYNCAnimators = new AnimatorPack[0], SYNCVars = packet.Content}, deliveryMethod);
			}
		}

		internal void Instantiate(Object obj, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			(int prefabID, SYNCIdentity prefab) = SYNCHelperInternal.GetMatchingSyncPrefab(obj, _registeredPrefabs);

			if (prefab == null) {
				Debug.LogError($"[SERVER] Trying to instantiate an object which does not have an SYNCIdentity: {obj.name}", obj);
				return;
			}

			SYNCIdentity syncComponent = Instantiate(prefab, position, rotation);

			InstantiatePack pack = new InstantiatePack(
				position,
				rotation,
				(SYNCInstantiateOptions)((ushort)mode | (ushort)accuracy)
			);

			SendObjectInstantiate(pack, syncComponent, prefabID);
		}

		internal void Instantiate(Object obj, int parentNetID, bool instantiateInWorldSpace) {
			(int prefabID, SYNCIdentity prefab) = SYNCHelperInternal.GetMatchingSyncPrefab(obj, _registeredPrefabs);

			if (prefab == null) {
				Debug.LogError($"[SERVER] Trying to instantiate an object which does not have an SYNCIdentity: {obj.name}", obj);
				return;
			}

			SYNCIdentity syncComponent = Instantiate(prefab, SyncIdentities[parentNetID].transform, instantiateInWorldSpace);

			InstantiatePack pack = new InstantiatePack(
				parentNetID,
				instantiateInWorldSpace ? SYNCInstantiateOptions.ParentWorldSpace : SYNCInstantiateOptions.Parent
			);

			SendObjectInstantiate(pack, syncComponent, prefabID);
		}

		private void SendObjectInstantiate(InstantiatePack pack, SYNCIdentity syncComponent, int prefabID) {
			syncComponent.AssignNetID(SYNC.GetNextNetID());
			SyncIdentities.Add(syncComponent.NetID, syncComponent);

			if (SYNC.IsClient)
				SYNCClient.Instance.SyncIdentities.Add(syncComponent.NetID, syncComponent);

			SYNCObjectInstantiateMsg msg = new SYNCObjectInstantiateMsg {NetID = syncComponent.NetID, PrefabID = prefabID, Info = pack};

			_packetProcessor.Send(_server, msg, DeliveryMethod.ReliableOrdered);
		}

		internal void SendObjectDestroy(SYNCIdentity obj) {
			SYNCObjectDestroyMsg msg = new SYNCObjectDestroyMsg {NetID = obj.NetID};
			Destroy(SyncIdentities[msg.NetID].gameObject);
			_packetProcessor.Send(_server, msg, DeliveryMethod.ReliableOrdered);
		}

		public void SendRPC(int clientID, int netID, byte behaviourID, string methodName, object[] args) {
			ObjectPack[] parameters = SYNCHelperInternal.PackifyObjects(args);
			_packetProcessor.Send(_server.GetPeerById(clientID), new SYNCRPCMsg() {NetID = netID, BehaviourID = behaviourID, MethodName = methodName, Parameters = parameters}, DeliveryMethod.ReliableOrdered);
		}

		public void SendRPC(int netID, byte behaviourID, string methodName, object[] args) {
			ObjectPack[] parameters = SYNCHelperInternal.PackifyObjects(args);
			_packetProcessor.Send(_server, new SYNCRPCMsg() {NetID = netID, BehaviourID = behaviourID, MethodName = methodName, Parameters = parameters}, DeliveryMethod.ReliableOrdered);
		}
		#endregion

		#region Network Callbacks
		public void OnPeerConnected(NetPeer peer) {
			Debug.Log("[SERVER] New peer connected: " + peer.EndPoint);
			_packetProcessor.Send(peer, new SYNCClientRegisterNetIDMsg {ClientNetID = peer.Id}, DeliveryMethod.ReliableOrdered);

			foreach (NetPeer connectedPeer in _server.ConnectedPeerList) {
				if (connectedPeer.Id == peer.Id) continue;
				if (SYNC.IsClient && SYNCClient.Instance.ClientNetID == connectedPeer.Id) continue;

				_packetProcessor.Send(connectedPeer, new SYNCClientJoinedMsg {ClientNetID = peer.Id}, DeliveryMethod.ReliableOrdered);
			}

			if (!SYNC.IsClient || SYNCClient.Instance.ClientNetID != peer.Id)
				SYNC.PlayerConnected(peer.Id);
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Debug.Log($"[SERVER] Peer disconnected {peer.EndPoint}, info: " + disconnectInfo.Reason);

			foreach (NetPeer connectedPeer in _server.ConnectedPeerList) {
				if (SYNC.IsClient && SYNCClient.Instance.ClientNetID == connectedPeer.Id) continue;

				_packetProcessor.Send(connectedPeer, new SYNCClientDisconnectMsg {ClientNetID = peer.Id, Reason = disconnectInfo.Reason}, DeliveryMethod.ReliableOrdered);
			}

			SYNC.PlayerDisconnected(peer.Id, disconnectInfo.Reason);
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
			if (_debugMode)
				request.AcceptIfKey(_settings.password);
			else
				request.AcceptIfKey(_password);
		}
		#endregion
	}
}
