using System;

namespace Jackey.ObjectPool {
	internal interface IPool {
		/// <summary>
		/// The total amount of objects currently in the pool
		/// </summary>
		int Count { get; }

		/// <summary>
		/// The total amount of objects ready for use within the pool
		/// </summary>
		int FreeCount { get; }

		/// <summary>
		/// The total amount of objects currently active within the pool
		/// </summary>
		int ActiveCount { get; }

		/// <summary>
		/// Delete all objects associated with the pool
		/// </summary>
		void Clear();
	}

	internal interface IPool<T> : IPool {
		/// <summary>
		/// Does the pool automatically return objects when they are no longer active
		/// </summary>
		bool DoesAutomaticReturns { get; }

		/// <summary>
		/// Event raised whenever an object is created
		/// </summary>
		event Action<T> ObjectCreated;

		/// <summary>
		/// Event raised whenever an object is preparing to be used
		/// </summary>
		event Action<T> ObjectSetup;

		/// <summary>
		/// Event raised whenever an object is returned to the pool
		/// either manually or automatically if <see cref="DoesAutomaticReturns"/> are enabled
		/// </summary>
		event Action<T> ObjectReturned;

		/// <summary>
		/// Get a free object from the pool, otherwise create one
		/// </summary>
		T GetObject();

		/// <summary>
		/// Return an object back to the pool
		/// </summary>
		void ReturnObject(T @object);

		/// <summary>
		/// Enable objects returning to pool automatically
		/// </summary>
		/// <param name="predicate">Predicate deciding if the object should be returned to the pool or not</param>
		void EnableAutomaticReturns(Predicate<T> predicate);

		/// <summary>
		/// Manually trigger a automatic return check
		/// </summary>
		void CheckAutomaticReturns();

		/// <summary>
		/// Disable objects returning to pool automatically
		/// </summary>
		void DisableAutomaticReturns();
	}
}
