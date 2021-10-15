using SYNC.Utils;

namespace SYNC.Messages {
	internal class SYNCClientRegisterNetIDMsg {
		public int ClientNetID { get; set; }
	}

	internal class SYNCClientJoinedMsg {
		public int ClientNetID { get; set; }
	}

	internal class SYNCClientDisconnectMsg {
		public int NetID { get; set; }
	}

	internal class SYNCServerStateMsg {
		public TransformPack[] SYNCTransforms { get; set; }
	}
}
