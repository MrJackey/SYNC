using System.Collections.Generic;
using LiteNetLib.Utils;
using Sync.Components;
using UnityEngine;

namespace Sync.Utils {
	internal static class SYNCHelperInternal {
		internal static void RegisterNestedTypes(NetPacketProcessor packetProcessor) {
			packetProcessor.RegisterNestedType(TransformPack.Serialize, TransformPack.Deserialize);
			packetProcessor.RegisterNestedType(InstantiatePack.Serialize, InstantiatePack.Deserialize);
			packetProcessor.RegisterNestedType(Vector3Pack.Serialize, Vector3Pack.Deserialize);
			packetProcessor.RegisterNestedType(Vector2Pack.Serialize, Vector2Pack.Deserialize);
		}

		internal static SYNCIdentity[] FindExistingIdentities() {
			return GameObject.FindObjectsOfType<SYNCIdentity>(true);
		}

		internal static List<SYNCPacket<TPack>> DividePacksIntoPackets<TPack>(IEnumerable<TPack> packs, int maxPacketSize, int initialSize = 0) where TPack : IPack {
			int remainingPacketSize = maxPacketSize - initialSize;
			List<SYNCPacket<TPack>> result = new List<SYNCPacket<TPack>>();
			List<TPack> acc = new List<TPack>();

			foreach (TPack pack in packs) {
				if (remainingPacketSize < pack.Size) {
					result.Add(new SYNCPacket<TPack>(acc.ToArray()));
					acc.Clear();
					remainingPacketSize = maxPacketSize;
				}

				acc.Add(pack);
				remainingPacketSize -= pack.Size;
			}

			if (acc.Count > 0)
				result.Add(new SYNCPacket<TPack>(acc.ToArray()));

			return result;
		}
	}
}
