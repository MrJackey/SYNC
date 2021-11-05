using System.Collections.Generic;

namespace Sync.Utils.Extensions {
	public static class DictionaryExtensions {
		public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value) {
			key = kv.Key;
			value = kv.Value;
		}
	}
}
