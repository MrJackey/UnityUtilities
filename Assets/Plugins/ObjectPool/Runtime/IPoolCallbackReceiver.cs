using UnityEngine;

namespace Jackey.ObjectPool {
	/// <summary>
	/// Interface providing callbacks for the pool when used with <see cref="ObjectPool"/>.
	/// </summary>
	/// <remarks>
	/// The type of T must match the type that implements the interface.
	/// </remarks>
	public interface IPoolCallbackReceiver<T> where T : Object {
		/// <summary>
		/// Invoked when a pool of this object is created.
		/// Note that the method is invoked on the original object e.g its prefab.
		/// </summary>
		/// <param name="handle">The handle connected to the created pool</param>
		void PoolCreate(PoolHandle<T> handle);
	}
}
