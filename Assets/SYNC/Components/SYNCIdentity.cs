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
		public int NetID { get; private set; }

		internal event Action<int> NetIDAssigned;

		internal SYNCTransform SyncTransform { get; set; }
		internal SYNCAnimator SyncAnimator { get; set; }
		internal Dictionary<int, SYNCBehaviour> SyncBehaviours { get; } = new Dictionary<int, SYNCBehaviour>();

		internal void AssignNetID(int netID) {
			NetID = netID;
			NetIDAssigned?.Invoke(NetID);
		}

		internal IdentityVarsPack GetVarData() {
			List<BehaviourVarsPack> packs = new List<BehaviourVarsPack>();

			foreach ((int _, SYNCBehaviour syncBehaviour) in SyncBehaviours) {
				BehaviourVarsPack fieldData = syncBehaviour.GetVarData();
				if (fieldData.Vars.Length > 0)
					packs.Add(fieldData);
			}

			return new IdentityVarsPack(NetID, packs.ToArray());
		}

		internal void ApplyVarData(IdentityVarsPack pack) {
			foreach (BehaviourVarsPack behaviourVars in pack.Packs)
				SyncBehaviours[behaviourVars.BehaviourID].ApplyVarData(behaviourVars.Vars);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			SyncBehaviours[msg.BehaviourID].ExecuteRPC(msg);
		}
	}
}
