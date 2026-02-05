using System;

namespace Jackey.ObjectPool {
	public class PoolHandle<T> {
		private readonly IPool<T> m_pool;

		internal IPool<T> Pool => m_pool;

		/// <inheritdoc cref="IPool{T}.Count"/>
		public int Count => m_pool.Count;

		/// <inheritdoc cref="IPool{T}.FreeCount"/>
		public int FreeCount => m_pool.FreeCount;

		/// <inheritdoc cref="IPool{T}.ActiveCount"/>
		public int ActiveCount => m_pool.ActiveCount;

		/// <inheritdoc cref="IPool{T}.DoesAutomaticReturns"/>
		public bool DoesAutomaticReturns => m_pool.DoesAutomaticReturns;

		/// <inheritdoc cref="IPool{T}.ObjectCreated"/>
    public event Action<T> ObjectCreated {
        add => m_pool.ObjectCreated += value;
        remove => m_pool.ObjectCreated -= value;
    }

		/// <inheritdoc cref="IPool{T}.ObjectSetup"/>
    public event Action<T> ObjectSetup {
        add => m_pool.ObjectSetup += value;
        remove => m_pool.ObjectSetup -= value;
    }

		/// <inheritdoc cref="IPool{T}.ObjectReturned"/>
    public event Action<T> ObjectReturned {
	    add => m_pool.ObjectReturned += value;
        remove => m_pool.ObjectReturned -= value;
    }

		internal PoolHandle(IPool<T> pool) {
			m_pool = pool;
		}

		/// <inheritdoc cref="IPool{T}.GetObject"/>
		public T GetObject() {
			return m_pool.GetObject();
		}

		/// <inheritdoc cref="IPool{T}.ReturnObject"/>
		public void ReturnObject(T @object) {
			if (@object == null)
				throw new ArgumentNullException(nameof(@object), "The object you want to return is null");

			m_pool.ReturnObject(@object);
		}

		/// <inheritdoc cref="IPool{T}.Clear"/>
		public void Clear() {
			m_pool.Clear();
		}

		/// <inheritdoc cref="IPool{T}.EnableAutomaticReturns"/>
		public void EnableAutomaticReturns(Predicate<T> predicate) => m_pool.EnableAutomaticReturns(predicate);

		/// <inheritdoc cref="IPool{T}.CheckAutomaticReturns"/>
		public void CheckAutomaticReturns() {
			if (!DoesAutomaticReturns) return;

			m_pool.CheckAutomaticReturns();
		}

		/// <inheritdoc cref="IPool{T}.DisableAutomaticReturns"/>
		public void DisableAutomaticReturns() => m_pool.DisableAutomaticReturns();
	}
}
