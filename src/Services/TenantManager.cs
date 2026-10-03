#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Orchestrates tenant lifecycle operations: provisioning, suspension, reactivation,
/// and decommissioning. Coordinates between <see cref="ITenantService"/>,
/// <see cref="IConnectionPoolManager"/>, and <see cref="IBackupService"/> to ensure
/// each step is executed in the correct order with proper resource cleanup.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Threading Guarantees:</strong>
/// This class is thread-safe for concurrent operations involving different tenants.
/// It uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> of <see cref="SemaphoreSlim"/> 
/// instances to serialize lifecycle operations (such as suspension and decommissioning) 
/// on a per-tenant basis.
/// </para>
/// <para>
/// Concurrent operations targeted at the <em>same</em> tenant will be queued sequentially 
/// through the per-tenant lock to prevent race conditions during state transitions or database 
/// modifications.
/// </para>
/// <para>
/// The <see cref="Dispose"/> method is not thread-safe and must not be called concurrently 
/// with any other operations on this instance. After calling <see cref="Dispose"/>, 
/// calling any other methods may result in undefined behavior or <see cref="ObjectDisposedException"/>.
/// </para>
/// </remarks>
public sealed class TenantManager : IDisposable {
    private readonly ITenantService _tenantService;
    private readonly IConnectionPoolManager _connectionPool;
    private readonly IBackupService _backupService;
    private readonly ILogger<TenantManager> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantManager"/> class.
    /// </summary>
    /// <param name="tenantService">The service used for CRUD operations on tenants.</param>
    /// <param name="connectionPool">The connection pool manager handling per-tenant database connections.</param>
    /// <param name="backupService">The backup service for creating pre-decommission snapshots.</param>
    /// <param name="logger">The logger instance for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is <see langword="null"/>.</exception>
    /// <remarks>
    /// The injected dependencies are stored and utilized throughout the lifetime of this 
    /// <see cref="TenantManager"/> instance. These dependencies are expected to remain active 
    /// and disposed of externally according to their registration lifecycle.
    /// </remarks>
    public TenantManager(
        ITenantService tenantService,
        IConnectionPoolManager connectionPool,
        IBackupService backupService,
        ILogger<TenantManager> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Provisions a new tenant: creates the tenant record, initializes its database,
    /// and warms up its connection pool.
    /// </summary>
    /// <param name="name">The display name for the tenant. Must not be null, empty, or whitespace.</param>
    /// <param name="description">An optional description of the tenant.</param>
    /// <param name="contactEmail">An optional contact email address for the tenant.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous provisioning operation. The task result contains the newly provisioned <see cref="Tenant"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method does not acquire a per-tenant lock since the tenant does not exist prior to this call, 
    /// making the operation inherently independent of existing tenants.
    /// </para>
    /// <para>
    /// It delegates the creation to the underlying <see cref="ITenantService"/> which persists 
    /// the registry entry and handles database creation.
    /// </para>
    /// </remarks>
    public async Task<Tenant> ProvisionTenantAsync(
        string name,
        string? description = null,
        string? contactEmail = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        _logger.LogInformation("Provisioning tenant '{Name}'", name);

        var tenant = await _tenantService.CreateTenantAsync(name, description, contactEmail, cancellationToken);
        _logger.LogInformation("Tenant '{TenantId}' provisioned successfully", tenant.TenantId);

        return tenant;
    }

    /// <summary>
    /// Suspends an active tenant: marks it as <see cref="TenantStatus.Suspended"/>
    /// and evicts all its pooled connections so no new queries can run.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to suspend.</param>
    /// <param name="reason">A human-readable suspension reason to be logged for audit purposes.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous suspension operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant matches the provided <paramref name="tenantId"/>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This operation is protected by a per-tenant lock (<see cref="SemaphoreSlim"/>). If another 
    /// operation (such as suspend or decommission) is concurrently executing for the same tenant, 
    /// this call will block until the lock is acquired.
    /// </para>
    /// <para>
    /// If the tenant is already suspended, the operation returns immediately without warning or error. 
    /// After suspending the tenant in the registry, the tenant's connection pool is evicted, 
    /// closing all open and cached connections.
    /// </para>
    /// </remarks>
    public async Task SuspendTenantAsync(string tenantId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        var semaphore = _tenantLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken)
                ?? throw new TenantNotFoundException(tenantId);

            if (tenant.Status == TenantStatus.Suspended)
            {
                _logger.LogWarning("Tenant '{TenantId}' is already suspended", tenantId);
                return;
            }

            await _tenantService.SuspendTenantAsync(tenantId, cancellationToken);
            await _connectionPool.EvictTenantAsync(tenantId);

            _logger.LogInformation("Tenant '{TenantId}' suspended. Reason: {Reason}", tenantId, reason);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Reactivates a suspended tenant by setting its status back to <see cref="TenantStatus.Active"/>.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to reactivate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous reactivation operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant matches the provided <paramref name="tenantId"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is not in a reactivatable state (i.e., not <see cref="TenantStatus.Suspended"/> or <see cref="TenantStatus.Inactive"/>).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// Reactivation transitions the tenant registry status back to active, allowing the connection pool 
    /// manager to accept new connections for this tenant on subsequent requests.
    /// </para>
    /// </remarks>
    public async Task ReactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken)
            ?? throw new TenantNotFoundException(tenantId);

