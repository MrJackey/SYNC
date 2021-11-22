using System.Collections.Generic;
using Sync.Components;
using Sync.Packs;
using Sync.Utils;

namespace Sync.Handlers {
	public static class SYNCVarHandler {
		private static readonly HashSet<int> _localBehaviourIDs = new HashSet<int>();

		internal static void Register(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localBehaviourIDs.Add(identity.NetID);
		}

		internal static void Unregister(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localBehaviourIDs.Remove(identity.NetID);
		}

		internal static IdentityVarsPack[] GetData() {
			List<IdentityVarsPack> packs = new List<IdentityVarsPack>();

			foreach (int ID in _localBehaviourIDs) {
				IdentityVarsPack identityVarsPack = default;

				if (SYNC.IsServer)
					identityVarsPack = SYNCServer.Instance.SyncIdentities[ID].GetVarData();
				else if (SYNC.IsClient)
					identityVarsPack = SYNCClient.Instance.SyncIdentities[ID].GetVarData();

				if (identityVarsPack.Packs.Length > 0)
					packs.Add(identityVarsPack);
			}

			return packs.ToArray();
		}

		internal static void ApplyData(IdentityVarsPack[] msg) {
			foreach (IdentityVarsPack pack in msg) {
				// TryGetValue is used due to ServerState messages can be received before other such as instantiating
				if (SYNC.IsServer && SYNCServer.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity serverIdentity))
					serverIdentity.ApplyVarData(pack);
				else if (SYNC.IsClient && SYNCClient.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity clientIdentity))
					clientIdentity.ApplyVarData(pack);
			}
		}
	}
}
