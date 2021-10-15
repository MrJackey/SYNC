using System.Collections.Generic;
using SYNC.Components;
using SYNC.Utils;

namespace SYNC {
	internal static class SYNCTransformHandler {
		private static HashSet<int> _transformIDs = new HashSet<int>();

		internal static void Register(int syncIdentityNetID) {
			_transformIDs.Add(syncIdentityNetID);
		}

		internal static void UnRegister(int syncIdentityNetID) {
			_transformIDs.Remove(syncIdentityNetID);
		}

		internal static TransformPack[] GetData() {
			TransformPack[] packs = new TransformPack[_transformIDs.Count];

			int i = 0;
			foreach (int ID in _transformIDs) {
				packs[i] = SYNCServer.Instance.SyncIdentities[ID].SYNCTransform.GetData();
				i++;
			}

			return packs;
		}

		internal static void ApplyData(TransformPack[] msg) {
			foreach (TransformPack pack in msg)
				// Workaround due to ServerState messages appearing before instantiating
				if (SYNCClient.Instance.SyncIdentities.TryGetValue(pack.netID, out SYNCIdentity syncIdentity))
					syncIdentity.SYNCTransform.ApplyData(pack);
		}
	}
}
