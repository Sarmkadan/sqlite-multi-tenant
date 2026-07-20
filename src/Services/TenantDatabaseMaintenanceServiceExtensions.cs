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
/// </summary>
public sealed class TenantDatabaseMaintenanceOptions
{
    /// <summary>
    /// Enable VACUUM operation to reclaim disk space.
    /// Default: true (recommended for production).
    /// </summary>
    public bool EnableVacuum { get; set; } = true;

    /// <summary>
    /// Enable ANALYZE operation to update query statistics.
    /// Default: true (improves query performance).
    /// </summary>
    public bool EnableAnalyze { get; set; } = true;

    /// <summary>
    /// Enable PRAGMA optimize operation to update database statistics.
    /// Default: true (recommended for production).
    /// </summary>
    public bool EnableOptimize { get; set; } = true;

    /// <summary>
    /// Maintenance interval in hours (default: 24 = daily).
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum time allowed for maintenance on a single database (in seconds).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Parallelism level for maintenance operations (0 = sequential).
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;
}
