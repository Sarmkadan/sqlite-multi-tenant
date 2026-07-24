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
public string TenantId { get; set; } = string.Empty;
public string? TenantName { get; set; }
public string? UserId { get; set; }
public string? UserEmail { get; set; }
public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public string? RequestId { get; set; }
public string? ConnectionId { get; set; }
public string? DatabasePath { get; set; }
public Dictionary<string, object>? ContextData { get; set; }
public HashSet<string> AllowedTenants { get; set; } = new();
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
return ContextData?.TryGetValue(key, out var value) == true ? value : null;
}

/// <summary>
/// Sets context data
/// </summary>
public void SetContextData(string key, object value)
{
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