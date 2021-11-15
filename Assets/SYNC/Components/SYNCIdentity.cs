﻿using System.Collections.Generic;
using Sync.Messages;
using Sync.Packs;
using Sync.Utils;
using Sync.Utils.Extensions;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Identity")]
	[DisallowMultipleComponent]
	public class SYNCIdentity : MonoBehaviour {
		[SerializeField] private SYNCAuthority _authority = SYNCAuthority.Server;

		private readonly Dictionary<byte, SYNCBehaviour> _syncBehaviours = new Dictionary<byte, SYNCBehaviour>();
		private int _authorityID = -1;
		private byte _behaviourID;

		internal SYNCTransform SyncTransform { get; set; }
		internal SYNCAnimator SyncAnimator { get; set; }

		public int NetID { get; private set; }
		public SYNCAuthority Authority => _authority;

		public int AuthorityID => _authorityID;

		private void Awake() {
			SYNCBehaviour[] behaviours = GetComponents<SYNCBehaviour>();

			foreach (SYNCBehaviour syncBehaviour in behaviours) {
				syncBehaviour.AssignBehaviourID(++_behaviourID);
				_syncBehaviours.Add(_behaviourID, syncBehaviour);
			}
		}

		private void OnDestroy() {
			if (NetID != default) {
				if (SYNC.IsClient && SYNCClient.Instance != null)
					SYNCClient.Instance.SyncIdentities.Remove(NetID);
				if (SYNC.IsServer && SYNCServer.Instance != null)
					SYNCServer.Instance.SyncIdentities.Remove(NetID);
			}
		}

		internal void Setup() {
			if (_authority == SYNCAuthority.Client && !SYNC.IsClient)
				_authority = SYNCAuthority.Server;

			if (SyncTransform != null)
				SyncTransform.RegisterAtHandler();

			if (SyncAnimator != null)
				SyncAnimator.RegisterAtHandler();

			foreach ((byte _, SYNCBehaviour behaviour) in _syncBehaviours) {
				behaviour.RegisterAtHandler();
				behaviour.UpdateEnableStatus();
			}
		}

		internal void AssignNetID(int netID) {
			NetID = netID;
		}

		internal void AssignAuthorityID(int authorityID) {
			_authorityID = authorityID;
		}

		internal IdentityVarsPack GetVarData() {
			List<BehaviourVarsPack> packs = new List<BehaviourVarsPack>();

			foreach ((int _, SYNCBehaviour syncBehaviour) in _syncBehaviours) {
				BehaviourVarsPack fieldData = syncBehaviour.GetVarData();
				if (fieldData.Vars.Length > 0)
					packs.Add(fieldData);
			}

			return new IdentityVarsPack(NetID, packs.ToArray());
		}

		internal void ApplyVarData(IdentityVarsPack pack) {
			foreach (BehaviourVarsPack behaviourVars in pack.Packs)
				_syncBehaviours[behaviourVars.BehaviourID].ApplyVarData(behaviourVars.Vars);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			_syncBehaviours[msg.BehaviourID].ExecuteRPC(msg);
		}
	}
}
