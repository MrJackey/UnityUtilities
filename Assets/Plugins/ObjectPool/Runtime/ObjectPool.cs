using System;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Jackey.ObjectPool {
	/// <summary>
	/// <para>
	/// The main class for accessing and interacting with ObjectPools.
	/// The pools are shared globally unless created using ObjectPool.CreateLocal().
	/// </para>
	/// <para>
	/// Note that any interaction with a pool will create a new one if none exists.
	/// </para>
	/// </summary>
	public static partial class ObjectPool {
		internal static event Action<IPool> PoolCreated;
		internal static event Action<IPool> PoolReset;
		internal static event Action Cleared;

		internal static event Action<IPool, object> AnyObjectSetup;
		internal static event Action<IPool, object> AnyObjectReturned;

		static ObjectPool() {
			SceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR
			EditorSceneManager.sceneClosed += OnSceneUnloaded;
#endif
		}

		/// <summary>
		/// Clear all pools of their objects and remove all existing pools
		/// </summary>
#if UNITY_EDITOR
		[MenuItem("Tools/Jackey/Object Pool/Clear Pools", false, 1020)]
#endif
		public static void Clear() {
			foreach (IPool pool in s_gameObjectPools.Values)
				pool.Clear();

			foreach (IPool pool in s_pocoPools.Values)
				pool.Clear();

			s_gameObjectPools.Clear();
			s_pocoPools.Clear();

			Cleared?.Invoke();
		}

		private static void OnSceneUnloaded(Scene _) {
			RemoveDestroyedGameObjects();
		}
	}
}
