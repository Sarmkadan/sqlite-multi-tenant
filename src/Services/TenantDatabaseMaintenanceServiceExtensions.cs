#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Extension methods for registering TenantDatabaseMaintenanceService with dependency injection.
/// </summary>
public static class TenantDatabaseMaintenanceServiceExtensions
{
    /// <summary>
    /// Adds the tenant database maintenance service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTenantDatabaseMaintenanceService(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddScoped<ITenantDatabaseMaintenanceService, TenantDatabaseMaintenanceService>();

        return services;
    }

    /// <summary>
    /// Adds the tenant database maintenance service with configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for maintenance options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTenantDatabaseMaintenanceService(
        this IServiceCollection services,
        Action<TenantDatabaseMaintenanceOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        services.AddScoped<ITenantDatabaseMaintenanceService, TenantDatabaseMaintenanceService>();
        services.Configure(configure);

        return services;
    }
}

/// <summary>
/// Configuration options for tenant database maintenance operations.
/// Controls which maintenance operations are enabled and their behavior.
/// </summary>
public sealed class TenantDatabaseMaintenanceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable VACUUM operation to reclaim disk space.
    /// VACUUM rebuilds the database file, repacking it into a minimal amount of disk space.
    /// Default: true (recommended for production to prevent database bloat).
    /// </summary>
    public bool EnableVacuum { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable ANALYZE operation to update query statistics.
    /// ANALYZE collects statistics about tables and indexes and stores them in the sqlite_stat1 table,
    /// which improves the SQLite query planner's performance.
    /// Default: true (recommended for production to maintain optimal query performance).
    /// </summary>
    public bool EnableAnalyze { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable PRAGMA optimize operation to update database statistics.
    /// PRAGMA optimize runs ANALYZE and other database optimizations automatically.
    /// Default: true (recommended for production to ensure regular database optimization).
    /// </summary>
    public bool EnableOptimize { get; set; } = true;

    /// <summary>
    /// Gets or sets the maintenance interval in hours.
    /// Specifies how often maintenance operations should be executed on each tenant database.
    /// Default: 24 (daily maintenance cycle).
    /// Minimum: 1 (hourly maintenance).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than 1.</exception>
    public int IntervalHours
    {
        get => _intervalHours;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Interval must be at least 1 hour.");
            }

            _intervalHours = value;
        }
    }
    private int _intervalHours = 24;

    /// <summary>
    /// Gets or sets the maximum time allowed for maintenance on a single database, in seconds.
    /// This timeout applies to each individual maintenance operation (VACUUM, ANALYZE, PRAGMA optimize).
    /// Default: 300 (5 minutes per database operation).
    /// Minimum: 30 (30 seconds minimum to allow for small databases).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than 30.</exception>
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (value < 30)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be at least 30 seconds.");
            }

            _timeoutSeconds = value;
        }
    }
    private int _timeoutSeconds = 300;

    /// <summary>
    /// Gets or sets the parallelism level for maintenance operations.
    /// A value of 0 runs operations sequentially. A value greater than 1 enables parallel execution.
    /// Note: Parallel execution may increase resource utilization and should be used cautiously in production.
    /// Default: 1 (sequential execution).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than 0.</exception>
    public int DegreeOfParallelism
    {
        get => _degreeOfParallelism;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Degree of parallelism cannot be negative.");
            }

            _degreeOfParallelism = value;
        }
    }
    private int _degreeOfParallelism = 1;
}
