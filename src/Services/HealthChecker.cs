#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Performs per-tenant health checks: verifies database file accessibility,
/// connection acquisition latency, schema integrity, and disk space around
/// tenant database directories. Aggregates individual results into an overall
/// system health report.
/// </summary>
/// <remarks>
/// Implements <see cref="IHealthCheck"/> so it can be registered with ASP.NET Core health checks,
/// e.g. <c>builder.Services.AddHealthChecks().AddCheck&lt;HealthChecker&gt;("sqlite-multi-tenant");</c>.
/// The result's <see cref="HealthCheckResult.Data"/> carries a per-tenant <see cref="TenantHealthReport"/>.
/// </remarks>
public sealed class HealthChecker : IHealthCheck {
    private readonly ITenantService _tenantService;
    private readonly IConnectionPoolManager _connectionPool;
    private readonly ILogger<HealthChecker> _logger;

    private static readonly TimeSpan DefaultLatencyThreshold = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthChecker"/> class.
    /// </summary>
    /// <param name="tenantService">Service to enumerate tenants.</param>
    /// <param name="connectionPool">Connection pool for probing tenant connectivity.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public HealthChecker(
        ITenantService tenantService,
        IConnectionPoolManager connectionPool,
        ILogger<HealthChecker> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs <see cref="CheckAllTenantsAsync"/> and maps the outcome onto an ASP.NET Core health result.
    /// The result is <see cref="HealthStatus.Unhealthy"/> when any tenant fails. <c>Data</c> contains
    /// <c>tenantCount</c>, <c>unhealthyCount</c>, and <c>tenants</c> (a dictionary of <see cref="TenantHealthReport"/> keyed by tenant ID).
    /// </summary>
    /// <param name="context">The health check context supplied by the health check framework.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TenantHealthReport> reports;
        try
        {
            reports = await CheckAllTenantsAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Tenant health check could not enumerate tenants");
            return HealthCheckResult.Unhealthy("Tenant health check failed to run.", ex);
        }

        var unhealthy = reports.Count(r => !r.IsHealthy);
        var data = new Dictionary<string, object>
        {
            ["tenantCount"] = reports.Count,
            ["unhealthyCount"] = unhealthy,
            ["tenants"] = reports.ToDictionary(r => r.TenantId)
        };

