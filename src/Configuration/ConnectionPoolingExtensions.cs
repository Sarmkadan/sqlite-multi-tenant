#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering connection pooling services.
/// </summary>
public static class ConnectionPoolingExtensions
{
    /// <summary>
    /// Registers <see cref="IConnectionPoolManager"/> as a singleton in the DI container.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection)"/>
    /// so that calling this method more than once (e.g. from a test host and the real host) does
    /// not register duplicate services or reset options that were already configured.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configure">
    /// Optional delegate to override default <see cref="ConnectionPoolOptions"/> values such as
    /// <see cref="ConnectionPoolOptions.MaxPoolSize"/> and <see cref="ConnectionPoolOptions.IdleTimeout"/>.
    /// </param>
    /// <returns><paramref name="services"/> to support fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddConnectionPooling(opts =>
    /// {
    ///     opts.MaxPoolSize          = 20;
    ///     opts.MinPoolSize          = 2;
    ///     opts.IdleTimeout          = TimeSpan.FromMinutes(10);
    ///     opts.AcquireTimeout       = TimeSpan.FromSeconds(15);
    ///     opts.MaxConnectionLifetime = TimeSpan.FromHours(2);
    ///     opts.PruneInterval        = TimeSpan.FromMinutes(2);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddConnectionPooling(
        this IServiceCollection services,
        Action<ConnectionPoolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ConnectionPoolOptions();
        configure?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(options);

        services.TryAddSingleton<IConnectionPoolManager>(sp =>
            new ConnectionPoolManager(
                sp.GetRequiredService<ConnectionPoolOptions>(),
                sp.GetRequiredService<ILogger<ConnectionPoolManager>>()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="IConnectionPoolManager"/> using settings already bound from
    /// <see cref="ConnectionPoolOptions"/> in the DI container (e.g. via
    /// <c>services.Configure&lt;ConnectionPoolOptions&gt;</c>).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns><paramref name="services"/> to support fluent chaining.</returns>
    public static IServiceCollection AddConnectionPooling(this IServiceCollection services)
        => services.AddConnectionPooling(configure: null);
}
