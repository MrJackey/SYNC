using SYNC.Utils;
using UnityEngine;

namespace SYNC.Components {
	[RequireComponent(typeof(Transform))]
	internal sealed class SYNCTransform : SYNCComponent {
		internal TransformPack GetData() {
			return new TransformPack() {netID = NetID, position = transform.position};
		}

		public void ApplyData(TransformPack pack) {
			transform.position = pack.position;
		}
	}
}
