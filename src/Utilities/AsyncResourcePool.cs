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
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A pooled resource. This method is thread-safe.</returns>
        public async Task<PooledResource<T>> AcquireAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Acquiring resource. Current pool count: {AvailableResources}, Total created: {TotalCreated}", _pool.Count, _totalCreated);
            await _semaphore.WaitAsync(cancellationToken);

            T resource;

            if (_pool.TryTake(out resource))
            {
                _logger.LogInformation("Acquired resource from pool.");
                return new PooledResource<T>(resource, ReleaseResourceAsync);
            }

            try
            {
                resource = await _resourceFactory();
                Interlocked.Increment(ref _totalCreated);

                _logger.LogInformation("Created new resource from factory. Total created: {Count}", _totalCreated);

                return new PooledResource<T>(resource, ReleaseResourceAsync);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create resource from factory.");
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
        /// Gets the current pool statistics.
        /// </summary>
        /// <returns>An object containing the current pool statistics. This method is thread-safe.</returns>
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
        /// Returns a string representation of the current pool state.
        /// </summary>
        /// <returns>A string containing the pool's available resources, total created, waiting requests, and max pool size.</returns>
        public override string ToString()
        {
            return $"AsyncResourcePool {{ AvailableResources = {_pool.Count}, TotalCreated = {_totalCreated}, WaitingRequests = {(_semaphore.CurrentCount == 0 ? 1 : 0)}, MaxPoolSize = {_maxPoolSize} }}";
        }

        /// <summary>
        /// Clears the pool and disposes all currently available resources.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        public async Task ClearAsync()
        {
            _logger.LogInformation("Clearing resource pool. Current pool count: {AvailableResources}", _pool.Count);
            while (_pool.TryTake(out var resource))
            {
                await _resourceDisposer(resource);
            }

            _logger.LogInformation("Resource pool cleared");
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AsyncResourcePool{T}"/>.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
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
        /// <remarks>This property is thread-safe.</remarks>
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
        /// <remarks>This method is thread-safe.</remarks>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await _onDispose(_resource);
            _disposed = true;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PooledResource{T}"/>.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
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
