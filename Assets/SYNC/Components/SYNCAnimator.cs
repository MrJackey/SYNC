using System;
using System.Collections.Generic;
using Sync.Handlers;
using Sync.Utils;
using UnityEngine;

namespace Sync.Components {
	[AddComponentMenu("SYNC/SYNC Animator")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SYNCIdentity))]
	internal sealed class SYNCAnimator : MonoBehaviour {
		[SerializeField] private Animator _animator;

		private readonly Dictionary<string, int> _parameterHashes = new Dictionary<string, int>();

		internal SYNCIdentity SyncIdentity { get; private set; }
		public int NetID => SyncIdentity.NetID;
		public Animator Animator => _animator;

		private void Awake() {
			_animator ??= GetComponent<Animator>();

			if (_animator == null) {
				Debug.LogWarning("[SYNC] Unable to locate Animator component", gameObject);
				return;
			}

			CacheParameterHashes();

			SyncIdentity = GetComponent<SYNCIdentity>();
			SyncIdentity.SyncAnimator = this;
			SyncIdentity.NetIDAssigned += OnNetIDAssigned;
		}

		private void CacheParameterHashes() {
			foreach (AnimatorControllerParameter parameter in _animator.parameters)
				_parameterHashes.Add(parameter.name, parameter.nameHash);
		}

		private void OnNetIDAssigned(int netID) {
			SYNCAnimatorHandler.Register(netID);
		}

		private void OnDestroy() {
			if (NetID != 0)
				SYNCAnimatorHandler.Unregister(NetID);

			if (SyncIdentity != null)
				SyncIdentity.NetIDAssigned -= OnNetIDAssigned;
		}

		internal AnimatorPack GetData() {
			List<AnimatorParameterPack> parameterPacks = new List<AnimatorParameterPack>();

			for (int i = 0; i < _animator.parameterCount; i++)
				if (_animator.parameters[i].type != AnimatorControllerParameterType.Trigger)
					parameterPacks.Add(ConstructAnimatorParameterPack(_animator.parameters[i]));

			return new AnimatorPack(NetID, parameterPacks.ToArray());
		}

		private AnimatorParameterPack ConstructAnimatorParameterPack(AnimatorControllerParameter parameter) {
			int nameHash = _parameterHashes[parameter.name];

			return parameter.type switch {
				AnimatorControllerParameterType.Float => new AnimatorParameterPack(nameHash, parameter.type, _animator.GetFloat(nameHash)),
				AnimatorControllerParameterType.Int => new AnimatorParameterPack(nameHash, parameter.type, _animator.GetInteger(nameHash)),
				AnimatorControllerParameterType.Bool => new AnimatorParameterPack(nameHash, parameter.type, _animator.GetBool(nameHash)),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		internal void ApplyData(AnimatorPack pack) {
			foreach (AnimatorParameterPack parameterPack in pack.ParameterPacks) {
				switch (parameterPack.Type) {
					case AnimatorControllerParameterType.Float:
						_animator.SetFloat(parameterPack.NameHash, parameterPack.FloatValue);
						break;
					case AnimatorControllerParameterType.Int:
						_animator.SetInteger(parameterPack.NameHash, parameterPack.IntValue);
						break;
					case AnimatorControllerParameterType.Bool:
						_animator.SetBool(parameterPack.NameHash, parameterPack.BoolValue);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}
