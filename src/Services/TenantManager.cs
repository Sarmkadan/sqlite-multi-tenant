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
    /// <param name="tenantService">Service for CRUD operations on tenants.</param>
    /// <param name="connectionPool">Per-tenant connection pool manager.</param>
    /// <param name="backupService">Backup service for pre-decommission snapshots.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
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
    /// <param name="name">Display name for the tenant.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="contactEmail">Optional contact email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly provisioned <see cref="Tenant"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
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
    /// <param name="tenantId">The tenant identifier to suspend.</param>
    /// <param name="reason">Human-readable suspension reason for audit logs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
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
    /// <param name="tenantId">The tenant identifier to reactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the tenant is not in a suspendable state.</exception>
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
    /// <param name="tenantId">The tenant identifier to decommission.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the final backup file, or null if backup was skipped.</returns>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
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
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>Pool statistics, or null if the tenant has no active pool.</returns>
    public PoolStatisticsSnapshot? GetTenantPoolStatistics(string tenantId)
    {
        var stats = _connectionPool.GetStatistics();
        return stats.TryGetValue(tenantId, out var snapshot) ? snapshot : null;
    }

    /// <inheritdoc />
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
