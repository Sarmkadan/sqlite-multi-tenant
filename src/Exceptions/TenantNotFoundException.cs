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
    public TenantNotFoundException(string tenantId)
        : base($"Tenant with ID '{tenantId}' was not found.", tenantId)
    {
    }

    public TenantNotFoundException(string tenantId, Exception innerException)
        : base($"Tenant with ID '{tenantId}' was not found.", innerException, tenantId)
    {
    }

    public TenantNotFoundException(string message, string tenantId, Exception? innerException = null)
        : base(message, innerException, tenantId)
    {
    }
}
