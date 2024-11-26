using System;
using System.Collections;
using UnityEngine;

namespace Jackey.Utilities.Unity.Coroutines {
	/// <summary>
	/// A handle for controlling coroutines started using <see cref="CoroutineManager.StartNew(IEnumerator)"/>.
	/// </summary>
	public partial class CoroutineHandle : IEnumerator {
		private IEnumerator m_coroutine;
		private State m_state;

		private WaitUntil m_timeYield;
		private TimeYield m_innerTimeYield;
		private float m_time;

		private object m_yield;

		public bool IsIdle => m_state == State.Idle;
		public bool IsRunning => m_state == State.Running;
		public bool IsPaused => m_state == State.Paused;
		public bool IsStopped => m_state == State.Stopped;

		public CoroutineHandle() { }

		internal CoroutineHandle(IEnumerator coroutine) {
			m_coroutine = coroutine;
			m_state = State.Running;
		}

		internal void Reset(IEnumerator coroutine) {
			m_coroutine = coroutine;
			m_state = State.Running;
		}

		/// <summary>
		/// Pause the execution of the coroutine. It will essentially wait forever after its current yield
		/// unless it is a <see cref="CoroutineHandle.TimeYield"/>, then the wait progress will halt as well
		/// </summary>
		public void Pause() {
			if (!IsRunning) return;

			m_state = State.Paused;
		}

		/// <summary>
		/// Resume the execution of a paused coroutine
		/// </summary>
		public void Resume() {
			if (!IsPaused) return;

			m_state = State.Running;
		}

		/// <summary>
		/// Stop the execution of the coroutine after its next tick. It can not be restarted
		/// </summary>
		public void Stop() {
			if (IsIdle) return;

			m_state = State.Stopped;
		}

		private bool TickWait() {
			if (IsPaused) return false;

			if (IsStopped)
				return true;

			m_time -= m_innerTimeYield.Delta;

			return m_time <= 0f;
		}

		object IEnumerator.Current => IsPaused ? null : m_yield;

		bool IEnumerator.MoveNext() {
			if (IsStopped)
				return false;

			if (IsPaused)
				return true;

			if (m_coroutine.MoveNext()) {
				object innerCurrent = m_coroutine.Current;

				if (innerCurrent is TimeYield timeYield) {
					m_yield = m_timeYield ??= new WaitUntil(TickWait);
					m_time = timeYield.Time;
					m_innerTimeYield = timeYield;
				}
				else {
					m_yield = innerCurrent;
				}

				return true;
			}

			m_state = State.Idle;
			return false;
		}

		void IEnumerator.Reset() => throw new NotSupportedException();

		private enum State {
			Idle,
			Running,
			Paused,
			Stopped,
		}
	}
}
