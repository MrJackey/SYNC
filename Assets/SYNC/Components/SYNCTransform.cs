using SYNC.Utils;
using UnityEngine;

namespace SYNC.Components {
	[RequireComponent(typeof(Transform), typeof(SYNCIdentity))]
	internal sealed class SYNCTransform : MonoBehaviour {
		internal SYNCIdentity SyncIdentity { get; private set; }
		internal int NetID => SyncIdentity.NetID;

		private void Awake() {
			SyncIdentity = GetComponent<SYNCIdentity>();
			SyncIdentity.SYNCTransform = this;
		}

		private void Start() {
			SYNCTransformHandler.Register(SyncIdentity.NetID);
		}

		private void OnDestroy() {
			SYNCTransformHandler.UnRegister(SyncIdentity.NetID);
		}

		internal TransformPack GetData() {
			return new TransformPack {netID = NetID, position = transform.position};
		}

		public void ApplyData(TransformPack pack) {
			transform.position = pack.position;
		}
	}
}
