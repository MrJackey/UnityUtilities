using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jackey.ObjectPool {
	public static partial class ObjectPool {
		internal static readonly Dictionary<Type, IPool> s_pocoPools = new();

		#region Actions

		/// <summary>
		/// Get a pooled instance of the given class
		/// </summary>
		/// <returns>
		/// An instance of the given class
		/// </returns>
		public static T New<T>() where T : class, new() {
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are trying to create a MonoBehaviour using the 'new' keyword. This is not allowed. MonoBehaviours can only be added using AddComponent()");

			if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] ScriptableObjects must be instantiated using the ScriptableObject.CreateInstance method instead of with the new keyword");

			return GetPocoObject<T>();
		}

		/// <summary>
		/// Get a pooled instance of the class
		/// </summary>
		/// <param name="handle">The handle of the pool to retrieve the instance from</param>
		/// <returns>
		/// An instance of the class from the pool connected to the handle
		/// </returns>
		public static T New<T>(PoolHandle<T> handle) where T : class, new() {
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are trying to create a MonoBehaviour using the 'new' keyword. This is not allowed. MonoBehaviours can only be added using AddComponent()");

			if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] ScriptableObjects must be instantiated using the ScriptableObject.CreateInstance method instead of with the new keyword");

			T @object = handle.GetObject();
			return @object;
		}

		/// <summary>
		/// Return a pooled instance to its pool. It can also be used to add non-pooled instances to a pool
		/// </summary>
		/// <param name="instance">The instance you wish to return to its pool</param>
		public static void Delete<T>(T instance) where T : class, new() {
			if (instance == null)
				throw new ArgumentNullException(nameof(instance), "[ObjectPool] The instance you want to delete is null");

			PocoPool<T> pool = GetPocoPool<T>();
			pool.ReturnObject(instance);
		}

		/// <summary>
		/// Return a pooled instance to its pool. It can also be used to add non-pooled instances to a pool
		/// </summary>
		/// <param name="handle">The handle of the pool to return the instance to</param>
		/// <param name="instance">The instance you wish to return to its pool</param>
		public static void Delete<T>(PoolHandle<T> handle, T instance) where T : class, new() {
			if (handle == null)
				throw new ArgumentNullException(nameof(handle), "[ObjectPool] The handle to the pool of the instance you want to delete is null");

			if (instance == null)
				throw new ArgumentNullException(nameof(instance), "[ObjectPool] The instance you want to delete is null");

			handle.ReturnObject(instance);
		}

		/// <summary>
		/// Get the handle of the pool of a class
		/// </summary>
		/// <returns>The handle connected to the pool of the class</returns>
		public static PoolHandle<T> GetHandle<T>() where T : class, new() {
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are retrieving a handle for a pool which uses the new keyword. This is not allowed on MonoBehaviours");

			if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are retrieving a handle for a pool which uses the new keyword. This is not allowed on ScriptableObjects");

			PocoPool<T> pool = GetPocoPool<T>();
			return pool.Handle;
		}

		/// <summary>
		/// Get the handle of a pool of a class. This pool is not accessible except for via the returned handle.
		/// Note that methods requiring a handle do accept handles referencing private pools as well
		/// </summary>
		/// <returns>The handle connected to the pool of the original</returns>
		public static PoolHandle<T> CreateLocal<T>() where T : class, new() {
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are creating a pool which uses the new keyword. This is not allowed on MonoBehaviours");

			if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("[ObjectPool] You are creating pool which uses the new keyword. This is not allowed on ScriptableObjects");

			PocoPool<T> pool = new PocoPool<T>();

			PoolCreated?.Invoke(pool);
			return pool.Handle;
		}

		#endregion

		private static T GetPocoObject<T>() where T : class, new() {
			PocoPool<T> pool = GetPocoPool<T>();
			return pool.GetObject();
		}

		private static PocoPool<T> GetPocoPool<T>() where T : class, new() {
			if (!s_pocoPools.TryGetValue(typeof(T), out IPool pool)) {
				pool = new PocoPool<T>();
				s_pocoPools.Add(typeof(T), pool);

				PoolCreated?.Invoke(pool);
			}

			return (PocoPool<T>)pool;
		}

		internal abstract class PocoPool { }

		internal class PocoPool<T> : PocoPool, IPool<T> where T : class, new() {
			private readonly PoolHandle<T> m_handle;

			// [active, ..., free]
			private readonly List<T> m_objects = new();
			private int m_activeCount;

			private bool m_doAutoReturns;
			private Predicate<T> m_autoReturnPredicate;
			private int m_lastAutoReturnFrame = -1;

			public PoolHandle<T> Handle => m_handle;

			public int Count => m_objects.Count;

			public int FreeCount {
				get {
					if (m_doAutoReturns && m_lastAutoReturnFrame != Time.frameCount)
						CheckAutomaticReturns();

					return m_objects.Count - m_activeCount;
				}
			}

			public int ActiveCount {
				get {
					if (m_doAutoReturns && m_lastAutoReturnFrame != Time.frameCount)
						CheckAutomaticReturns();

					return m_activeCount;
				}
			}

			public bool DoesAutomaticReturns => m_doAutoReturns;

			public event Action<T> ObjectCreated;
			public event Action<T> ObjectSetup;
			public event Action<T> ObjectReturned;

			public PocoPool() {
				m_handle = new PoolHandle<T>(this);
			}

			public T GetObject() {
				if (m_doAutoReturns && m_lastAutoReturnFrame != Time.frameCount)
					CheckAutomaticReturns();

				T @object = m_activeCount < m_objects.Count
					? m_objects[m_activeCount]
					: CreateObject();

				SetupObject(@object);

				return @object;
			}

			internal bool TryReturnObject(T @object) {
				int objectIndex = m_objects.IndexOf(@object);

				if (objectIndex == -1 || objectIndex > m_activeCount - 1)
					return false;

				TeardownObject(@object);

				m_activeCount--;
				(m_objects[objectIndex], m_objects[m_activeCount]) = (m_objects[m_activeCount], m_objects[objectIndex]);

				ObjectReturned?.Invoke(@object);
				AnyObjectReturned?.Invoke(this, @object);

				return true;
			}

			public void ReturnObject(T @object) {
				int objectIndex = m_objects.IndexOf(@object);

				if (objectIndex != -1) {
					if (objectIndex > m_activeCount - 1)
						throw new InvalidOperationException($"[ObjectPool] Unable to return {@object}. It is already free.");

					TeardownObject(@object);

					m_activeCount--;
					(m_objects[objectIndex], m_objects[m_activeCount]) = (m_objects[m_activeCount], m_objects[objectIndex]);
				}
				else {
					TeardownObject(@object);

					m_objects.Add(@object);
				}

				ObjectReturned?.Invoke(@object);
				AnyObjectReturned?.Invoke(this, @object);
			}

			public void Clear() {
				for (int i = m_objects.Count - 1; i >= 0; i--) {
					T @object = m_objects[i];

					if (i < m_activeCount)
						TeardownObject(@object);
				}

				m_objects.Clear();
				m_activeCount = 0;

				PoolReset?.Invoke(this);
			}

			public void EnableAutomaticReturns(Predicate<T> predicate) {
				m_doAutoReturns = true;
				m_autoReturnPredicate = predicate;
			}

			public void CheckAutomaticReturns() {
				m_lastAutoReturnFrame = Time.frameCount;

				for (int i = m_activeCount - 1; i >= 0; i--) {
					if (m_autoReturnPredicate.Invoke(m_objects[i]))
						ReturnObject(m_objects[i]);
				}
			}

			public void DisableAutomaticReturns() {
				m_doAutoReturns = false;
				m_autoReturnPredicate = null;
			}

			private T CreateObject() {
				T @object = new T();

				m_objects.Add(@object);

				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Create();

				ObjectCreated?.Invoke(@object);

				return @object;
			}

			private void SetupObject(T @object) {
				m_activeCount++;

				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Setup();

				ObjectSetup?.Invoke(@object);
				AnyObjectSetup?.Invoke(this, @object);
			}

			private void TeardownObject(T @object) {
				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Teardown();
			}
		}
	}
}
