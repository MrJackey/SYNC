using System.Linq;
using System.Reflection;
using Sync.Messages;
using UnityEngine;

namespace Sync.Components {
	[RequireComponent(typeof(SYNCIdentity))]
	public abstract class SYNCBehaviour : MonoBehaviour {
		private SYNCIdentity SyncIdentity { get; set; }
		internal int NetID => SyncIdentity.NetID;

		protected virtual void Awake() {
			SyncIdentity = GetComponent<SYNCIdentity>();
			SyncIdentity.SYNCBehaviours.Add(GetInstanceID(), this);
		}

		/// <summary>
		/// Send an RPC to the server instance
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		public void ServerInvoke(string methodName, params object[] args) {
			if (SYNC.IsClient)
				SYNCClient.Instance.SendRPC(NetID, GetInstanceID(), methodName, args);
		}

		internal void ExecuteRPC(SYNCRPCMsg msg) {
			object[] parameters = msg.Parameters.Select(obj => obj.Data).ToArray();
			GetType().InvokeMember(msg.MethodName, BindingFlags.InvokeMethod, null, this, parameters);
		}
	}
}
