using Sync.Handlers;
using Sync.Utils;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Transform")]
	[RequireComponent(typeof(Transform), typeof(SYNCIdentity))]
	internal sealed class SYNCTransform : MonoBehaviour {
		[SerializeField] private SYNCPositionPrecision _positionPrecision = SYNCPositionPrecision.Vector3Half;
		[SerializeField] private SYNCRotationPrecision _rotationPrecision = SYNCRotationPrecision.Ignore;
		[SerializeField] private SYNCScalePrecision _scalePrecision = SYNCScalePrecision.Ignore;

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
			Transform myTransform = transform;

			return new TransformPack(
				NetID,
				(SYNCTransformOptions)((ushort)_positionPrecision | (ushort)_rotationPrecision | (ushort)_scalePrecision),
				myTransform.position,
				myTransform.rotation,
				myTransform.localScale
			);
		}

		public void ApplyData(TransformPack pack) {
			Transform myTransform = transform;

			if ((pack.options & SYNCTransformOptions.PositionIgnore) == 0) {
				Vector3 myPosition = myTransform.position;

				if ((pack.options & (SYNCTransformOptions.PositionVector3Half | SYNCTransformOptions.PositionVector3Float)) != 0)
					transform.position = pack.Position;
				else if ((pack.options & (SYNCTransformOptions.PositionVector2Half | SYNCTransformOptions.PositionVector2Float)) != 0)
					transform.position = new Vector3(pack.Position.x, pack.Position.y, myPosition.z);
			}

			if ((pack.options & SYNCTransformOptions.RotationIgnore) == 0) {
				if ((pack.options & SYNCTransformOptions.Quaternion) != 0)
					transform.rotation = pack.Rotation;
			}

			if ((pack.options & SYNCTransformOptions.ScaleIgnore) == 0) {
				Vector3 myScale = myTransform.localScale;

				if ((pack.options & (SYNCTransformOptions.ScaleUniformHalf | SYNCTransformOptions.ScaleUniformFloat)) != 0)
					transform.localScale = new Vector3(pack.Scale.x, pack.Scale.x, pack.Scale.x);
				else if ((pack.options & (SYNCTransformOptions.ScaleVector3Half | SYNCTransformOptions.ScaleVector3Half)) != 0)
					transform.localScale = pack.Scale;
				else if ((pack.options & (SYNCTransformOptions.ScaleVector2Half | SYNCTransformOptions.ScaleVector2Float)) != 0)
					transform.localScale = new Vector3(pack.Scale.x, pack.Scale.y, myScale.z);
			}
		}
	}
}
