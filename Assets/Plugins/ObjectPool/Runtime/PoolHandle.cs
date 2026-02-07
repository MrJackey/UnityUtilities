namespace Jackey.ObjectPool {
	public class PoolHandle<T> {
		internal IPool<T> Pool { get; set; }

		internal PoolHandle(IPool<T> pool) {
			Pool = pool;
		}
	}
}
