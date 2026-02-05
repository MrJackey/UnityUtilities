namespace Jackey.ObjectPool {
	public class PoolHandle<T> {
		internal T Original { get; set; }
		internal IPool<T> Pool { get; set; }

		internal bool IsValid { get; set; } = true;

		internal PoolHandle(IPool<T> pool) {
			Pool = pool;
		}

		internal PoolHandle(T original, IPool<T> pool) {
			Original = original;
			Pool = pool;
		}
	}
}
