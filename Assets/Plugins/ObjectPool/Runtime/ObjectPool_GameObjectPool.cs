using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.ObjectPool {
	public static partial class ObjectPool {
		internal static readonly Dictionary<Object, IPool> s_gameObjectPools = new();

		#region Actions

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The object you want to instantiate is null");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			T @object = pool.GetObject();

			return @object;
		}

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle) where T : Object {
			T @object = handle.GetObject();

			return @object;
		}

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <param name="parent">The parent of the clone</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original, Transform parent) where T : Object => Instantiate(original, parent, false);

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <param name="parent">The parent of the clone</param>
		/// <param name="instantiateInWorldSpace">When true, positions and orients the clone in world space. When false, it is relative to its parent</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The object you want to instantiate is null");

			if (original is not (GameObject or Component))
				throw new ArgumentException("[ObjectPool] Cannot instantiate an asset with a parent");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			GameObjectTransform transform = GetOriginalTransform(original);
			transform.Parent = parent;
			transform.WorldSpace = instantiateInWorldSpace;

			T @object = pool.GetObject(transform);

			return @object;
		}

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <param name="parent">The parent of the clone</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle, Transform parent) where T : Object => Instantiate(handle, parent, false);

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <param name="parent">The parent of the clone</param>
		/// <param name="instantiateInWorldSpace">When true, positions and orients the clone in world space. When false, it is relative to its parent</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle, Transform parent, bool instantiateInWorldSpace) where T : Object {
			GameObjectPool<T> pool = (GameObjectPool<T>)handle.Pool;
			T original = pool.Original;

			if (original is not (GameObject or Component))
				throw new ArgumentException("[ObjectPool] Cannot instantiate an asset with a parent");

			GameObjectTransform transform = GetOriginalTransform(original);
			transform.Parent = parent;
			transform.WorldSpace = instantiateInWorldSpace;

			T @object = pool.GetObject(transform);

			return @object;
		}

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <param name="position">The position of the clone</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original, Vector3 position) where T : Object => Instantiate(original, position, Quaternion.identity);

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <param name="position">The position of the clone</param>
		/// <param name="rotation">The orientation of the clone</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The object you want to instantiate is null");

			if (original is not (GameObject or Component))
				throw new ArgumentException("[ObjectPool] Cannot instantiate an asset with a position and rotation");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			T @object = pool.GetObject(new GameObjectTransform() {
				Position = position,
				Rotation = rotation,
				Parent = null,
				WorldSpace = true,
			});

			return @object;
		}

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <param name="position">The position of the clone</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle, Vector3 position) where T : Object => Instantiate(handle, position, Quaternion.identity);

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <param name="position">The position of the clone</param>
		/// <param name="rotation">The orientation of the clone</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle, Vector3 position, Quaternion rotation) where T : Object {
			GameObjectPool<T> pool = (GameObjectPool<T>)handle.Pool;

			if (pool.Original is not (GameObject or Component))
				throw new ArgumentException("[ObjectPool] Cannot instantiate an asset with a position and rotation");

			T @object = pool.GetObject(new GameObjectTransform() {
				Position = position,
				Rotation = rotation,
				Parent = null,
				WorldSpace = true,
			});

			return @object;
		}

		/// <summary>
		/// Get a pooled clone to the original object
		/// </summary>
		/// <param name="original">An existing object that you want get a clone of</param>
		/// <param name="position">The position of the clone</param>
		/// <param name="rotation">The orientation of the clone</param>
		/// <param name="parent">The parent of the clone</param>
		/// <returns>
		/// A clone of the original
		/// </returns>
		public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The object you want to instantiate is null");

			if (original is not (GameObject or Component))
				throw new ArgumentException("[ObjectPool] Cannot instantiate an asset with a position, rotation and parent");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			T @object = pool.GetObject(new GameObjectTransform() {
				Position = position,
				Rotation = rotation,
				Parent = parent,
				WorldSpace = true,
			});

			return @object;
		}

		/// <summary>
		/// Get a pooled clone of an object from the pool connected to the handle
		/// </summary>
		/// <param name="handle">The handle of the pool</param>
		/// <param name="position">The position of the clone</param>
		/// <param name="rotation">The orientation of the clone</param>
		/// <param name="parent">The parent of the clone</param>
		/// <returns>
		/// A clone from the handle's pool
		/// </returns>
		public static T Instantiate<T>(PoolHandle<T> handle, Vector3 position, Quaternion rotation, Transform parent) where T : Object {
			GameObjectPool<T> pool = (GameObjectPool<T>)handle.Pool;
			T @object = pool.GetObject(new GameObjectTransform() {
				Position = position,
				Rotation = rotation,
				Parent = parent,
				WorldSpace = true,
			});

			return @object;
		}

		/// <summary>
		/// Return a pooled object to its pool. It can also be used to add non-pooled objects to a pool
		/// </summary>
		/// <param name="original">The original the object is based on</param>
		/// <param name="object">The object you wish to return to its pool</param>
		public static void Destroy<T>(T original, T @object) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The original of the object you want to destroy is null");

			if (!@object)
				throw new ArgumentNullException(nameof(@object), "[ObjectPool] The object you want to destroy is null");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			pool.ReturnObject(@object);
		}

		/// <summary>
		/// Return a pooled object to its pool. It can also be used to add non-pooled objects to a pool
		/// </summary>
		/// <param name="handle">The handle connected to the pool the object is part of</param>
		/// <param name="object">The object you wish to return to its pool</param>
		public static void Destroy<T>(PoolHandle<T> handle, T @object) where T : Object {
			if (handle == null)
				throw new ArgumentNullException(nameof(handle), "[ObjectPool] The handle of the pool of the object you want to destroy is null");

			if (!@object)
				throw new ArgumentNullException(nameof(@object), "[ObjectPool] The object you want to destroy is null");

			handle.ReturnObject(@object);
		}

		/// <summary>
		/// <para>
		/// Return an object to its pool
		/// </para>
		///
		/// <para>
		/// This method is not fast and should be avoided unless really necessary.
		/// It looks through all existing pools to find which pool the object is part of.
		/// Prefer using <see cref="ObjectPool.Destroy{T}(T, T)"/> or <see cref="ObjectPool.Destroy{T}(PoolHandle{T}, T)"/> instead
		/// </para>
		/// </summary>
		/// <param name="object">The object you wish to return to its pool</param>
		public static void FindAndDestroy<T>(T @object) where T : Object {
			foreach (IPool pool in s_gameObjectPools.Values) {
				if (pool is GameObjectPool<T> gameObjectPool && gameObjectPool.TryReturnObject(@object)) {
					return;
				}
			}

			throw new InvalidOperationException("[ObjectPool] The object you want to destroy does not belong to an pool");
		}

		/// <summary>
		/// Get the handle of the pool of an original object
		/// </summary>
		/// <param name="original">The original to the pool</param>
		/// <returns>The handle connected to the pool of the original</returns>
		public static PoolHandle<T> GetHandle<T>(T original) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The original of the pool whose handle is being retrieved is null");

			GameObjectPool<T> pool = GetGameObjectPool(original);
			return pool.Handle;
		}

		/// <summary>
		/// Get the handle of a new pool of an original object. This pool is not accessible except for via the returned handle.
		/// Note that methods requiring a handle accept handles referencing private pools as well
		/// </summary>
		/// <param name="original">The original to the pool</param>
		/// <returns>The handle connected to the pool of the original</returns>
		public static PoolHandle<T> CreateLocal<T>(T original) where T : Object {
			if (!original)
				throw new ArgumentNullException(nameof(original), "[ObjectPool] The original you want to pool is null");

			GameObjectPool<T> pool = new GameObjectPool<T>(original);

			return pool.Handle;
		}

		#endregion

		private static GameObjectPool<T> GetGameObjectPool<T>(T original) where T : Object {
			if (!s_gameObjectPools.TryGetValue(original, out IPool pool)) {
				pool = new GameObjectPool<T>(original);
				s_gameObjectPools.Add(original, pool);

				PoolCreated?.Invoke(pool);
			}

			return (GameObjectPool<T>)pool;
		}

		private static GameObjectTransform GetOriginalTransform(Object original) {
			Transform originalTransform = original switch {
				GameObject gameObject => gameObject.transform,
				Component component => component.transform,
				_ => null,
			};

			Debug.Assert(originalTransform, "[ObjectPool] Tried to base a transformation on an object without a transform");

			return new GameObjectTransform() {
				Position = originalTransform.position,
				Rotation = originalTransform.rotation,
				InheritPosition = true,
			};
		}

		internal static void RemoveDestroyedGameObjects() {
			foreach (GameObjectPool pool in s_gameObjectPools.Values) {
				pool.RemoveDestroyedObjects();
			}
		}

		internal abstract class GameObjectPool {
			internal abstract void RemoveDestroyedObjects();
		}

		internal class GameObjectPool<T> : GameObjectPool, IPool<T> where T : Object {
			private readonly T m_original;
			private readonly PoolHandle<T> m_handle;

			// [active, ..., free]
			private readonly List<T> m_objects = new();
			private int m_activeCount;

			private readonly Action<T> m_objectSetup;
			private readonly Action<T> m_objectTeardown;

			private bool m_doAutoReturns;
			private Predicate<T> m_autoReturnPredicate;
			private int m_lastAutoReturnFrame = -1;

			public PoolHandle<T> Handle => m_handle;
			public T Original => m_original;

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

			public GameObjectPool(T original) {
				m_original = original;
				m_handle = new PoolHandle<T>(this);

				switch (original) {
					case GameObject:
						m_objectSetup = GameObjectSetup;
						m_objectTeardown = GameObjectTeardown;
						break;
					case Component:
						m_objectSetup = ComponentSetup;
						m_objectTeardown = ComponentTeardown;
						break;
				}

				if (m_original is IPoolCallbackReceiver<T> callbackReceiver) {
					callbackReceiver.PoolCreate(m_handle);
				}
			}

			public T GetObject() => GetObject(GetOriginalTransform(m_original));

			public T GetObject(GameObjectTransform transform) {
				if (m_doAutoReturns && m_lastAutoReturnFrame != Time.frameCount)
					CheckAutomaticReturns();

				T @object;

				if (m_activeCount < m_objects.Count) {
					@object = m_objects[m_activeCount];
					SetupObject(@object, transform);
				}
				else {
					@object = CreateObject(transform);
					SetupObject(@object);
				}

				return @object;
			}

			public bool TryReturnObject(T @object) {
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

#if UNITY_EDITOR
					if (Application.isPlaying)
						Object.Destroy(@object);
					else
						Object.DestroyImmediate(@object);
#else
					Object.Destroy(@object);
#endif
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

			internal override void RemoveDestroyedObjects() {
				for (int i = m_objects.Count - 1; i >= 0; i--) {
					T @object = m_objects[i];

					if (!@object) {
						m_objects.RemoveAt(i);

						if (i < m_activeCount) {
							m_activeCount--;
						}
					}
				}

				PoolReset?.Invoke(this);
			}

			private T CreateObject(GameObjectTransform instantiation) {
				T @object = instantiation.Create(m_original);

				m_objects.Add(@object);

				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Create();

				ObjectCreated?.Invoke(@object);

				return @object;
			}

			private void SetupObject(T @object, GameObjectTransform transformation) {
				transformation.Apply(@object);
				SetupObject(@object);
			}

			private void SetupObject(T @object) {
				m_objectSetup.Invoke(@object);

				m_activeCount++;

				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Setup();

				ObjectSetup?.Invoke(@object);
				AnyObjectSetup?.Invoke(this, @object);
			}

			private void TeardownObject(T @object) {
				if (@object is IPoolObjectCallbackReceiver callbackReceiver)
					callbackReceiver.Teardown();

				m_objectTeardown.Invoke(@object);
			}

			private void GameObjectSetup(Object @object) {
				GameObject gameObject = (GameObject)@object;
				gameObject.SetActive((m_original as GameObject).activeSelf);
			}

			private void GameObjectTeardown(Object @object) {
				GameObject gameObject = (GameObject)@object;

				gameObject.SetActive(false);

				// Returning a GameObject as part of OnDisable/OnDestroy can cause the scene to be unloaded
				// while the GameObject still exists. Changing the parent in those
				// cases causes the GameObject to not be cleaned up together with the scene.
				if (gameObject.scene.isLoaded)
					gameObject.transform.SetParent(null);
			}

			private void ComponentSetup(Object @object) {
				Component component = (Component)@object;
				component.gameObject.SetActive((m_original as Component).gameObject.activeSelf);
			}

			private void ComponentTeardown(Object @object) {
				Component component = (Component)@object;
				GameObjectTeardown(component.gameObject);
			}
		}
	}
}
