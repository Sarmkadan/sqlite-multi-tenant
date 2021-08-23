#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Caching;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration;
using SqliteMultiTenant.Logging;
using SqliteMultiTenant.Middleware;
using SqliteMultiTenant.Monitoring;
using SqliteMultiTenant.Operations;
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Validation;

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// Service collection extension methods for dependency injection registration.
/// Provides fluent API for configuring all SQLite Multi-Tenant services.
/// Allows selective service registration for custom configurations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all SQLite Multi-Tenant services.
    /// </summary>
    public static IServiceCollection AddSqliteMultiTenantServices(
        this IServiceCollection services,
        Action<ServiceOptions>? configureOptions = null)
    {
        var options = new ServiceOptions();
        configureOptions?.Invoke(options);

        // Core services
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IDataMapper>(sp => new DataMapper(sp.GetRequiredService<ILogger<DataMapper>>()));

        // Caching
        services.AddSingleton<IDistributedCache>(sp =>
            new DistributedCacheService(
                sp.GetRequiredService<ILogger<DistributedCacheService>>(),
                options.MaxCacheItems));

        // Event bus
        services.AddSingleton<IEventBus, EventBus>();

        // Integration services
        services.AddSingleton<WebhookService>();
        services.AddHttpClient<HttpClientWrapper>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);
            });

        // Monitoring & Logging
        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IRequestResponseLogger, RequestResponseLogger>();

        // Validation
        services.AddScoped(sp => new DataValidator(sp.GetRequiredService<ILogger<DataValidator>>()));

        // Background workers
        services.AddSingleton<IScheduledTaskService>(sp =>
            new ScheduledTaskService(sp.GetRequiredService<ILogger<ScheduledTaskService>>()));

        // Operations
        services.AddScoped<IBatchProcessor, BatchProcessor>();

        // CLI
        services.AddScoped<Cli.CommandParser>();
        services.AddScoped<Cli.CommandExecutor>();
        services.AddScoped<Cli.CliApplication>();
        services.AddScoped<Cli.IConsoleWriter, Cli.ConsoleWriter>();

        return services;
    }

    /// <summary>
    /// Registers exception handling services.
    /// </summary>
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddSingleton<Exceptions.IExceptionProcessor, Exceptions.ExceptionProcessor>();
        return services;
    }

    /// <summary>
    /// Registers event handlers.
    /// </summary>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        services.AddScoped<Events.IDomainEventHandler<Events.TenantCreatedNotificationEvent>, Events.TenantCreatedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.TenantDeletedEvent>, Events.TenantDeletedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.BackupCompletedNotificationEvent>, Events.BackupCompletedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.MigrationCompletedEvent>, Events.MigrationCompletedEventHandler>();
        return services;
    }

    /// <summary>
    /// Registers health check services.
    /// </summary>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services)
    {
        services.AddSingleton<Health.HealthCheckService>();
        return services;
    }

    /// <summary>
    /// Registers formatters.
    /// </summary>
    public static IServiceCollection AddFormatters(this IServiceCollection services)
    {
        services.AddSingleton<Formatters.OutputFormatter>();
        services.AddScoped(sp => new Formatters.JsonExportFormatter(sp.GetRequiredService<ILogger<Formatters.JsonExportFormatter>>()));
        services.AddScoped(sp => new Formatters.CsvExportFormatter(sp.GetRequiredService<ILogger<Formatters.CsvExportFormatter>>()));
        services.AddScoped(sp => new Formatters.XmlExportFormatter(sp.GetRequiredService<ILogger<Formatters.XmlExportFormatter>>()));
        return services;
    }

    /// <summary>
    /// Adds request/response logging middleware.
    /// </summary>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
        app.UsePerformanceTracking();
        return app;
    }
}

/// <summary>
/// Configuration options for service registration.
/// </summary>
public sealed class ServiceOptions {
    public int MaxCacheItems { get; set; } = 1000;
    public int HttpClientTimeoutSeconds { get; set; } = 30;
    public bool EnableAuiting { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableEventBus { get; set; } = true;
}
