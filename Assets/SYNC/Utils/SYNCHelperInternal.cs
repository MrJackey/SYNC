using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Sync.Components;
using Sync.Messages;
using Sync.Packs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sync.Utils {
	internal static class SYNCHelperInternal {
		internal static void RegisterNestedTypes(NetPacketProcessor packetProcessor) {
			packetProcessor.RegisterNestedType(TransformPack.Serialize, TransformPack.Deserialize);
			packetProcessor.RegisterNestedType(AnimatorPack.Serialize, AnimatorPack.Deserialize);
			packetProcessor.RegisterNestedType(AnimatorParameterPack.Serialize, AnimatorParameterPack.Deserialize);
			packetProcessor.RegisterNestedType(InstantiatePack.Serialize, InstantiatePack.Deserialize);
			packetProcessor.RegisterNestedType(IdentityVarsPack.Serialize, IdentityVarsPack.Deserialize);
			packetProcessor.RegisterNestedType(BehaviourVarsPack.Serialize, BehaviourVarsPack.Deserialize);
			packetProcessor.RegisterNestedType(Vector3Pack.Serialize, Vector3Pack.Deserialize);
			packetProcessor.RegisterNestedType(Vector2Pack.Serialize, Vector2Pack.Deserialize);
			packetProcessor.RegisterNestedType(ObjectPack.Serialize, ObjectPack.Deserialize);
		}

		internal static SYNCIdentity[] FindExistingIdentities() {
			return GameObject.FindObjectsOfType<SYNCIdentity>(true);
		}

		internal static bool IsAuthorityMine(SYNCIdentity obj) {
			return obj.Authority switch {
				SYNCAuthority.Server => SYNC.IsServer,
				SYNCAuthority.Client => SYNC.IsClient && SYNC.ClientNetID == obj.AuthorityID,
				_ => throw new ArgumentOutOfRangeException(nameof(obj.Authority), obj.Authority, null),
			};
		}

		internal static void SendServerState(NetPacketProcessor packetProcessor, NetPeer peer, uint tick, TransformPack[] transformPacks, AnimatorPack[] animatorPacks, IdentityVarsPack[] varsPacks) {
			const DeliveryMethod Delivery_Method = DeliveryMethod.Unreliable;
			int maxPacketSize = peer.GetMaxSinglePacketSize(Delivery_Method)
			                    - sizeof(ulong) // The NetPacketProcessor adds an ulong hash of 8 bytes onto its own writer
			                    - 2 // Not sure where these 2 bytes are being added to the writer
													- SYNCServerStateMsg.HeaderSize;

			List<SYNCPacket<TransformPack>> transformPackets = DividePacksIntoPackets(transformPacks, maxPacketSize);
			foreach (SYNCPacket<TransformPack> packet in transformPackets)
				packetProcessor.Send(peer, new SYNCServerStateMsg {tick = tick, SYNCTransforms = packet.Content, SYNCAnimators = new AnimatorPack[0], SYNCVars = new IdentityVarsPack[0]}, Delivery_Method);

			List<SYNCPacket<AnimatorPack>> animatorPackets = DividePacksIntoPackets(animatorPacks, maxPacketSize);
			foreach (SYNCPacket<AnimatorPack> packet in animatorPackets)
				packetProcessor.Send(peer, new SYNCServerStateMsg {tick = tick, SYNCTransforms = new TransformPack[0], SYNCAnimators = packet.Content, SYNCVars = new IdentityVarsPack[0]}, Delivery_Method);

			List<SYNCPacket<IdentityVarsPack>> varPackets = DividePacksIntoPackets(varsPacks, maxPacketSize);
			foreach (SYNCPacket<IdentityVarsPack> packet in varPackets)
				packetProcessor.Send(peer, new SYNCServerStateMsg {tick = tick, SYNCTransforms = new TransformPack[0], SYNCAnimators = new AnimatorPack[0], SYNCVars = packet.Content}, Delivery_Method);
		}

		internal static List<SYNCPacket<TPack>> DividePacksIntoPackets<TPack>(IEnumerable<TPack> packs, int maxPacketSize, int initialSize = 0) where TPack : IPack {
			int remainingPacketSize = maxPacketSize - initialSize;
			List<SYNCPacket<TPack>> result = new List<SYNCPacket<TPack>>();
			List<TPack> acc = new List<TPack>();

			foreach (TPack pack in packs) {
				if (remainingPacketSize < pack.ByteSize) {
					result.Add(new SYNCPacket<TPack>(acc.ToArray()));
					acc.Clear();
					remainingPacketSize = maxPacketSize;
				}

				acc.Add(pack);
				remainingPacketSize -= pack.ByteSize;
			}

			if (acc.Count > 0)
				result.Add(new SYNCPacket<TPack>(acc.ToArray()));

			return result;
		}

		internal static ObjectPack[] PackifyObjects(IEnumerable<object> objects) {
			return objects.Select(obj => new ObjectPack(obj)).ToArray();
		}

		internal static (int ID, SYNCIdentity prefab) GetMatchingSyncPrefab(Object obj,  Dictionary<int,SYNCIdentity> prefabs) {
			SYNCIdentity identity = GetSYNCIdentity(obj);
			if (identity == default)
				return default;

			int prefabID = identity.GetInstanceID();
			if (!prefabs.TryGetValue(prefabID, out SYNCIdentity syncIdentity)) {
				Debug.LogError($"[SYNC] Failed to find prefab {obj.name}", obj);
				return default;
			}

			return (prefabID, syncIdentity);
		}

		internal static SYNCIdentity GetSYNCIdentity(Object obj) {
			return obj switch {
				SYNCIdentity syncComp => syncComp,
				GameObject go when go.TryGetComponent(out SYNCIdentity objIdentity) => objIdentity,
				Component comp when comp.TryGetComponent(out SYNCIdentity compIdentity) => compIdentity,
				_ => throw new MissingComponentException($"Object does not have a SYNCIdentity component {obj.name}"),
			};
		}

		// https://en.wikipedia.org/wiki/Cubic_Hermite_spline
		internal static Vector3 EvaluateCubicHermiteSpline(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t) {
			float square = t * t;
			float cubic = square * t;

			float cubic2 = cubic * 2;
			float square3 = square * 3;

			// return (2 * cubic - 3 * square + 1) * p0 + (cubic - 2 * square + t) * m0 + (-2 * cubic + 3 * square) * p1 + (cubic - square) * m1;
			return (cubic2 - square3 + 1) * p0 + (cubic - 2 * square + t) * m0 + (-cubic2 + square3) * p1 + (cubic - square) * m1;
		}
	}
}
