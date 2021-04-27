#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when a tenant is not found in the system
/// </summary>
public sealed class TenantNotFoundException : Exception {
    public string? TenantId { get; }

    public TenantNotFoundException(string tenantId)
        : base($"Tenant with ID '{tenantId}' was not found.")
    {
        TenantId = tenantId;
    }

    public TenantNotFoundException(string tenantId, Exception innerException)
        : base($"Tenant with ID '{tenantId}' was not found.", innerException)
    {
        TenantId = tenantId;
    }

    public TenantNotFoundException(string message, string tenantId, Exception? innerException = null)
        : base(message, innerException)
    {
        TenantId = tenantId;
    }
}
