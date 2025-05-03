// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Utilities
{
    // Generic resource pool for managing expensive resource creation and reuse
    // Useful for database connections, HTTP clients, and other pooled resources
    public class AsyncResourcePool<T> : IDisposable where T : class
    {
        private readonly Func<Task<T>> _resourceFactory;
        private readonly Func<T, Task> _resourceDisposer;
        private readonly ILogger<AsyncResourcePool<T>> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<T> _pool;
        private readonly int _maxPoolSize;
        private int _totalCreated;

        public AsyncResourcePool(Func<Task<T>> resourceFactory, Func<T, Task> resourceDisposer,
            ILogger<AsyncResourcePool<T>> logger, int maxPoolSize = 10)
        {
            _resourceFactory = resourceFactory ?? throw new ArgumentNullException(nameof(resourceFactory));
            _resourceDisposer = resourceDisposer ?? throw new ArgumentNullException(nameof(resourceDisposer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxPoolSize = maxPoolSize;
            _pool = new ConcurrentBag<T>();
            _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
        }

        // Acquires a resource from the pool
        public async Task<PooledResource<T>> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            T resource;

            if (_pool.TryTake(out resource))
            {
                return new PooledResource<T>(resource, ReleaseResourceAsync);
            }

            try
            {
                resource = await _resourceFactory();
                Interlocked.Increment(ref _totalCreated);

                _logger.LogDebug("Created new resource from factory. Total created: {Count}", _totalCreated);

                return new PooledResource<T>(resource, ReleaseResourceAsync);
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }

        // Releases a resource back to the pool
        private async Task ReleaseResourceAsync(T resource)
        {
            if (resource != null && _pool.Count < _maxPoolSize)
            {
                _pool.Add(resource);
                _logger.LogDebug("Resource returned to pool. Available: {Count}", _pool.Count);
            }
            else
            {
                await _resourceDisposer(resource);
                _logger.LogDebug("Resource disposed. Pool at capacity or resource null");
            }

            _semaphore.Release();
        }

        // Gets pool statistics
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                AvailableResources = _pool.Count,
                TotalCreated = _totalCreated,
                WaitingRequests = _semaphore.CurrentCount == 0 ? 1 : 0,
                MaxPoolSize = _maxPoolSize
            };
        }

        // Clears the pool and disposes all resources
        public async Task ClearAsync()
        {
            while (_pool.TryTake(out var resource))
            {
                await _resourceDisposer(resource);
            }

            _logger.LogInformation("Resource pool cleared");
        }

        public void Dispose()
        {
            ClearAsync().GetAwaiter().GetResult();
            _semaphore?.Dispose();
        }
    }

    // Disposable wrapper for pooled resources
    public class PooledResource<T> : IAsyncDisposable, IDisposable where T : class
    {
        private readonly T _resource;
        private readonly Func<T, Task> _onDispose;
        private bool _disposed;

        public T Resource => _resource;

        public PooledResource(T resource, Func<T, Task> onDispose)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await _onDispose(_resource);
            _disposed = true;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _onDispose(_resource).GetAwaiter().GetResult();
            _disposed = true;
        }
    }

    public class PoolStatistics
    {
        public int AvailableResources { get; set; }
        public int TotalCreated { get; set; }
        public int WaitingRequests { get; set; }
        public int MaxPoolSize { get; set; }
    }
}
