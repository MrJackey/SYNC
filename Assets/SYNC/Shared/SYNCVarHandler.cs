using System.Collections.Generic;
using Sync.Components;
using Sync.Packs;

namespace Sync.Handlers {
	public static class SYNCVarHandler {
		private static HashSet<int> _behaviourIDs = new HashSet<int>();

		internal static void Register(int syncIdentityNetID) {
			_behaviourIDs.Add(syncIdentityNetID);
		}

		internal static void Unregister(int syncIdentityNetID) {
			_behaviourIDs.Remove(syncIdentityNetID);
		}

		internal static IdentityVarsPack[] GetData() {
			List<IdentityVarsPack> packs = new List<IdentityVarsPack>();

			foreach (int ID in _behaviourIDs) {
				IdentityVarsPack identityVarsPack = SYNCServer.Instance.SyncIdentities[ID].GetVarData();
				if (identityVarsPack.Packs.Length > 0)
					packs.Add(identityVarsPack);
			}

			return packs.ToArray();
		}

		internal static void ApplyData(IdentityVarsPack[] msg) {
			foreach (IdentityVarsPack pack in msg)
				if (SYNCClient.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity syncIdentity))
					syncIdentity.ApplyVarData(pack);
		}
	}
}
