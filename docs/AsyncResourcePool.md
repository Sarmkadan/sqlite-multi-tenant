# AsyncResourcePool

`AsyncResourcePool<T>` provides a thread-safe, asynchronous mechanism for managing a pool of reusable resources of type `T`. It is designed to mitigate the overhead of repeatedly creating and destroying expensive resources by maintaining a set of available instances that can be acquired and released as needed. The pool enforces capacity constraints and manages resource lifecycles, ensuring efficient resource utilization in high-concurrency environments.

## API

### `AsyncResourcePool<T>`

`public sealed class AsyncResourcePool<T> : IDisposable where T : class`

Represents the core pool manager for resources of type `T`.

#### `public AsyncResourcePool(Func<Task<T>> factory, int maxPoolSize)`
Initializes a new instance of the pool.
- **factory**: An asynchronous delegate used to create new instances of `T` when the pool is empty and capacity is available.
- **maxPoolSize**: The maximum number of resources that can be held or created by this pool.

#### `public async Task<PooledResource<T>> AcquireAsync(CancellationToken cancellationToken = default)`
Asynchronously acquires a resource from the pool. If no resource is available and the pool is below its maximum capacity, a new resource is created using the factory. If the pool is at maximum capacity and all resources are in use, the method asynchronously waits until a resource is returned.
- **cancellationToken**: A token to observe for cancellation of the acquisition request.
- **Returns**: A `PooledResource<T>` wrapper that provides access to the resource.

#### `public PoolStatistics GetStatistics()`
Retrieves the current state of the pool.
- **Returns**: A `PoolStatistics` object containing metrics about resource usage.

#### `public async Task ClearAsync()`
Cleans up the pool by disposing of all currently available idle resources. Resources currently in use are not affected until they are returned to the pool and subsequently cleared.

#### `public void Dispose()`
Releases all resources held by the pool.

---

### `PooledResource<T>`

`public sealed class PooledResource<T> : IAsyncDisposable, IDisposable where T : class`

A wrapper class for an acquired resource that ensures it is automatically returned to the `AsyncResourcePool<T>` when disposed.

#### `public T Resource { get; }`
Gets the underlying resource instance.

#### `public async ValueTask DisposeAsync()`
Returns the resource to the pool asynchronously. This should be called via `await using` or explicitly to ensure resources are properly recycled.

#### `public void Dispose()`
Returns the resource to the pool synchronously.

---

### `PoolStatistics`

`public sealed class PoolStatistics`

Contains diagnostic information about the pool.

- **AvailableResources**: The number of idle resources currently in the pool.
- **TotalCreated**: The total number of resources created by the pool since initialization.
- **WaitingRequests**: The number of tasks currently awaiting a resource.
- **MaxPoolSize**: The configured maximum capacity of the pool.

## Usage

### Basic Usage with `await using`

The recommended way to use the pool is with the `await using` statement to ensure resources are returned promptly.

```csharp
var pool = new AsyncResourcePool<MyDatabaseConnection>(
    async () => await MyDatabaseConnection.CreateAsync(), 
    maxPoolSize: 10);

// Acquire and automatically return the resource
await using (var pooledResource = await pool.AcquireAsync())
{
    await pooledResource.Resource.ExecuteQueryAsync("SELECT 1");
}
```

### Checking Pool Health

Monitoring the pool state can help in diagnosing bottlenecks or configuring the `maxPoolSize`.

```csharp
var stats = pool.GetStatistics();
Console.WriteLine($"Available: {stats.AvailableResources}, Waiting: {stats.WaitingRequests}");
```

## Notes

- **Thread Safety**: `AsyncResourcePool<T>` is fully thread-safe. Multiple tasks can acquire and release resources concurrently without external synchronization.
- **Cancellation**: `AcquireAsync` respects the provided `CancellationToken`. If the token is cancelled while waiting for a resource, the operation will throw an `OperationCanceledException`.
- **Resource Lifecycle**: The pool does not automatically dispose of resources created by the factory when the pool itself is disposed, unless the resources implement `IDisposable` or `IAsyncDisposable` and are returned to the pool to be cleared.
- **Capacity**: If the pool reaches `maxPoolSize` and all resources are checked out, subsequent `AcquireAsync` calls will queue until a `PooledResource<T>` is disposed, at which point the resource is returned to the pool and passed to the next waiting requester.
