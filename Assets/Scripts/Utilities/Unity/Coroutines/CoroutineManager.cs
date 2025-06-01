using System.Collections;
using UnityEngine;

namespace Jackey.Utilities.Unity.Coroutines {
	/// <summary>
	/// Allows starting coroutines without requiring a MonoBehaviour. It also provides a handle
	/// to control coroutines started using <see cref="CoroutineManager.StartNew(IEnumerator)"/>
	/// </summary>
	public class CoroutineManager : LazySingletonBehaviour<CoroutineManager> {
		protected override void Awake() {
			base.Awake();

			DontDestroyOnLoad(gameObject);
		}

		/// <summary>
		/// Start a coroutine with a <see cref="CoroutineHandle"/> attached to it.
		/// The CoroutineManager will act as the owner
		/// </summary>
		/// <param name="coroutine">The coroutine to run</param>
		/// <returns>Returns a <see cref="CoroutineHandle"/> that can be used to control the started coroutine</returns>
		public static CoroutineHandle StartNew(IEnumerator coroutine) => StartNew(coroutine, Instance);

		/// <summary>
		/// Start a coroutine with a <see cref="CoroutineHandle"/> attached to it.
		/// </summary>
		/// <param name="coroutine">The coroutine to run</param>
		/// <param name="owner">The owner of the coroutine</param>
		/// <returns>Returns a <see cref="CoroutineHandle"/> that can be used to control the started coroutine</returns>
		public static CoroutineHandle StartNew(IEnumerator coroutine, MonoBehaviour owner) {
			CoroutineHandle handle = new CoroutineHandle(coroutine);
			owner.StartCoroutine(handle);

			return handle;
		}

		/// <summary>
		/// Start a coroutine with a <see cref="CoroutineHandle"/> attached to it.
		/// The CoroutineManager will act as the owner
		/// </summary>
		/// <param name="handle">The handle to hold the coroutine</param>
		/// <param name="coroutine">The coroutine to run</param>
		/// <returns>Returns a <see cref="CoroutineHandle"/> that can be used to control the started coroutine</returns>
		public static void StartNew(CoroutineHandle handle, IEnumerator coroutine) => StartNew(handle, coroutine, Instance);

		/// <summary>
		/// Start a coroutine with a <see cref="CoroutineHandle"/> attached to it.
		/// </summary>
		/// <param name="handle">The handle to hold the coroutine</param>
		/// <param name="coroutine">The coroutine to run</param>
		/// <param name="owner">The owner of the coroutine</param>
		/// <returns>Returns a <see cref="CoroutineHandle"/> that can be used to control the started coroutine</returns>
		public static void StartNew(CoroutineHandle handle, IEnumerator coroutine, MonoBehaviour owner) {
			handle.Reset(coroutine);
			owner.StartCoroutine(handle);
		}
	}
}
