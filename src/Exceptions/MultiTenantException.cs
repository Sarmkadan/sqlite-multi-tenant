#nullable enable
namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Base exception type for all multi‑tenant related errors.
/// Provides a <see cref="TenantId"/> property to identify the tenant that caused the error.
/// </summary>
public abstract class MultiTenantException : Exception
{
    /// <summary>
    /// Gets the identifier of the tenant related to the exception, if any.
    /// </summary>
    public string? TenantId { get; }

    protected MultiTenantException(string message) : base(message) { }

    protected MultiTenantException(string message, Exception innerException) : base(message, innerException) { }

    protected MultiTenantException(string message, string tenantId) : base(message)
    {
        TenantId = tenantId;
    }

    protected MultiTenantException(string message, Exception innerException, string tenantId) : base(message, innerException)
    {
        TenantId = tenantId;
    }
}
