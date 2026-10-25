#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Caching;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Formatters;
using SqliteMultiTenant.Health;
using SqliteMultiTenant.Integration;
using SqliteMultiTenant.Middleware;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Validation;

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// Dependency injection setup for all application services.
/// Centralizes service registration following composition root pattern.
/// Enables easy testing by allowing dependency override.
/// </summary>
public static class DependencyInjectionSetup
{
    /// <summary>
    /// Registers all API controllers as services.
    /// Controllers typically have service dependencies injected.
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<TenantController>();
        services.AddScoped<BackupController>();
        services.AddScoped<MigrationController>();

        return services;
    }

    /// <summary>
    /// Registers middleware components.
    /// Middleware is typically stateless but may depend on logging/caching.
    /// </summary>
    public static IServiceCollection AddMiddlewareServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ErrorHandlingMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddSingleton(new RateLimitingOptions { RequestsPerMinute = 300 });
        services.AddScoped<RateLimitingMiddleware>();

        return services;
    }

    /// <summary>
    /// Registers caching services.
    /// Cache service is singleton to maintain in-memory state across requests.
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<CacheInvalidationService>();

        return services;
    }

    /// <summary>
    /// Registers event system services.
    /// Event publisher is singleton; handlers are registered per-type.
    /// </summary>
    public static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEventPublisher, EventPublisher>();

        // Register event handlers - these can be expanded with specific handlers
        services.AddScoped(typeof(IEventHandler<>), typeof(LoggingEventHandler<>));

        return services;
    }

    /// <summary>
    /// Registers formatter services for output serialization.
    /// Factory pattern allows runtime selection of formatters.
    /// </summary>
    public static IServiceCollection AddFormatterServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<FormatterFactory>();
        services.AddScoped<IOutputFormatter, JsonFormatter>();

        return services;
    }

    /// <summary>
    /// Registers validation services.
    /// Validators are stateless and can be singletons.
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TenantValidator>();
        services.AddSingleton<MigrationValidator>();
        services.AddSingleton<ConnectionStringValidator>();
        services.AddSingleton<BackupValidator>();

        return services;
    }

    /// <summary>
    /// Registers health check services.
    /// Responsible for system diagnostics and monitoring.
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, string databasePath = ".")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        services.AddScoped<IHealthCheckService>(sp => new HealthCheckService(
            sp.GetRequiredService<ILogger<HealthCheckService>>(),
            databasePath));

        return services;
    }

    /// <summary>
    /// Registers background worker services.
    /// These run indefinitely and should be singleton to maintain state.
    /// </summary>
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register backup scheduler
        services.AddSingleton<BackupSchedulerService>();
        services.AddHostedService<BackupSchedulerService>(sp => sp.GetRequiredService<BackupSchedulerService>());

        // Register backup cleanup
        services.AddSingleton<BackupCleanupService>();
        services.AddHostedService<BackupCleanupService>(sp => sp.GetRequiredService<BackupCleanupService>());

        // Register database maintenance worker and service
        services.AddTenantDatabaseMaintenanceService();
        services.AddSingleton<DatabaseMaintenanceWorker>();
        services.AddHostedService<DatabaseMaintenanceWorker>(sp => sp.GetRequiredService<DatabaseMaintenanceWorker>());

        // Register tenant size report service
        services.AddTenantSizeReportService();

        // Register integrity check service
        services.AddIntegrityCheckService();

        return services;
    }

    /// <summary>
    /// Registers integration services (HTTP clients, webhooks).
    /// </summary>
    public static IServiceCollection AddIntegrationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<IWebhookHandler, WebhookHandler>();

        return services;
    }

    /// <summary>
    /// Registers all Phase 2 services in one call.
    /// Simplifies Program.cs and ensures consistent initialization.
    /// </summary>
    public static IServiceCollection AddPhase2Services(
        this IServiceCollection services,
        string databasePath = ".")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        services.AddApiControllers();
        services.AddMiddlewareServices();
        services.AddCachingServices();
        services.AddEventServices();
        services.AddFormatterServices();
        services.AddValidationServices();
        services.AddHealthCheckServices(databasePath);
        services.AddBackgroundWorkers();
        services.AddIntegrationServices();

        return services;
    }
}

/// <summary>
/// Builder pattern for fluent configuration of multi-tenant options.
/// Enables progressive configuration without modifying ServiceConfiguration directly.
/// </summary>
public sealed class MultiTenantOptionsBuilder
{
    private readonly SqliteMultiTenantOptions _options;

    /// <summary>
    /// Initializes a new builder with default <see cref="SqliteMultiTenantOptions"/> values.
    /// </summary>
    public MultiTenantOptionsBuilder()
    {
        _options = new SqliteMultiTenantOptions();
    }

    /// <summary>
    /// Sets the number of days backups are retained.
    /// </summary>
    /// <param name="days">Retention period in days.</param>
    /// <returns>This builder, for chaining.</returns>
    public MultiTenantOptionsBuilder WithBackupRetention(int days)
    {
        _options.BackupRetentionDays = days;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of pooled database connections.
    /// </summary>
    /// <param name="count">Maximum connection count.</param>
    /// <returns>This builder, for chaining.</returns>
    public MultiTenantOptionsBuilder WithMaxConnections(int count)
    {
        _options.MaxConnections = count;
        return this;
    }

    /// <summary>
    /// Sets the database connection timeout.
    /// </summary>
    /// <param name="seconds">Timeout in seconds.</param>
    /// <returns>This builder, for chaining.</returns>
    public MultiTenantOptionsBuilder WithConnectionTimeout(int seconds)
    {
        _options.ConnectionTimeoutSeconds = seconds;
        return this;
    }

    /// <summary>
    /// Enables or disables database encryption.
    /// </summary>
    /// <param name="enabled">True to enable encryption; otherwise false.</param>
    /// <returns>This builder, for chaining.</returns>
    public MultiTenantOptionsBuilder WithEncryption(bool enabled)
    {
        _options.EnableEncryption = enabled;
        return this;
    }

    /// <summary>
    /// Sets the directory where backups are stored.
    /// </summary>
    /// <param name="path">Backup directory path. Must not be null or empty.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    public MultiTenantOptionsBuilder WithBackupDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _options.BackupDirectory = path;
        return this;
    }

    /// <summary>
    /// Sets the directory where tenant databases are stored.
    /// </summary>
    /// <param name="path">Database directory path. Must not be null or empty.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    public MultiTenantOptionsBuilder WithDatabaseDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _options.DatabaseDirectory = path;
        return this;
    }

    /// <summary>
    /// Enables or disables logging.
    /// </summary>
    /// <param name="enabled">True to enable logging; otherwise false.</param>
    /// <returns>This builder, for chaining.</returns>
    public MultiTenantOptionsBuilder WithLogging(bool enabled)
    {
        _options.EnableLogging = enabled;
        return this;
    }

    /// <summary>
    /// Builds the configured options.
    /// </summary>
    /// <returns>The <see cref="SqliteMultiTenantOptions"/> instance held by this builder.</returns>
    public SqliteMultiTenantOptions Build()
    {
        return _options;
    }
}
