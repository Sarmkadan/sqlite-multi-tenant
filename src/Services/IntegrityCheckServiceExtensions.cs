#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Extension methods for registering IntegrityCheckService with dependency injection.
/// </summary>
public static class IntegrityCheckServiceExtensions
{
    /// <summary>
    /// Adds the integrity check service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddIntegrityCheckService(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddScoped<IIntegrityCheckService, IntegrityCheckService>();

        return services;
    }
}
