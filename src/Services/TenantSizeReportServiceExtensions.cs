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
    /// Adds the <see cref="TenantSizeReportService"/> and <see cref="ITenantSizeReportService"/> to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddTenantSizeReportService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITenantSizeReportService, TenantSizeReportService>();
        return services;
    }
}
