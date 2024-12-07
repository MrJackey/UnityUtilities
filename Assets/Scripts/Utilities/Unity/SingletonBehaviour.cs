using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Utilities.Unity {
	[DefaultExecutionOrder(-10)] // Ensure singletons are initialized before other behaviours OnEnable/OnDisable
	public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T> {
		/// <summary>
		/// Get the instance of the singleton
		/// </summary>
		public static T Instance { get; private set; }

		/// <summary>
		/// <para>
		/// Get the instance of the singleton if it exists, otherwise it returns real null.
		/// </para>
		/// <para>
		/// This allows safe use of null propagation
		/// </para>
		/// </summary>
		[CanBeNull]
		public static T NullableInstance {
			get {
				T instance = Instance;
				return instance != null ? instance : null;
			}
		}

		/// <summary>
		/// Shorthand for checking if an instance of the singleton exists
		/// </summary>
		public static bool Exists => Instance != null;

		protected virtual void Awake() {
			if (Instance == null) {
				Instance = (T)this;
			}
			else {
				Debug.LogWarning($"Found multiple instances of {typeof(T).Name} Singleton. Destroying the latest addition...");
				Destroy(this);
			}
		}
	}
}
