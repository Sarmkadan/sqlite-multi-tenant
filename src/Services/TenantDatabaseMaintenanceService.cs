#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for database maintenance operations on tenant databases.
/// Executes VACUUM, ANALYZE, and PRAGMA optimize commands to optimize SQLite database performance.
/// Reports file sizes before and after operations for monitoring and validation.
/// </summary>
public sealed class TenantDatabaseMaintenanceService : ITenantDatabaseMaintenanceService
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantDatabaseMaintenanceService> _logger;
    private readonly TimeSpan _defaultTimeout;

    public TenantDatabaseMaintenanceService(
        ITenantService tenantService,
        ILogger<TenantDatabaseMaintenanceService> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeout = TimeSpan.FromSeconds(300); // 5 minutes per database
    }

    /// <summary>
    /// Executes VACUUM on a specific tenant database to reclaim space from deleted rows.
    /// VACUUM rebuilds the database file, repacking it into a minimal amount of disk space.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with before/after sizes and duration.</returns>
    public async Task<TenantMaintenanceResult> VacuumTenantDatabaseAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new TenantNotFoundException(tenantId);

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        var result = new TenantMaintenanceResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            Operation = "VACUUM",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Get initial file size
            var initialSize = new FileInfo(tenant.DatabasePath).Length;
            result.SizeBeforeBytes = initialSize;
            _logger.LogInformation("Starting VACUUM on tenant {TenantId} ({TenantName}), initial size: {Size} bytes",
                tenantId, tenant.Name, initialSize);

            // Execute VACUUM
            var vacuumCommand = "VACUUM;";
            await ExecuteSqlOnTenantAsync(tenant, vacuumCommand, cancellationToken, _defaultTimeout);

            // Get final file size
            var finalSize = new FileInfo(tenant.DatabasePath).Length;
            result.SizeAfterBytes = finalSize;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("VACUUM completed for tenant {TenantId}: {InitialSize} -> {FinalSize} bytes (saved: {Saved} bytes)",
                tenantId, initialSize, finalSize, result.SizeReductionBytes);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during VACUUM on tenant {TenantId}", tenantId);
            result.Error = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            throw;
        }
    }

    /// <summary>
    /// Executes VACUUM on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    public async Task<List<TenantMaintenanceResult>> VacuumAllTenantDatabasesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var results = new List<TenantMaintenanceResult>(tenants.Count);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await VacuumTenantDatabaseAsync(tenant.TenantId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to vacuum tenant {TenantId} ({TenantName})",
                    tenant.TenantId, tenant.Name);
                results.Add(new TenantMaintenanceResult
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    Operation = "VACUUM",
                    StartedAt = DateTime.UtcNow,
                    Error = ex.Message,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Executes ANALYZE on a specific tenant database to update query planner statistics.
    /// ANALYZE collects statistics about tables and indexes and stores them in the sqlite_stat1 table.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with duration.</returns>
    public async Task<TenantMaintenanceResult> AnalyzeTenantDatabaseAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new TenantNotFoundException(tenantId);

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        var result = new TenantMaintenanceResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            Operation = "ANALYZE",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting ANALYZE on tenant {TenantId} ({TenantName})", tenantId, tenant.Name);

            // Execute ANALYZE
            var analyzeCommand = "ANALYZE;";
            await ExecuteSqlOnTenantAsync(tenant, analyzeCommand, cancellationToken, _defaultTimeout);

            result.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("ANALYZE completed for tenant {TenantId}", tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ANALYZE on tenant {TenantId}", tenantId);
            result.Error = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            throw;
        }
    }

    /// <summary>
    /// Executes ANALYZE on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    public async Task<List<TenantMaintenanceResult>> AnalyzeAllTenantDatabasesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var results = new List<TenantMaintenanceResult>(tenants.Count);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await AnalyzeTenantDatabaseAsync(tenant.TenantId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze tenant {TenantId} ({TenantName})",
                    tenant.TenantId, tenant.Name);
                results.Add(new TenantMaintenanceResult
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    Operation = "ANALYZE",
                    StartedAt = DateTime.UtcNow,
                    Error = ex.Message,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Executes PRAGMA optimize on a specific tenant database to update database statistics and optimize performance.
    /// PRAGMA optimize is a convenience pragma that runs ANALYZE and other optimizations.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with duration.</returns>
    public async Task<TenantMaintenanceResult> OptimizeTenantDatabaseAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new TenantNotFoundException(tenantId);

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        var result = new TenantMaintenanceResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            Operation = "PRAGMA optimize",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting PRAGMA optimize on tenant {TenantId} ({TenantName})", tenantId, tenant.Name);

            // Execute PRAGMA optimize
            var optimizeCommand = "PRAGMA optimize;";
            await ExecuteSqlOnTenantAsync(tenant, optimizeCommand, cancellationToken, _defaultTimeout);

            result.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("PRAGMA optimize completed for tenant {TenantId}", tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PRAGMA optimize on tenant {TenantId}", tenantId);
            result.Error = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            throw;
        }
    }

    /// <summary>
    /// Executes PRAGMA optimize on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    public async Task<List<TenantMaintenanceResult>> OptimizeAllTenantDatabasesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var results = new List<TenantMaintenanceResult>(tenants.Count);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await OptimizeTenantDatabaseAsync(tenant.TenantId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize tenant {TenantId} ({TenantName})",
                    tenant.TenantId, tenant.Name);
                results.Add(new TenantMaintenanceResult
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    Operation = "PRAGMA optimize",
                    StartedAt = DateTime.UtcNow,
                    Error = ex.Message,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Executes comprehensive maintenance (VACUUM + ANALYZE + PRAGMA optimize) on a specific tenant database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with before/after sizes and duration.</returns>
    public async Task<TenantMaintenanceResult> PerformFullMaintenanceAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new TenantNotFoundException(tenantId);

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        var result = new TenantMaintenanceResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            Operation = "Full Maintenance (VACUUM + ANALYZE + PRAGMA optimize)",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Get initial file size
            var initialSize = new FileInfo(tenant.DatabasePath).Length;
            result.SizeBeforeBytes = initialSize;
            _logger.LogInformation("Starting full maintenance on tenant {TenantId} ({TenantName}), initial size: {Size} bytes",
                tenantId, tenant.Name, initialSize);

            // Execute VACUUM
            var vacuumCommand = "VACUUM;";
            await ExecuteSqlOnTenantAsync(tenant, vacuumCommand, cancellationToken, _defaultTimeout);

            // Get size after VACUUM
            var afterVacuumSize = new FileInfo(tenant.DatabasePath).Length;
            result.IntermediateSizeBytes = afterVacuumSize;

            // Execute ANALYZE
            var analyzeCommand = "ANALYZE;";
            await ExecuteSqlOnTenantAsync(tenant, analyzeCommand, cancellationToken, _defaultTimeout);

            // Execute PRAGMA optimize
            var optimizeCommand = "PRAGMA optimize;";
            await ExecuteSqlOnTenantAsync(tenant, optimizeCommand, cancellationToken, _defaultTimeout);

            // Get final file size
            var finalSize = new FileInfo(tenant.DatabasePath).Length;
            result.SizeAfterBytes = finalSize;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Full maintenance completed for tenant {TenantId}: {InitialSize} -> {FinalSize} bytes (saved: {Saved} bytes)",
                tenantId, initialSize, finalSize, result.SizeReductionBytes);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full maintenance on tenant {TenantId}", tenantId);
            result.Error = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            throw;
        }
    }

    /// <summary>
    /// Executes comprehensive maintenance on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    public async Task<List<TenantMaintenanceResult>> PerformFullMaintenanceOnAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var results = new List<TenantMaintenanceResult>(tenants.Count);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await PerformFullMaintenanceAsync(tenant.TenantId, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform full maintenance on tenant {TenantId} ({TenantName})",
                    tenant.TenantId, tenant.Name);
                results.Add(new TenantMaintenanceResult
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    Operation = "Full Maintenance",
                    StartedAt = DateTime.UtcNow,
                    Error = ex.Message,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Executes SQL command on a tenant database.
    /// </summary>
    /// <param name="tenant">The tenant.</param>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="timeout">Command timeout.</param>
    private async Task ExecuteSqlOnTenantAsync(
        Tenant tenant,
        string sql,
        CancellationToken cancellationToken,
        TimeSpan timeout)
    {
        if (tenant is null)
            throw new ArgumentNullException(nameof(tenant));

        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenant.TenantId} database file not found");

        await using var connection = new SQLiteConnection($"Data Source={tenant.DatabasePath};");
        await connection.OpenAsync(cancellationToken);

        using var command = new SQLiteCommand(sql, connection);
        command.CommandTimeout = (int)timeout.TotalSeconds;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
