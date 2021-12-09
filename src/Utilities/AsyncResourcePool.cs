#nullable enable
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
    /// <summary>
    /// Generic resource pool for managing expensive resource creation and reuse.
    /// Useful for database connections, HTTP clients, and other pooled resources.
    /// </summary>
    /// <typeparam name="T">Type of resource to be pooled.</typeparam>
    public sealed class AsyncResourcePool<T> : IDisposable where T : class
    {
        private readonly Func<Task<T>> _resourceFactory;
        private readonly Func<T, Task> _resourceDisposer;
        private readonly ILogger<AsyncResourcePool<T>> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<T> _pool;
        private readonly int _maxPoolSize;
        private int _totalCreated;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResourcePool{T}"/> class.
        /// </summary>
        /// <param name="resourceFactory">Factory method to create a new resource.</param>
        /// <param name="resourceDisposer">Method to dispose of a resource.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="maxPoolSize">Maximum number of resources to be pooled.</param>
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

        /// <summary>
        /// Acquires a resource from the pool.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A pooled resource.</returns>
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

        /// <summary>
        /// Releases a resource back to the pool.
        /// </summary>
        /// <param name="resource">Resource to be released.</param>
        private async Task ReleaseResourceAsync(T resource)
        {
            if (resource is not null && _pool.Count < _maxPoolSize)
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

        /// <summary>
        /// Gets pool statistics.
        /// </summary>
        /// <returns>Pool statistics.</returns>
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

        /// <summary>
        /// Clears the pool and disposes all resources.
        /// </summary>
        public async Task ClearAsync()
        {
            while (_pool.TryTake(out var resource))
            {
                await _resourceDisposer(resource);
            }

            _logger.LogInformation("Resource pool cleared");
        }

        /// <summary>
        /// Releases unmanaged resources and performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ClearAsync().GetAwaiter().GetResult();
            _semaphore?.Dispose();
        }
    }

    /// <summary>
    /// Disposable wrapper for pooled resources.
    /// </summary>
    /// <typeparam name="T">Type of resource.</typeparam>
    public sealed class PooledResource<T> : IAsyncDisposable, IDisposable where T : class
    {
        private readonly T _resource;
        private readonly Func<T, Task> _onDispose;
        private bool _disposed;

        /// <summary>
        /// Gets the pooled resource.
        /// </summary>
        public T Resource => _resource;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledResource{T}"/> class.
        /// </summary>
        /// <param name="resource">Pooled resource.</param>
        /// <param name="onDispose">Method to dispose of the resource.</param>
        public PooledResource(T resource, Func<T, Task> onDispose)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await _onDispose(_resource);
            _disposed = true;
        }

        /// <summary>
        /// Releases unmanaged resources and performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _onDispose(_resource).GetAwaiter().GetResult();
            _disposed = true;
        }
    }

    /// <summary>
    /// Represents pool statistics.
    /// </summary>
    public sealed class PoolStatistics
    {
        /// <summary>
        /// Gets the number of available resources in the pool.
        /// </summary>
        public int AvailableResources { get; set; }

        /// <summary>
        /// Gets the total number of resources created.
        /// </summary>
        public int TotalCreated { get; set; }

        /// <summary>
        /// Gets the number of waiting requests.
        /// </summary>
        public int WaitingRequests { get; set; }

        /// <summary>
        /// Gets the maximum pool size.
        /// </summary>
        public int MaxPoolSize { get; set; }
    }
}
