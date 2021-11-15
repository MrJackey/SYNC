namespace Sync.Utils {
	public enum SYNCAuthority : byte {
		Server,
		Client,
	}

	public enum SYNCBehaviourUpdateMode : byte {
		AsIs,
		ServerOnly,
		AuthorityOnly,
	}
}
