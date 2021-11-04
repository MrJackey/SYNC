using System;
using System.Collections;
using System.Collections.Generic;

namespace Sync.Utils {
	internal sealed class RingBuffer<T> {
		private T[] _buffer;
		private int _read;
		private int _write;

		public int Size => _buffer.Length;
		public int Count { get; private set; }
		public bool IsFull => Count == Size;

		public T this[int index] {
			get {
				if (index < 0 || index >= Size)
					throw new IndexOutOfRangeException();

				return _buffer[WrapIndex(_read + index)];
			}
		}

		public RingBuffer(int size) {
			_buffer = new T[size];
		}

		public IEnumerator<T> GetEnumerator() {
			return new RingBufferEnumerator<T>(this);
		}

		public void Push(T obj) {
			_buffer[_write] = obj;

			if (Count == Size)
				_read = WrapIndex(_read + 1);
			else
				Count++;

			if (++_write >= Size)
				_write = WrapIndex(_write);
		}

		public T Peek() => _buffer[_read];

		public void Clear() {
			_buffer = new T[Size];
			_write = 0;
			_read = 0;
		}

		private int WrapIndex(int index) {
			return index % Size;
		}
	}

	internal struct RingBufferEnumerator<T> : IEnumerator<T> {
		private readonly RingBuffer<T> _ringBuffer;
		private int _counter;

		public T Current => _ringBuffer[_counter];
		object IEnumerator.Current => Current;

		public RingBufferEnumerator(RingBuffer<T> buffer) {
			this._ringBuffer = buffer;
			this._counter = -1;
		}

		public bool MoveNext() {
			_counter++;

			return _counter < _ringBuffer.Size;
		}

		public void Reset() {
			_counter = -1;
		}

		public void Dispose() { }
	}
}
