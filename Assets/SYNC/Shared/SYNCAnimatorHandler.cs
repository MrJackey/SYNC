using System.Collections.Generic;
using Sync.Components;
using Sync.Utils;

namespace Sync.Handlers {
	public static class SYNCAnimatorHandler {
		private static HashSet<int> _animatorIDs = new HashSet<int>();

		internal static void Register(int syncIdentityNetID) {
			_animatorIDs.Add(syncIdentityNetID);
		}

		internal static void Unregister(int syncIdentityNetID) {
			_animatorIDs.Remove(syncIdentityNetID);
		}

		internal static AnimatorPack[] GetData() {
			AnimatorPack[] packs = new AnimatorPack[_animatorIDs.Count];

			int i = 0;
			foreach (int ID in _animatorIDs) {
				packs[i] = SYNCServer.Instance.SyncIdentities[ID].SyncAnimator.GetData();
				i++;
			}

			return packs;
		}

		internal static void ApplyData(AnimatorPack[] msg) {
			foreach (AnimatorPack pack in msg)
				// Workaround due to ServerState messages appearing before instantiating
				if (SYNCClient.Instance.SyncIdentities.TryGetValue(pack.NetID, out SYNCIdentity syncIdentity))
					syncIdentity.SyncAnimator.ApplyData(pack);
		}
	}
}
