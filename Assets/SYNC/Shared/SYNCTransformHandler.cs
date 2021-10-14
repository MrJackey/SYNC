using System.Collections.Generic;
using SYNC.Components;
using SYNC.Utils;
using UnityEngine;

namespace SYNC {
	internal static class SYNCTransformHandler {
		private static Dictionary<int, SYNCTransform> _transforms = new Dictionary<int, SYNCTransform>();

		internal static void Initialize() {
			SYNCTransform[] syncTransforms = GameObject.FindObjectsOfType<SYNCTransform>();

			foreach (SYNCTransform syncTransform in syncTransforms) {
				syncTransform.NetID = SYNC.NextNetID;
				_transforms.Add(syncTransform.NetID, syncTransform);
			}
		}

		internal static TransformPack[] GetData() {
			TransformPack[] packs = new TransformPack[_transforms.Count];

			int i = 0;
			foreach (SYNCTransform syncTransform in _transforms.Values) {
				packs[i] = syncTransform.GetData();
				i++;
			}

			return packs;
		}

		internal static void ApplyData(TransformPack[] msg) {
			foreach (TransformPack pack in msg)
				_transforms[pack.netID].ApplyData(pack);
		}
	}
}
