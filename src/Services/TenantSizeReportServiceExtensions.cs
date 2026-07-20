#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Extension methods for registering the TenantSizeReportService with dependency injection.
/// </summary>
public static class TenantSizeReportServiceExtensions
{
    /// <summary>
    /// Adds the TenantSizeReportService to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTenantSizeReportService(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddScoped<ITenantSizeReportService, TenantSizeReportService>();
        return services;
    }
}
