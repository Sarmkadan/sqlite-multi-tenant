#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.DataOperations;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Operations;

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Extension methods that register the async bulk import/export subsystem
/// with the ASP.NET Core / Generic Host dependency-injection container.
/// </summary>
/// <remarks>
/// Typical usage in <c>Program.cs</c> or a startup class:
/// <code>
/// services.AddBulkDataServices(opts =>
/// {
///     opts.DefaultBatchSize       = 2_000;
///     opts.MaxConcurrentTables    = 5;
///     opts.PublishDomainEvents    = true;
///     opts.DefaultExportDirectory = "/var/exports";
///     opts.BaseDatabasePath       = "/var/databases";
/// });
/// </code>
/// </remarks>
public static class BulkDataExtensions
{
    /// <summary>
    /// Registers <see cref="IBulkDataService"/> and all its dependencies in the DI container.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configureOptions">
    /// Optional delegate for overriding default <see cref="BulkDataOptions"/> values.
    /// When <c>null</c>, all defaults apply.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddBulkDataServices(
        this IServiceCollection services,
        Action<BulkDataOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BulkDataOptions();
        configureOptions?.Invoke(options);

        // Register the resolved options snapshot as a singleton so all scoped
        // BulkDataService instances share the same configuration object.
        services.AddSingleton(options);

        // Low-level DataExporter / DataImporter are stateless and logger-only;
        // register as transient so each operation gets its own instance.
        services.AddTransient(sp =>
            new DataExporter(sp.GetRequiredService<ILogger<DataExporter>>()));

        services.AddTransient(sp =>
            new DataImporter(sp.GetRequiredService<ILogger<DataImporter>>()));

        // IBatchProcessor is already registered by AddSqliteMultiTenantServices but
        // we guard with TryAddScoped to avoid duplicate registration when callers
        // compose both extension methods.
        services.TryAddScoped<IBatchProcessor, BatchProcessor>();

        // IEventBus is registered as a singleton by AddSqliteMultiTenantServices.
        // Register a no-op fallback so BulkDataServices can be used standalone.
        services.TryAddSingleton<IEventBus>(sp =>
            new EventBus(sp.GetRequiredService<ILogger<EventBus>>()));

        // Register the main service as scoped — one instance per HTTP request
        // (or per explicit scope in background workers).
        services.AddScoped<IBulkDataService>(sp => new BulkDataService(
            sp.GetRequiredService<DataExporter>(),
            sp.GetRequiredService<DataImporter>(),
            sp.GetRequiredService<IBatchProcessor>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<ILogger<BulkDataService>>(),
            sp.GetRequiredService<BulkDataOptions>()));

        return services;
    }
}

/// <summary>
/// Provides <see cref="IServiceCollection"/> guard-registration helpers.
/// Extracted here to avoid a dependency on Microsoft.Extensions.DependencyInjection.Abstractions
/// beyond what is already present in the host project.
/// </summary>
file static class ServiceCollectionGuardExtensions
{
    internal static void TryAddScoped<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services.All(d => d.ServiceType != typeof(TService)))
            services.AddScoped<TService, TImplementation>();
    }

    internal static void TryAddSingleton<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> factory)
        where TService : class
    {
        if (services.All(d => d.ServiceType != typeof(TService)))
            services.AddSingleton(factory);
    }
}
