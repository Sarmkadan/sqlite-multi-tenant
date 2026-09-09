#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when a tenant is not found in the system
/// </summary>
public sealed class TenantNotFoundException : MultiTenantException
{
    private static string EnsureNotNull(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        return tenantId;
    }

    /// <summary>
    /// Thrown when a tenant is not found in the system
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    public TenantNotFoundException(string tenantId)
        : base($"Tenant with ID '{EnsureNotNull(tenantId)}' was not found.", EnsureNotNull(tenantId))
    {
    }

    /// <summary>
    /// Thrown when a tenant is not found in the system
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    public TenantNotFoundException(string tenantId, Exception innerException)
        : base($"Tenant with ID '{EnsureNotNull(tenantId)}' was not found.", innerException, EnsureNotNull(tenantId))
    {
    }

    /// <summary>
    /// Thrown when a tenant is not found in the system
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    public TenantNotFoundException(string message, string tenantId, Exception? innerException = null)
        : base(message, innerException, EnsureNotNull(tenantId))
    {
    }
}
