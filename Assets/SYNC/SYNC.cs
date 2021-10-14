using SYNC.Components;

namespace SYNC {
	public static class SYNC {
		private static int netID;
		internal static int NextNetID => ++netID;

		public static bool IsServer => SYNCServer.Instance != null;
		public static bool IsClient => SYNCClient.Instance != null;
	}
}
