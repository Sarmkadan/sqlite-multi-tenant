#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Caching;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Formatters;
using SqliteMultiTenant.Health;
using SqliteMultiTenant.Integration;
using SqliteMultiTenant.Middleware;
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
public sealed class MultiTenantOptionsBuilder {
    private readonly SqliteMultiTenantOptions _options;

    public MultiTenantOptionsBuilder()
    {
        _options = new SqliteMultiTenantOptions();
    }

    public MultiTenantOptionsBuilder WithBackupRetention(int days)
    {
        _options.BackupRetentionDays = days;
        return this;
    }

    public MultiTenantOptionsBuilder WithMaxConnections(int count)
    {
        _options.MaxConnections = count;
        return this;
    }

    public MultiTenantOptionsBuilder WithConnectionTimeout(int seconds)
    {
        _options.ConnectionTimeoutSeconds = seconds;
        return this;
    }

    public MultiTenantOptionsBuilder WithEncryption(bool enabled)
    {
        _options.EnableEncryption = enabled;
        return this;
    }

    public MultiTenantOptionsBuilder WithBackupDirectory(string path)
    {
        _options.BackupDirectory = path;
        return this;
    }

    public MultiTenantOptionsBuilder WithDatabaseDirectory(string path)
    {
        _options.DatabaseDirectory = path;
        return this;
    }

    public MultiTenantOptionsBuilder WithLogging(bool enabled)
    {
        _options.EnableLogging = enabled;
        return this;
    }

    public SqliteMultiTenantOptions Build()
    {
        return _options;
    }
}
