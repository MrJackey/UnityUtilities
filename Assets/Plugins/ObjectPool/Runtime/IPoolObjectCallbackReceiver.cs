namespace Jackey.ObjectPool {
	/// <summary>
	/// Interface providing callbacks for the pooled objects when used with <see cref="ObjectPool"/>
	/// </summary>
	public interface IPoolObjectCallbackReceiver {
		/// <summary>
		/// Invoked when created by a pool
		/// </summary>
		void Create();

		/// <summary>
		/// Invoked when about to be used via a pool
		/// </summary>
		void Setup();

		/// <summary>
		/// Invoked on return to a pool either manually or automatically
		/// </summary>
		void Teardown();
	}
}
