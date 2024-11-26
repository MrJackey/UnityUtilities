using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Utilities.Unity {
	[DefaultExecutionOrder(-10)] // Ensure singletons are initialized before other behaviours OnEnable/OnDisable
	public abstract class LazySingletonBehaviour<T> : MonoBehaviour where T : LazySingletonBehaviour<T> {
		private static T s_instance;

		/// <summary>
		/// <para>Get the instance of the singleton</para>
		/// <para>
		///		Do not use this to check if an instance exists!
		///		Any use of the <see cref="Instance"/> property will create an instance if none exists.
		///		Use <see cref="Exists"/> to check existence without accidentally creating an instance
		/// </para>
		/// </summary>
		public static T Instance {
			get {
				if (s_instance == null)
					s_instance = new GameObject(typeof(T).Name, typeof(T)).GetComponent<T>();

				return s_instance;
			}
		}

		/// <summary>
		/// <para>
		/// Get the instance of the singleton if it exists, otherwise it returns real null.
		/// </para>
		/// <para>
		/// This allows safe use of null propagation
		/// </para>
		/// </summary>
		[CanBeNull]
		public static T NullableInstance => s_instance != null ? s_instance : null;

		/// <summary>
		/// Does an instance of the singleton exist?
		/// </summary>
		public static bool Exists => s_instance != null;

		protected virtual void Awake() {
			if (s_instance == null) {
				s_instance = (T)this;
			}
			else {
				Debug.LogWarning($"Found multiple instances of {typeof(T).Name} Singleton. Destroying the latest addition...");
				Destroy(this);
			}
		}
	}
}
