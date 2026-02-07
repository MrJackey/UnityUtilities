# 1.1.0 (2026-02-07)

### Features
- All methods previously found on `PoolHandle<T>` are now available on `IPool<T>` instead. The referenced `IPool<T>` can be retrieved using `ObjectPool.GetGameObjectPool<T>()` or `ObjectPool.GetPocoPool<T>()` depending on object type.

### Fixes
- Added handling of GameObject PoolHandles referencing cleaned up pools. (This could happen if the handle persisted through scene unloads when the pool did not)
