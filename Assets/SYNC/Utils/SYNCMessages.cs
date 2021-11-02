﻿using Sync.Utils;

namespace Sync.Messages {
	internal class SYNCClientRegisterNetIDMsg {
		public int ClientNetID { get; set; }
	}

	internal class SYNCClientJoinedMsg {
		public int ClientNetID { get; set; }
	}

	internal class SYNCClientDisconnectMsg {
		public int NetID { get; set; }
	}

	internal class SYNCObjectInstantiateMsg {
		public int NetID { get; set; }
		public int PrefabID { get; set; }
		public InstantiatePack Info { get; set; }
	}

	internal class SYNCObjectDestroyMsg {
		public int NetID { get; set; }
	}

	internal class SYNCServerStateMsg {
		public static int HeaderSize => sizeof(uint);
		public uint tick;
		public TransformPack[] SYNCTransforms { get; set; }
		public AnimatorPack[] SYNCAnimators { get; set; }
	}

	internal class SYNCRPCMsg {
		public int NetID { get; set; }
		public int BehaviourID { get; set; }
		public string MethodName { get; set; }
		public ObjectPack[] Parameters { get; set; }
	}
}
