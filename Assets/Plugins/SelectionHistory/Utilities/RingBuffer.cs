using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Jackey.SelectionHistory.Utilities {
	public sealed class RingBuffer<T> : IList<T> {
		private readonly T[] m_buffer;

		private int m_read;
		private int m_write;

		private int m_count;

		public int Size => m_buffer.Length;
		public int Count => m_count;

		public bool IsReadOnly => true;

		public T this[int index] {
			get {
				if (index < 0 || index >= Count)
					throw new IndexOutOfRangeException();

				return m_buffer[GetInternalIndex(index)];
			}
			set {
				if (index < 0 || index >= Count)
					throw new IndexOutOfRangeException();

				m_buffer[GetInternalIndex(index)] = value;
			}
		}

		public RingBuffer(int size) {
			m_buffer = new T[size];
		}

		public void Add(T obj) {
			m_buffer[m_write] = obj;

			if (m_count != Size)
				m_count++;
			else if (++m_read >= Size)
				m_read -= Size;

			IncrementWrite();
		}

		public void Insert(int index, T item) {
			if (index < 0 || index > m_count)
				throw new IndexOutOfRangeException();

			int internalIndex = GetInternalIndex(index);

			if (m_count == Size) {
				int shiftIndex = m_read;
				int overflow = index - internalIndex;

				if (overflow > 0) {
					for (int i = 0; i < overflow - 1; i++) {
						m_buffer[m_read + i] = m_buffer[m_read + i + 1];
					}

					m_buffer[Size - 1] = m_buffer[0];
					shiftIndex = 0;
				}

				for (int i = shiftIndex; i < internalIndex; i++) {
					m_buffer[i] = m_buffer[i + 1];
				}
			}
			else {
				for (int i = Size - 1; i > internalIndex; i--) {
					m_buffer[i] = m_buffer[i - 1];
				}

				m_count++;
				IncrementWrite();
			}

			m_buffer[internalIndex] = item;
		}

		public bool Remove(T item) {
			int index = IndexOf(item);

			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public void RemoveAt(int index) {
			if (index < 0 || index > m_count)
				throw new IndexOutOfRangeException();

			int internalIndex = GetInternalIndex(index);

			// Shift all items when removing a non-last item
			if (internalIndex != m_read - 1) {
				int overflow = internalIndex - index;

				for (int i = internalIndex; i < Size - 1; i++) {
					m_buffer[i] = m_buffer[i + 1];
				}

				if (overflow > 0) {
					m_buffer[Size - 1] = m_buffer[0];

					for (int i = 0; i < overflow - 1; i++) {
						m_buffer[i] = m_buffer[i + 1];
					}
				}
			}

			m_count--;
			DecrementWrite();

			m_buffer[m_write] = default;
		}

		public int IndexOf(T item) {
			for (int i = m_read; i < m_count; i++) {
				if (m_buffer[i].Equals(item)) {
					return GetExternalIndex(i);
				}
			}

			int endIndex = Mathf.Min(m_read, m_write);
			for (int i = 0; i < endIndex; i++) {
				if (m_buffer[i].Equals(item)) {
					return GetExternalIndex(i);
				}
			}

			return -1;
		}

		public void Clear() {
			Array.Clear(m_buffer, 0, m_buffer.Length);

			m_write = 0;
			m_read = 0;
			m_count = 0;
		}

		[Pure]
		public bool Contains(T item) {
			foreach (T bufferItem in this) {
				if (bufferItem.Equals(item)) {
					return true;
				}
			}

			return false;
		}

		public void CopyTo(T[] array, int arrayIndex) {
			if (array == null)
				throw new ArgumentNullException(nameof(array), "The array is null");
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex), "The arrayIndex is less than 0");
			if (arrayIndex + m_count > array.Length)
				throw new ArgumentException("The number of elements in the RingBuffer is greater than the available space from arrayIndex to the end of the destination array.", nameof(array));

			for (int i = 0; i < m_count; i++) {
				array[arrayIndex + i] = m_buffer[GetInternalIndex(i)];
			}
		}

		private int GetInternalIndex(int index) {
			int ret = m_read + index;

			if (ret >= Size)
				ret -= Size;

			return ret;
		}

		private int GetExternalIndex(int index) {
			int ret = index - m_read;

			if (ret < 0)
				ret += Size;

			return ret;
		}

		private void IncrementWrite() {
			if (++m_write >= Size) {
				m_write -= Size;
			}
		}

		private void DecrementWrite() {
			if (--m_write < 0) {
				m_write += Size;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<T> GetEnumerator() => new RingBufferEnumerator<T>(this);
	}

	public struct RingBufferEnumerator<T> : IEnumerator<T> {
		private readonly RingBuffer<T> m_ringBuffer;
		private int m_index;

		public T Current => m_ringBuffer[m_index];
		object IEnumerator.Current => Current;

		public RingBufferEnumerator(RingBuffer<T> buffer) {
			m_ringBuffer = buffer;
			m_index = -1;
		}

		public bool MoveNext() {
			m_index++;

			return m_index < m_ringBuffer.Count;
		}

		public void Reset() {
			m_index = -1;
		}

		public void Dispose() { }
	}
}
