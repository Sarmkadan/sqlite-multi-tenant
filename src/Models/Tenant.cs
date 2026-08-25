#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a tenant in the multi-tenant system
/// </summary>
public sealed class Tenant {
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? DatabasePath { get; set; }
    public bool IsDataIsolated { get; set; } = true;
    public int MaxConnections { get; set; } = 10;
    public Dictionary<string, string>? Metadata { get; set; }

    // Navigation properties
    public ICollection<TenantDatabase> Databases { get; set; } = new List<TenantDatabase>();
    public ICollection<TenantSettings> Settings { get; set; } = new List<TenantSettings>();

    /// <summary>
    /// Validates the tenant entity
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(TenantId) || TenantId.Length > TenantConstants.MaxTenantIdLength)
            errors.Add($"TenantId must be non-empty and less than {TenantConstants.MaxTenantIdLength} characters");

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > TenantConstants.MaxTenantNameLength)
            errors.Add($"Name must be non-empty and less than {TenantConstants.MaxTenantNameLength} characters");

        if (MaxConnections <= 0)
            errors.Add("MaxConnections must be greater than zero");

        if (CreatedAt > UpdatedAt)
            errors.Add("CreatedAt cannot be after UpdatedAt");

        return errors.Count == 0;
    }

    /// <summary>
    /// Marks the tenant as accessed
    /// </summary>
    public void MarkAsAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the tenant
    /// </summary>
    public void Deactivate()
    {
        Status = TenantStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the tenant
    /// </summary>
    public void Activate()
    {
        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets metadata for the tenant
    /// </summary>
    public void SetMetadata(string key, string value)
    {
        Metadata ??= new Dictionary<string, string>();
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets metadata value by key
    /// </summary>
    public string? GetMetadata(string key)
    {
        return Metadata?.TryGetValue(key, out var value) == true ? value : null;
    }

    public override string ToString() => $"Tenant {{ TenantId = {TenantId}, Name = {Name}, Description = {Description}, Status = {Status}, CreatedAt = {CreatedAt}, UpdatedAt = {UpdatedAt} }}";
}
