using System;
using System.Collections.Generic;
using Sync.Messages;
using Sync.Packs;
using Sync.Utils.Extensions;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Identity")]
	[DisallowMultipleComponent]
	public class SYNCIdentity : MonoBehaviour {
		private Dictionary<byte, SYNCBehaviour> syncBehaviours = new Dictionary<byte, SYNCBehaviour>();
		private byte behaviourID;

		internal event Action<int> NetIDAssigned;

		internal SYNCTransform SyncTransform { get; set; }
		internal SYNCAnimator SyncAnimator { get; set; }

		public int NetID { get; private set; }

		private void Awake() {
			SYNCBehaviour[] behaviours = GetComponents<SYNCBehaviour>();

			foreach (SYNCBehaviour syncBehaviour in behaviours) {
				syncBehaviour.AssignBehaviourID(++behaviourID);
				syncBehaviours.Add(behaviourID, syncBehaviour);
			}
		}

		internal void AssignNetID(int netID) {
			NetID = netID;
			NetIDAssigned?.Invoke(NetID);
		}

		internal IdentityVarsPack GetVarData() {
			List<BehaviourVarsPack> packs = new List<BehaviourVarsPack>();

			foreach ((int _, SYNCBehaviour syncBehaviour) in syncBehaviours) {
				BehaviourVarsPack fieldData = syncBehaviour.GetVarData();
				if (fieldData.Vars.Length > 0)
					packs.Add(fieldData);
			}

			return new IdentityVarsPack(NetID, packs.ToArray());
		}

		internal void ApplyVarData(IdentityVarsPack pack) {
			foreach (BehaviourVarsPack behaviourVars in pack.Packs)
				syncBehaviours[behaviourVars.BehaviourID].ApplyVarData(behaviourVars.Vars);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			syncBehaviours[msg.BehaviourID].ExecuteRPC(msg);
		}
	}
}
