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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Server")]
	[DefaultExecutionOrder(-2)]
	internal sealed class SYNCServer : MonoBehaviour, INetEventListener {
		internal static SYNCServer Instance { get; private set; }

		[SerializeField] private SYNCSettings _settings;
		[SerializeField] private bool _hostOnStart;
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

			AssignNetIDs();
		}

		private void Start() {
			if (_hostOnStart)
				if (_settings != null) {
					_password = _settings.serverKey;
					InitializeNetwork(_settings.port, _settings.sendRate);

					if (!SYNC.IsClient)
						SYNC.SetupComplete();
				}
				else {
					Debug.LogError("[SERVER] Server requires a settings object when starting on awake", gameObject);
				}

			SceneManager.sceneLoaded += OnSceneLoaded;
			DontDestroyOnLoad(gameObject);
		}

		private void OnSceneLoaded(Scene _, LoadSceneMode __) => AssignNetIDs();

		private void AssignNetIDs() {
			foreach (SYNCIdentity syncIdentity in SYNCHelperInternal.FindExistingIdentities()) {
				if (syncIdentity.NetID != default) continue;

				syncIdentity.AssignNetID(SYNC.IncrementNetID());
				SyncIdentities.Add(syncIdentity.NetID, syncIdentity);
			}
		}

		private void InitializeNetwork(int port, int sendRate) {
			_server = new NetManager(this);

			RegisterPrefabs();
			if (_settings != null)
				_settings.Apply(_server);

			_server.Start(port);

			tickTimer = new SYNCTickTimer(sendRate);

			SYNCHelperInternal.RegisterNestedTypes(_packetProcessor);

			_packetProcessor.SubscribeReusable<SYNCRPCMsg, NetPeer>(OnRPC);
			_packetProcessor.SubscribeReusable<SYNCServerStateMsg, NetPeer>(OnServerState);
			_packetProcessor.SubscribeReusable<SYNCObjectInstantiateMsg, NetPeer>(OnObjectInstantiate);
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
			Instance = null;
			SceneManager.sceneLoaded -= OnSceneLoaded;
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

		internal void Shutdown() {
			if (!IsRunning) return;

			_server.Stop();
			Destroy(this);
		}

		private void OnRPC(SYNCRPCMsg msg, NetPeer _) {
			SyncIdentities[msg.NetID].ExecuteRPC(msg);
		}

		private void OnServerState(SYNCServerStateMsg msg, NetPeer peer) {
			if (!SYNC.IsClient || peer.Id != SYNC.ClientNetID) {
				SYNCTransformHandler.ApplyData(msg.SYNCTransforms);
				SYNCAnimatorHandler.ApplyData(msg.SYNCAnimators);
				SYNCVarHandler.ApplyData(msg.SYNCVars);
			}

			foreach (NetPeer connectedPeer in _server.ConnectedPeerList) {
				if (connectedPeer.Id == peer.Id) continue;
				if (SYNC.IsClient && connectedPeer.Id == SYNC.ClientNetID) continue;

				_packetProcessor.Send(connectedPeer, msg, DeliveryMethod.Unreliable);
			}
		}

		private void OnObjectInstantiate(SYNCObjectInstantiateMsg msg, NetPeer peer) {
			if (!_registeredPrefabs.TryGetValue(msg.PrefabID, out SYNCIdentity prefab)) {
				Debug.LogError($"[Server] Received an instantiate message with unknown id: {msg.PrefabID}");
				return;
			}

			SYNCIdentity syncComponent;
			if ((msg.Info.options & SYNCInstantiateOptions.Parent) != 0 || (msg.Info.options & SYNCInstantiateOptions.ParentWorldSpace) != 0)
				syncComponent = Instantiate(prefab, SyncIdentities[msg.Info.Parent].transform, (msg.Info.options & SYNCInstantiateOptions.ParentWorldSpace) != 0);
			else
				syncComponent = Instantiate(prefab, msg.Info.Position, msg.Info.Rotation);

			syncComponent.AssignNetID(SYNC.IncrementNetID());
			syncComponent.AssignAuthorityID(msg.Info.ClientAuthorityID);
			SyncIdentities.Add(syncComponent.NetID, syncComponent);
			msg.NetID = syncComponent.NetID;

			if (SYNC.IsClient)
				SYNCClient.Instance.SyncIdentities.Add(syncComponent.NetID, syncComponent);

			syncComponent.ManualRegistration();

			foreach (NetPeer connectedPeer in _server.ConnectedPeerList) {
				if (SYNC.IsClient && connectedPeer.Id == SYNC.ClientNetID) continue;

				_packetProcessor.Send(connectedPeer, msg, DeliveryMethod.ReliableOrdered);
			}
		}

		#region Message Senders
		private void SendServerState() {
			TransformPack[] syncTransforms = SYNCTransformHandler.GetData();
			AnimatorPack[] syncAnimators = SYNCAnimatorHandler.GetData();
			IdentityVarsPack[] identityVars = SYNCVarHandler.GetData();

			foreach (NetPeer peer in _server.ConnectedPeerList) {
				if (SYNC.IsClient && peer.Id == SYNC.ClientNetID) continue;

				SYNCHelperInternal.SendServerState(
					_packetProcessor,
					peer,
					_serverTick,
					syncTransforms,
					syncAnimators,
					identityVars
				);
			}
		}

		internal void Instantiate(Object obj, Vector3 position, Quaternion rotation, SYNCInstantiateMode mode, SYNCFloatAccuracy accuracy) {
			(int prefabID, SYNCIdentity prefab) = SYNCHelperInternal.GetMatchingSyncPrefab(obj, _registeredPrefabs);

			if (prefab == null) {
				Debug.LogError($"[SERVER] Trying to instantiate an object which does not have an SYNCIdentity: {obj.name}", obj);
				return;
			}

			SYNCIdentity syncComponent = Instantiate(prefab, position, rotation);

			SYNCInstantiateOptions instantiateOptions = (SYNCInstantiateOptions)((ushort)mode | (ushort)accuracy);

			if (syncComponent.Authority == SYNCAuthority.Client && SYNC.IsClient) {
				syncComponent.AssignAuthorityID(SYNC.ClientNetID);
				instantiateOptions |= SYNCInstantiateOptions.ClientAuth;
			}
			else {
				syncComponent.Authority = SYNCAuthority.Server;
			}

			InstantiatePack pack = new InstantiatePack(
				position,
				rotation,
				instantiateOptions,
				syncComponent.AuthorityID
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

			SYNCInstantiateOptions instantiateOptions = instantiateInWorldSpace ? SYNCInstantiateOptions.ParentWorldSpace : SYNCInstantiateOptions.Parent;

			if (syncComponent.Authority == SYNCAuthority.Client && SYNC.IsClient) {
				syncComponent.AssignAuthorityID(SYNC.ClientNetID);
				instantiateOptions |= SYNCInstantiateOptions.ClientAuth;
			}

			InstantiatePack pack = new InstantiatePack(
				parentNetID,
				instantiateOptions,
				syncComponent.AuthorityID
			);

			SendObjectInstantiate(pack, syncComponent, prefabID);
		}

		private void SendObjectInstantiate(InstantiatePack pack, SYNCIdentity syncComponent, int prefabID) {
			syncComponent.AssignNetID(SYNC.IncrementNetID());
			SyncIdentities.Add(syncComponent.NetID, syncComponent);
			syncComponent.ManualRegistration();

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
				request.Accept();
			else if (_server.ConnectedPeersCount < _settings.maxAllowedClients || _settings.maxAllowedClients == -1)
				request.AcceptIfKey(_password);
			else
				request.Reject();
		}
		#endregion
	}
}
