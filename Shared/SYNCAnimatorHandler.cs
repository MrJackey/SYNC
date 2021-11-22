using System.Collections.Generic;
using Sync.Components;
using Sync.Packs;
using Sync.Utils;

namespace Sync.Handlers {
	public static class SYNCAnimatorHandler {
		private static readonly HashSet<int> _localAnimatorIDs = new HashSet<int>();

		internal static void Register(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localAnimatorIDs.Add(identity.NetID);
		}

		internal static void Unregister(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localAnimatorIDs.Remove(identity.NetID);
		}

		internal static AnimatorPack[] GetData() {
			AnimatorPack[] packs = new AnimatorPack[_localAnimatorIDs.Count];

			int i = 0;
			foreach (int ID in _localAnimatorIDs) {
				if (SYNC.IsServer)
					packs[i] = SYNCServer.Instance.SyncIdentities[ID].SyncAnimator.GetData();
				else if (SYNC.IsClient)
					packs[i] = SYNCClient.Instance.SyncIdentities[ID].SyncAnimator.GetData();

				i++;
			}

			return packs;
		}

		internal static void ApplyData(AnimatorPack[] msg) {
			foreach (AnimatorPack pack in msg) {
				// TryGetValue is used due to ServerState messages can be received before other such as instantiating
				if (SYNC.IsServer && SYNCServer.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity serverIdentity))
					serverIdentity.SyncAnimator.ApplyData(pack);
				else if (SYNC.IsClient && SYNCClient.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity clientIdentity))
					clientIdentity.SyncAnimator.ApplyData(pack);
			}
		}
	}
}
