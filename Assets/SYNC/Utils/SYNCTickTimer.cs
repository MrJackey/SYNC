using System;
using System.Diagnostics;

namespace Sync.Utils {
	internal class SYNCTickTimer {
		private float _duration;
		private Stopwatch _stopwatch;

		public float Time => _stopwatch.ElapsedMilliseconds;

		public float Duration {
			get => _duration;
			private set => _duration = Math.Max(0, value);
		}

		public bool Elapsed => Time >= _duration;

		public SYNCTickTimer(int tickRate) {
			_stopwatch = new Stopwatch();
			_stopwatch.Start();

			Duration = 1f / tickRate * 1000f;
		}

		public void Restart() {
			_stopwatch.Restart();
		}
	}
}