        return unhealthy == 0
            ? HealthCheckResult.Healthy($"All {reports.Count} tenant database(s) are healthy.", data)
            : HealthCheckResult.Unhealthy($"{unhealthy} of {reports.Count} tenant database(s) are unhealthy.", data: data);
    }

    /// <summary>
    /// Probes every tenant known to <see cref="ITenantService"/> and returns one structured report per tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A report per tenant, in the order returned by the tenant service.</returns>
    public async Task<IReadOnlyList<TenantHealthReport>> CheckAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var reports = new List<TenantHealthReport>(tenants.Count);

        foreach (var tenant in tenants)
            reports.Add(await CheckTenantReportAsync(tenant, cancellationToken));

        return reports;
    }

    /// <summary>
    /// Builds a <see cref="TenantHealthReport"/> for one tenant: file existence and size, <c>PRAGMA integrity_check</c>,
    /// <c>PRAGMA user_version</c> (schema version), connection pool utilization, and the most recent backup time.
    /// </summary>
    /// <param name="tenant">The tenant to probe.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The structured report. Problems are captured in <see cref="TenantHealthReport.Error"/>, not thrown.</returns>
    /// <remarks>
    /// The file is checked before any connection is opened, because SQLite would create an empty database
    /// at a missing path.
    /// </remarks>
    public async Task<TenantHealthReport> CheckTenantReportAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var report = new TenantHealthReport
        {
            TenantId = tenant.TenantId,
            Name = tenant.Name,
            LastBackupAt = tenant.Databases.Max(d => d.LastBackupAt),
            CheckedAt = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath))
            return report with { Error = "No database path configured." };

        var fileInfo = new FileInfo(tenant.DatabasePath);
        if (!fileInfo.Exists)
            return report with { Error = $"Database file not found: {tenant.DatabasePath}" };

        report = report with { FileExists = true, FileSizeBytes = fileInfo.Length };

        string? integrityResult = null;
        long? schemaVersion = null;
        string? error = null;
        SQLiteConnection? connection = null;

        try
        {
            connection = await _connectionPool.AcquireAsync(tenant.TenantId, $"Data Source={tenant.DatabasePath};", cancellationToken);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA integrity_check";
                integrityResult = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version";
                schemaVersion = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            error = ex.Message;
            _logger.LogWarning(ex, "Health probe failed for tenant '{TenantId}'", tenant.TenantId);
        }
        finally
        {
            if (connection is not null)
                await _connectionPool.ReleaseAsync(tenant.TenantId, connection);
        }

        report = report with
        {
            IntegrityCheckResult = integrityResult,
            SchemaVersion = schemaVersion,
            Error = error,
            IsHealthy = error is null && string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase)
        };

        if (_connectionPool.GetStatistics().TryGetValue(tenant.TenantId, out var pool))
        {
            report = report with
            {
                PoolTotal = pool.Total,
                PoolIdle = pool.Available,
                PoolLeased = Math.Max(0, pool.Total - pool.Available),
                PoolWaiting = pool.Waiting
            };
        }

        if (!report.IsHealthy && error is null)
            report = report with { Error = $"integrity_check returned: {integrityResult ?? "no result"}" };

        return report;
    }

    /// <summary>
    /// Checks connectivity to a single tenant database by acquiring a connection and
    /// executing a lightweight query (<c>SELECT 1</c>).
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="connectionString">SQLite connection string for the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TenantHealthResult"/> with latency and status information.</returns>
    public async Task<TenantHealthResult> CheckTenantAsync(
        string tenantId,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        var sw = Stopwatch.StartNew();
        try
        {
            var connection = await _connectionPool.AcquireAsync(tenantId, connectionString, cancellationToken);
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                sw.Stop();

                return new TenantHealthResult
                {
                    TenantId = tenantId,
                    IsHealthy = true,
                    LatencyMs = sw.ElapsedMilliseconds,
                    CheckedAt = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                await _connectionPool.ReleaseAsync(tenantId, connection);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Health check failed for tenant '{TenantId}'", tenantId);

            return new TenantHealthResult
            {
                TenantId = tenantId,
                IsHealthy = false,
                LatencyMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Runs an SQLite integrity check (<c>PRAGMA integrity_check</c>) on a tenant database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="connectionString">SQLite connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the database passes integrity check; false otherwise.</returns>
    public async Task<bool> CheckIntegrityAsync(
        string tenantId,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionPool.AcquireAsync(tenantId, connectionString, cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var isOk = string.Equals(result?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);

            if (!isOk)
                _logger.LogWarning("Integrity check failed for tenant '{TenantId}': {Result}", tenantId, result);

            return isOk;
        }
        finally
        {
            await _connectionPool.ReleaseAsync(tenantId, connection);
        }
    }

    /// <summary>
    /// Checks available disk space at the specified path and returns whether it exceeds
    /// the given minimum threshold.
    /// </summary>
    /// <param name="path">Path to check (typically the tenant database directory).</param>
    /// <param name="minimumFreeBytes">Minimum required free space in bytes. Default: 1 GB.</param>
    /// <returns>True if available space is sufficient; false otherwise.</returns>
    public bool CheckDiskSpace(string path, long minimumFreeBytes = 1_073_741_824)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path)) ?? "/");
            var available = driveInfo.AvailableFreeSpace;
            var healthy = available >= minimumFreeBytes;

            if (!healthy)
            {
                _logger.LogWarning(
                    "Low disk space at '{Path}': {Available} bytes available, {Required} bytes required",
                    path, available, minimumFreeBytes);
            }

            return healthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space at '{Path}'", path);
            return false;
        }
    }

    /// <summary>
    /// Returns connection pool statistics for all tenants, flagging any pool
    /// where waiters exceed zero as unhealthy (back-pressure detected).
    /// </summary>
    /// <returns>Dictionary mapping tenant IDs to their pool health status.</returns>
    public IReadOnlyDictionary<string, bool> CheckPoolHealth()
    {
        var stats = _connectionPool.GetStatistics();
        var result = new Dictionary<string, bool>(stats.Count);

        foreach (var (tenantId, snapshot) in stats)
        {
            var healthy = snapshot.Waiting == 0;
            result[tenantId] = healthy;

            if (!healthy)
                _logger.LogWarning("Pool back-pressure detected for tenant '{TenantId}': {Waiting} waiters",
                    tenantId, snapshot.Waiting);
        }

        return result;
    }
}

/// <summary>
/// Result of a single tenant health check probe.
/// </summary>
public sealed class TenantHealthResult
{
    /// <summary>The tenant identifier that was checked.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Whether the tenant database responded successfully.</summary>
    public bool IsHealthy { get; init; }

    /// <summary>Round-trip latency of the health probe in milliseconds.</summary>
    public long LatencyMs { get; init; }

    /// <summary>Error message if the check failed; null on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>UTC timestamp when the check was performed.</summary>
    public DateTimeOffset CheckedAt { get; init; }
}
