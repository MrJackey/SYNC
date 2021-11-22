using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sync.Attributes;
using Sync.Handlers;
using Sync.Messages;
using Sync.Packs;
using Sync.Utils;
using Sync.Utils.Extensions;
using UnityEngine;

namespace Sync.Components {
	[RequireComponent(typeof(SYNCIdentity))]
	public abstract class SYNCBehaviour : MonoBehaviour {
		private const BindingFlags Field_Bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags Method_Bindings = BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		[Header("SYNC Behaviour")]
		[SerializeField, Tooltip("Control component updates")]
		private SYNCBehaviourUpdateMode _updateMode = SYNCBehaviourUpdateMode.ServerOnly;

		private Dictionary<string, FieldInfo> _syncVars = new Dictionary<string, FieldInfo>();
		private byte behaviourID;

		private SYNCIdentity SyncIdentity { get; set; }
		public int NetID => SyncIdentity.NetID;

		protected virtual void Awake() {
			SyncIdentity = GetComponent<SYNCIdentity>();

			foreach (FieldInfo fieldInfo in GetType().GetFields(Field_Bindings))
				foreach (CustomAttributeData customAttr in fieldInfo.CustomAttributes)
					if (customAttr.AttributeType == typeof(SYNCVarAttribute)) {
						_syncVars.Add(fieldInfo.Name, fieldInfo);
						break;
					}
		}

		internal void UpdateEnableStatus() {
			switch (_updateMode) {
				case SYNCBehaviourUpdateMode.AsIs:
					return;
				case SYNCBehaviourUpdateMode.ServerOnly when !SYNC.IsServer:
					enabled = false;
					break;
				case SYNCBehaviourUpdateMode.AuthorityOnly:
					enabled = SYNCHelperInternal.IsAuthorityMine(SyncIdentity);
					break;
			}
		}

		internal void RegisterAtHandler() {
			if (_syncVars.Count > 0)
				SYNCVarHandler.Register(SyncIdentity);
		}

		internal void AssignBehaviourID(byte ID) {
			behaviourID = ID;
		}

		protected virtual void OnDestroy() {
			if (_syncVars.Count > 0)
				SYNCVarHandler.Unregister(SyncIdentity);
		}

		/// <summary>
		/// Send an RPC to the server instance
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		public void InvokeServer(string methodName, params object[] args) {
			if (SYNC.IsClient)
				SYNCClient.Instance.SendRPC(NetID, behaviourID, methodName, args);
		}

		/// <summary>
		/// Send an RPC to a specific client instance
		/// </summary>
		/// <param name="clientID"></param>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		public void InvokeClient(int clientID, string methodName, params object[] args) {
			if (SYNC.IsServer)
				SYNCServer.Instance.SendRPC(clientID, NetID, behaviourID, methodName, args);
		}

		/// <summary>
		/// Send an RPC to all connected clients' instances
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		public void InvokeClients(string methodName, params object[] args) {
			if (SYNC.IsServer)
				SYNCServer.Instance.SendRPC(NetID, behaviourID, methodName, args);
		}

		internal BehaviourVarsPack GetVarData() {
			(string, object)[] vars = new (string, object)[_syncVars.Count];

			int i = 0;
			foreach ((string fieldName, FieldInfo field) in _syncVars) {
				vars[i] = (fieldName, field.GetValue(this));
				i++;
			}

			return new BehaviourVarsPack(behaviourID, vars);
		}

		public void ApplyVarData((string name, object value)[] behaviourVars) {
			foreach ((string fieldName, object fieldValue) in behaviourVars)
				_syncVars[fieldName].SetValue(this, fieldValue);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			object[] parameters = msg.Parameters.Select(obj => obj.Data).ToArray();
			GetType().InvokeMember(msg.MethodName, Method_Bindings, null, this, parameters);
		}
	}
}
