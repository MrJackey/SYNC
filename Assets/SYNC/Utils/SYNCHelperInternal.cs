using LiteNetLib.Utils;
using SYNC.Components;
using UnityEngine;

namespace SYNC.Utils {
	internal static class SYNCHelperInternal {
		internal static void RegisterNestedTypes(NetPacketProcessor packetProcessor) {
			packetProcessor.RegisterNestedType(TransformPack.Serialize, TransformPack.Deserialize);
			packetProcessor.RegisterNestedType(Vector3Pack.Serialize, Vector3Pack.Deserialize);
			packetProcessor.RegisterNestedType(Vector2Pack.Serialize, Vector2Pack.Deserialize);
		}

		internal static SYNCIdentity[] FindExistingIdentities() {
			return GameObject.FindObjectsOfType<SYNCIdentity>(true);
		}
	}
}
