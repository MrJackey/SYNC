using System;
using Sync.Handlers;
using Sync.Utils;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Transform")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Transform), typeof(SYNCIdentity))]
	internal sealed class SYNCTransform : MonoBehaviour {
		private const int Optimal_Snapshot = 2;
		private const int Max_Snapshot = 4;

		[Header("Synchronization")]
		[SerializeField] private SYNCPositionPrecision _positionPrecision = SYNCPositionPrecision.Vector3Half;
		[SerializeField] private SYNCRotationPrecision _rotationPrecision = SYNCRotationPrecision.Ignore;
		[SerializeField] private SYNCScalePrecision _scalePrecision = SYNCScalePrecision.Ignore;

		[Header("Interpolation")]
		[SerializeField] private SYNCPositionInterpolationOptions _positionInterpolation = SYNCPositionInterpolationOptions.Linear;
		[SerializeField] private SYNCInterpolationOptions _rotationInterpolation = SYNCInterpolationOptions.Linear;
		[SerializeField] private SYNCInterpolationOptions _scaleInterpolation = SYNCInterpolationOptions.Linear;

		[Header("Extrapolation")]
		[SerializeField] private bool _extrapolate;
		[SerializeField, Range(0, 1)] private float _extrapolationPreservation = 0.75f;

		#if UNITY_EDITOR
		[Header("Debug")]
		[SerializeField] private bool _visualizePosition;
		#endif

		private readonly RingBuffer<Vector3> _positionInterpolationBuffer = new RingBuffer<Vector3>(6);
		private readonly RingBuffer<Quaternion> _rotationInterpolationBuffer = new RingBuffer<Quaternion>(6);
		private readonly RingBuffer<Vector3> _scaleInterpolationBuffer = new RingBuffer<Vector3>(6);

		private readonly RingBuffer<int> _catchDownBuffer = new RingBuffer<int>(6);

		private float _tInterpolation;
		private bool _activeExtrapolation;
		private float _tExtrapolation = 1f;

		private int TargetSnapshot { get; set; } = Optimal_Snapshot;
		private int PrevSnapshot => TargetSnapshot - 1;

		internal SYNCIdentity SyncIdentity { get; private set; }
		public int NetID => SyncIdentity.NetID;

		private void Awake() {
			SyncIdentity = GetComponent<SYNCIdentity>();
			SyncIdentity.SyncTransform = this;

			SyncIdentity.NetIDAssigned += OnNetIDAssigned;
		}

		private void OnNetIDAssigned(int netID) {
			SYNCTransformHandler.Register(netID);
		}

		private void Update() {
			if (SYNC.IsClient && !SYNC.IsServer && _catchDownBuffer.Count > Optimal_Snapshot + 1) {
				float tickDelta = SYNCClient.Instance.ReceiveRate * Time.deltaTime;
				float catchDown = TargetSnapshot != Optimal_Snapshot
					? _catchDownBuffer[TargetSnapshot]
					: 1;
				float scaledTickDelta = tickDelta / catchDown;

				_tInterpolation += scaledTickDelta;
				if (_tInterpolation > 1) {
					if (TargetSnapshot == Max_Snapshot) {
						if (!_activeExtrapolation) {
							_activeExtrapolation = true;
							_tExtrapolation = _tInterpolation;
						}
					}
					else {
						TargetSnapshot = Mathf.Min(Max_Snapshot, TargetSnapshot + 1);
					}

					_tInterpolation -= 1;
				}

				if (_extrapolate && _activeExtrapolation) {
					_tExtrapolation += tickDelta * Mathf.Pow(_extrapolationPreservation, _tExtrapolation);

					if (_positionPrecision != SYNCPositionPrecision.Ignore)
						ExtrapolatePosition();
					if (_rotationPrecision != SYNCRotationPrecision.Ignore)
						ExtrapolateRotation();
					if (_scalePrecision != SYNCScalePrecision.Ignore)
						ExtrapolateScale();
				}
				else {
					if (_positionPrecision != SYNCPositionPrecision.Ignore)
						InterpolatePosition();
					if (_rotationPrecision != SYNCRotationPrecision.Ignore)
						InterpolateRotation();
					if (_scalePrecision != SYNCScalePrecision.Ignore)
						InterpolateScale();
				}
			}
		}

		private void InterpolatePosition() {
			switch (_positionInterpolation) {
				case SYNCPositionInterpolationOptions.None:
					transform.position = _positionInterpolationBuffer[TargetSnapshot];
					break;
				case SYNCPositionInterpolationOptions.Linear when _positionInterpolationBuffer.Count >= 2:
					transform.position = Vector3.Lerp(_positionInterpolationBuffer[PrevSnapshot], _positionInterpolationBuffer[TargetSnapshot], _tInterpolation);
					break;
				case SYNCPositionInterpolationOptions.CubicHermiteSpline when _positionInterpolationBuffer.Count >= 4:
					Vector3 m0 = _positionInterpolationBuffer[PrevSnapshot] - _positionInterpolationBuffer[PrevSnapshot - 1];
					Vector3 m1 = _positionInterpolationBuffer[TargetSnapshot + 1] - _positionInterpolationBuffer[TargetSnapshot];

					if (m0 == Vector3.zero)
						m1.Set(0, 0, 0);
					else if (m1 == Vector3.zero)
						m0.Set(0, 0, 0);

					transform.position = SYNCHelperInternal.EvaluateCubicHermiteSpline(
						_positionInterpolationBuffer[PrevSnapshot],
						_positionInterpolationBuffer[TargetSnapshot],
						m0,
						m1,
						_tInterpolation);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void InterpolateRotation() {
			transform.rotation = _rotationInterpolation switch {
				SYNCInterpolationOptions.None => _rotationInterpolationBuffer[TargetSnapshot],
				SYNCInterpolationOptions.Linear => Quaternion.Slerp(_rotationInterpolationBuffer[PrevSnapshot], _rotationInterpolationBuffer[TargetSnapshot], _tInterpolation),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		private void InterpolateScale() {
			transform.localScale = _scaleInterpolation switch {
				SYNCInterpolationOptions.None => _scaleInterpolationBuffer[TargetSnapshot],
				SYNCInterpolationOptions.Linear => Vector3.Lerp(_scaleInterpolationBuffer[PrevSnapshot],_scaleInterpolationBuffer[TargetSnapshot], _tInterpolation),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		private void ExtrapolatePosition() {
			transform.position = Vector3.LerpUnclamped(_positionInterpolationBuffer[PrevSnapshot], _positionInterpolationBuffer[TargetSnapshot], _tExtrapolation);
		}

		private void ExtrapolateRotation() {
			transform.rotation = Quaternion.SlerpUnclamped(_rotationInterpolationBuffer[PrevSnapshot], _rotationInterpolationBuffer[TargetSnapshot], _tExtrapolation);
		}

		private void ExtrapolateScale() {
			transform.localScale = Vector3.LerpUnclamped(_scaleInterpolationBuffer[PrevSnapshot], _scaleInterpolationBuffer[TargetSnapshot], _tExtrapolation);
		}

		#if UNITY_EDITOR
		private void OnDrawGizmos() {
			if (_visualizePosition) {
				for (int i = 0; i < Max_Snapshot + 2; i++) {
					if (i == TargetSnapshot)
						Gizmos.color = Color.green;
					else if (i == PrevSnapshot)
						Gizmos.color = Color.yellow;
					else
						Gizmos.color = Color.red;

					if (_activeExtrapolation)
						Gizmos.color = Color.blue;

					Gizmos.DrawWireSphere(_positionInterpolationBuffer[i], 0.5f);
				}
			}
		}
		#endif

		private void OnDestroy() {
			if (NetID != 0)
				SYNCTransformHandler.UnRegister(NetID);

			if (SyncIdentity != null)
				SyncIdentity.NetIDAssigned -= OnNetIDAssigned;
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
				if ((pack.options & (SYNCTransformOptions.PositionVector3Half | SYNCTransformOptions.PositionVector3Float)) != 0)
					_positionInterpolationBuffer.Push(pack.Position);
				else if ((pack.options & (SYNCTransformOptions.PositionVector2Half | SYNCTransformOptions.PositionVector2Float)) != 0)
					_positionInterpolationBuffer.Push(new Vector3(pack.Position.x, pack.Position.x, myTransform.position.z));
			}

			if ((pack.options & SYNCTransformOptions.RotationIgnore) == 0) {
				if ((pack.options & SYNCTransformOptions.Quaternion) != 0)
					_rotationInterpolationBuffer.Push(pack.Rotation);
			}

			if ((pack.options & SYNCTransformOptions.ScaleIgnore) == 0) {
				if ((pack.options & (SYNCTransformOptions.ScaleUniformHalf | SYNCTransformOptions.ScaleUniformFloat)) != 0)
					_scaleInterpolationBuffer.Push(new Vector3(pack.Scale.x, pack.Scale.x, pack.Scale.x));
				else if ((pack.options & (SYNCTransformOptions.ScaleVector3Half | SYNCTransformOptions.ScaleVector3Half)) != 0)
					_scaleInterpolationBuffer.Push(pack.Scale);
				else if ((pack.options & (SYNCTransformOptions.ScaleVector2Half | SYNCTransformOptions.ScaleVector2Float)) != 0)
					_scaleInterpolationBuffer.Push(new Vector3(pack.Scale.x, pack.Scale.y, myTransform.localScale.z));
			}

			// Restart interpolation on next set of data if already on time
			if (TargetSnapshot == Optimal_Snapshot)
				_tInterpolation = 0;
			else
				TargetSnapshot = Mathf.Max(Optimal_Snapshot, TargetSnapshot - 1);

			int catchDown = TargetSnapshot - Optimal_Snapshot + 1;
			// For some reason it adds a catchDown in form of triplets into the buffer so doing this for now
			if (catchDown != 1 && (catchDown == _catchDownBuffer[_catchDownBuffer.Count - 1] || catchDown == _catchDownBuffer[_catchDownBuffer.Count - 2]))
				catchDown--;

			_catchDownBuffer.Push(catchDown);

			_activeExtrapolation = false;
			_tExtrapolation = 1;
		}
	}
}