        if (tenant.Status != TenantStatus.Suspended && tenant.Status != TenantStatus.Inactive)
            throw new InvalidOperationException(
                $"Tenant '{tenantId}' is in state '{tenant.Status}' and cannot be reactivated.");

        await _tenantService.ActivateTenantAsync(tenantId, cancellationToken);
        _logger.LogInformation("Tenant '{TenantId}' reactivated", tenantId);
    }

    /// <summary>
    /// Decommissions a tenant: creates a final backup, evicts pooled connections,
    /// marks the tenant as <see cref="TenantStatus.Deleted"/>, and removes it from the registry.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to decommission.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous decommission operation. The task result contains the path to the final backup file, or <see langword="null"/> if the backup failed or was skipped.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant matches the provided <paramref name="tenantId"/>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This operation is protected by a per-tenant lock (<see cref="SemaphoreSlim"/>). If another 
    /// operation (such as suspend or decommission) is concurrently executing for the same tenant, 
    /// this call will block until the lock is acquired.
    /// </para>
    /// <para>
    /// A final full database backup is attempted before decommission. If the backup creation fails due to 
    /// an exception, the failure is caught, logged as a warning, and the decommission proceeds to ensure 
    /// that faulty databases or backup configurations do not block decommissioning.
    /// </para>
    /// <para>
    /// After decommissioning, any active connections in the pool are evicted, the tenant's registry status 
    /// is set to inactive and then deleted, and the per-tenant semaphore is cleaned up.
    /// </para>
    /// </remarks>
    public async Task<string?> DecommissionTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        var semaphore = _tenantLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken)
                ?? throw new TenantNotFoundException(tenantId);

            _logger.LogInformation("Decommissioning tenant '{TenantId}'", tenantId);

            string? backupPath = null;
            try
            {
                var backup = await _backupService.CreateBackupAsync(tenantId, BackupType.Full, "TenantManager", cancellationToken: cancellationToken);
                backupPath = backup.BackupId;
                _logger.LogInformation("Final backup for tenant '{TenantId}' created: '{BackupId}'", tenantId, backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Final backup for tenant '{TenantId}' failed; proceeding with decommission", tenantId);
            }

            await _connectionPool.EvictTenantAsync(tenantId);
            await _tenantService.DeactivateTenantAsync(tenantId, cancellationToken);
            await _tenantService.DeleteTenantAsync(tenantId, cancellationToken);

            _tenantLocks.TryRemove(tenantId, out _);
            _logger.LogInformation("Tenant '{TenantId}' fully decommissioned", tenantId);

            return backupPath;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Returns a snapshot of connection pool statistics for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>The <see cref="PoolStatisticsSnapshot"/> containing pool details, or <see langword="null"/> if the tenant has no active connection pool.</returns>
    /// <remarks>
    /// <para>
    /// This operation does not acquire a tenant lock and is safe to call concurrently. It returns a 
    /// point-in-time snapshot from the connection pool manager.
    /// </para>
    /// </remarks>
    public PoolStatisticsSnapshot? GetTenantPoolStatistics(string tenantId)
    {
        var stats = _connectionPool.GetStatistics();
        return stats.TryGetValue(tenantId, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="TenantManager"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposes all active per-tenant <see cref="SemaphoreSlim"/> instances and clears 
    /// the internal tracking registry.
    /// </para>
    /// <para>
    /// This method is not thread-safe. Calling it while other lifecycle operations are in progress 
    /// on other threads may result in undefined behavior, <see cref="ObjectDisposedException"/>, 
    /// or resource leaks.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _tenantLocks)
        {
            kvp.Value.Dispose();
        }
        _tenantLocks.Clear();
    }
}
