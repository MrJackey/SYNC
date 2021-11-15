using System.Collections.Generic;
using Sync.Components;
using Sync.Packs;
using Sync.Utils;

namespace Sync.Handlers {
	internal static class SYNCTransformHandler {
		private static readonly HashSet<int> _localTransformIDs = new HashSet<int>();

		internal static void Register(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localTransformIDs.Add(identity.NetID);
		}

		internal static void Unregister(SYNCIdentity identity) {
			if (SYNCHelperInternal.IsAuthorityMine(identity))
				_localTransformIDs.Remove(identity.NetID);
		}

		internal static TransformPack[] GetData() {
			TransformPack[] packs = new TransformPack[_localTransformIDs.Count];

			int i = 0;
			foreach (int ID in _localTransformIDs) {
				if (SYNC.IsServer)
					packs[i] = SYNCServer.Instance.SyncIdentities[ID].SyncTransform.GetData();
				else if (SYNC.IsClient)
					packs[i] = SYNCClient.Instance.SyncIdentities[ID].SyncTransform.GetData();

				i++;
			}

			return packs;
		}

		internal static void ApplyData(TransformPack[] msg) {
			foreach (TransformPack pack in msg)
				// TryGetValue is used due to ServerState messages can be received before other such as instantiating
				if (SYNC.IsServer && SYNCServer.Instance.SyncIdentities.TryGetValue(pack.netID, out SYNCIdentity serverIdentity))
					serverIdentity.SyncTransform.ApplyData(pack);
				else if (SYNC.IsClient && SYNCClient.Instance.SyncIdentities.TryGetValue(pack.netID, out SYNCIdentity clientIdentity))
					clientIdentity.SyncTransform.ApplyData(pack);
		}
	}
}
