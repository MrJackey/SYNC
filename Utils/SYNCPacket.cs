using System.Linq;
using Sync.Packs;

namespace Sync.Utils {
	internal class SYNCPacket<T> where T : IPack {
		internal T[] Content { get; }
		internal int Size { get; }

		internal SYNCPacket(T[] content) {
			Content = content;
			Size = Content.Sum(pack => pack.ByteSize);
		}
	}
}
