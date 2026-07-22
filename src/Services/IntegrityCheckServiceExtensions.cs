#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Extension methods for registering <see cref="IIntegrityCheckService"/> with dependency injection.
/// </summary>
public static class IntegrityCheckServiceExtensions
{
    /// <summary>
    /// Adds the integrity check service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddIntegrityCheckService(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IIntegrityCheckService, IntegrityCheckService>();

        return services;
    }
}
