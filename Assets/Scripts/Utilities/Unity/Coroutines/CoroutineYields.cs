using UnityEngine;

namespace Jackey.Utilities.Unity.Coroutines {
	public partial class CoroutineHandle {
		private static readonly WaitYield s_wait = new WaitYield();
		private static readonly WaitRealTimeYield s_waitRealTime = new WaitRealTimeYield();

		public static readonly YieldInstruction WaitForFixedUpdate = new WaitForFixedUpdate();
		public static readonly YieldInstruction WaitForEndOfFrame = new WaitForEndOfFrame();

		/// <summary>
		/// Yield a coroutine with a wait that can be paused
		/// </summary>
		/// <param name="seconds">The duration of the wait</param>
		/// <returns>The object telling the coroutine to wait</returns>
		public static WaitYield Wait(float seconds) {
			s_wait.Time = seconds;
			return s_wait;
		}

		/// <summary>
		/// Yield a coroutine with a real time wait that can be paused
		/// </summary>
		/// <param name="seconds">The duration of the wait</param>
		/// <returns>The object telling the coroutine to wait</returns>
		public static WaitRealTimeYield WaitRealTime(float seconds) {
			s_waitRealTime.Time = seconds;
			return s_waitRealTime;
		}

		/// <summary>
		/// Base class for custom yields that can be paused when started
		/// using <see cref="CoroutineManager.StartNew(System.Collections.IEnumerator)"/>
		/// </summary>
		public abstract class TimeYield {
			internal float Time;

			/// <summary>
			/// How much the timer should progress whenever its ticked. It is ticked each frame
			/// </summary>
			public abstract float Delta { get; }
		}

		/// <summary>
		/// A custom yield that can be paused when used to wait in coroutines started with <see cref="CoroutineManager.StartNew(System.Collections.IEnumerator)"/>.
		/// Avoid creating new instances of this and instead use <see cref="CoroutineHandle.Wait"/>
		/// </summary>
		public class WaitYield : TimeYield {
			public override float Delta => UnityEngine.Time.deltaTime;
		}

		/// <summary>
		/// A custom yield that can be paused when used to wait in real time in coroutines started with <see cref="CoroutineManager.StartNew(System.Collections.IEnumerator)"/>.
		/// Avoid creating new instances of this and instead use <see cref="CoroutineHandle.WaitRealTime"/>
		/// </summary>
		public class WaitRealTimeYield : TimeYield {
			public override float Delta => UnityEngine.Time.unscaledDeltaTime;
		}
	}
}
