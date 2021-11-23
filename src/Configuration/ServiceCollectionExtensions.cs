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

using System.Diagnostics.CodeAnalysis;

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
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">Optional configuration action for service options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddSqliteMultiTenantServices(
        this IServiceCollection services,
        Action<ServiceOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ServiceOptions();
        configureOptions?.Invoke(options);

        // Core services
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IDataMapper>(
            sp => new DataMapper(sp.GetRequiredService<ILogger<DataMapper>>()));

        // Caching
        services.AddSingleton<IDistributedCache>(
            sp =>
            new DistributedCacheService(
                sp.GetRequiredService<ILogger<DistributedCacheService>>(),
                options.MaxCacheItems));

        // Event bus (conditionally registered based on options)
        if (options.EnableEventBus)
        {
            services.AddSingleton<IEventBus, EventBus>();
        }

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
        services.AddSingleton<IScheduledTaskService>(
            sp => new ScheduledTaskService(sp.GetRequiredService<ILogger<ScheduledTaskService>>()));

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
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<Exceptions.IExceptionProcessor, Exceptions.ExceptionProcessor>();
        return services;
    }

    /// <summary>
    /// Registers event handlers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<Events.IDomainEventHandler<Events.TenantCreatedNotificationEvent>, Events.TenantCreatedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.TenantDeletedEvent>, Events.TenantDeletedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.BackupCompletedNotificationEvent>, Events.BackupCompletedEventHandler>();
        services.AddScoped<Events.IDomainEventHandler<Events.MigrationCompletedEvent>, Events.MigrationCompletedEventHandler>();
        return services;
    }

    /// <summary>
    /// Registers health check services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<Health.HealthCheckService>();
        return services;
    }

    /// <summary>
    /// Registers formatters.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddFormatters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<Formatters.OutputFormatter>();
        services.AddScoped(sp => new Formatters.JsonExportFormatter(sp.GetRequiredService<ILogger<Formatters.JsonExportFormatter>>()));
        services.AddScoped(sp => new Formatters.CsvExportFormatter(sp.GetRequiredService<ILogger<Formatters.CsvExportFormatter>>()));
        services.AddScoped(sp => new Formatters.XmlExportFormatter(sp.GetRequiredService<ILogger<Formatters.XmlExportFormatter>>()));
        return services;
    }

    /// <summary>
    /// Adds request/response logging middleware.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
        app.UsePerformanceTracking();
        return app;
    }
}

/// <summary>
/// Configuration options for service registration.
/// </summary>
public sealed class ServiceOptions
{
    private int _maxCacheItems = 1000;
    private int _httpClientTimeoutSeconds = 30;

    /// <summary>
    /// Gets or sets the maximum number of items to cache.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is less than 0.</exception>
    public int MaxCacheItems
    {
        get => _maxCacheItems;
        set => _maxCacheItems = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "MaxCacheItems must be non-negative");
    }

    /// <summary>
    /// Gets or sets the HTTP client timeout in seconds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is less than 1.</exception>
    public int HttpClientTimeoutSeconds
    {
        get => _httpClientTimeoutSeconds;
        set => _httpClientTimeoutSeconds = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "HttpClientTimeoutSeconds must be at least 1");
    }

    /// <summary>
    /// Gets or sets whether auditing is enabled.
    /// </summary>
    public bool EnableAuiting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the event bus is enabled.
    /// </summary>
    public bool EnableEventBus { get; set; } = true;
}