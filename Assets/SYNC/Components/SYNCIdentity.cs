using System;
using System.Collections.Generic;
using Sync.Messages;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Identity")]
	[DisallowMultipleComponent]
	public class SYNCIdentity : MonoBehaviour {
		internal int NetID { get; private set; }

		internal event Action<int> NetIDAssigned;

		internal SYNCTransform SYNCTransform { get; set; }
		internal Dictionary<int, SYNCBehaviour> SYNCBehaviours { get; } = new Dictionary<int, SYNCBehaviour>();

		internal void AssignNetID(int netID) {
			NetID = netID;
			NetIDAssigned?.Invoke(NetID);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			SYNCBehaviours[msg.BehaviourID].ExecuteRPC(msg);
		}
	}
}
