#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the current tenant context for a request or operation
/// </summary>
public sealed class TenantContext
{
    /// <summary>
    /// Gets or sets the identifier of the current tenant
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the current tenant
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user performing the request or operation
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user performing the request or operation
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Gets or sets the UTC time when the tenant context was established
    /// </summary>
    public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC time when the context instance was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the correlation identifier of the current request
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the database connection associated with this context
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the file path of the tenant's database
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Gets or sets additional key-value data attached to this context
    /// </summary>
    public Dictionary<string, object>? ContextData { get; set; }

    /// <summary>
    /// Gets or sets the set of tenant identifiers this context is allowed to access
    /// </summary>
    public HashSet<string> AllowedTenants { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the context is still valid
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Validates the context
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(TenantId))
        {
            errorMessage = "TenantId is required";
            return false;
        }

        if (!IsValid)
        {
            errorMessage = "Context is marked as invalid";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Gets context data by key
    /// </summary>
    public object? GetContextData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return ContextData?.TryGetValue(key, out var value) == true ? value : null;
    }

    /// <summary>
    /// Sets context data
    /// </summary>
    public void SetContextData(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        ContextData ??= new Dictionary<string, object>();
        ContextData[key] = value;
    }

    /// <summary>
    /// Invalidates the context
    /// </summary>
    public void Invalidate()
    {
        IsValid = false;
    }

    /// <summary>
    /// Gets a summary of the context
    /// </summary>
    public override string ToString()
    {
        return $"TenantContext(TenantId={TenantId}, User={UserEmail}, Established={EstablishedAt:O})";
    }
}
