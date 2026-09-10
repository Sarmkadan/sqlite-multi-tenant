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
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tenant description.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the tenant status.
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    /// <summary>
    /// Gets or sets the date and time the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the date and time the tenant was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the date and time the tenant was last accessed.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }
    /// <summary>
    /// Gets or sets the contact email for the tenant.
    /// </summary>
    public string? ContactEmail { get; set; }
    /// <summary>
    /// Gets or sets the database path for the tenant.
    /// </summary>
    public string? DatabasePath { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the tenant's data is isolated.
    /// </summary>
    public bool IsDataIsolated { get; set; } = true;
    /// <summary>
    /// Gets or sets the maximum number of connections allowed for the tenant.
    /// </summary>
    public int MaxConnections { get; set; } = 10;
    /// <summary>
    /// Gets or sets the metadata key-value pairs for the tenant.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of tenant databases.
    /// </summary>
    public ICollection<TenantDatabase> Databases { get; set; } = new List<TenantDatabase>();
    /// <summary>
    /// Gets or sets the collection of tenant settings.
    /// </summary>
    public ICollection<TenantSettings> Settings { get; set; } = new List<TenantSettings>();

    /// <summary>
    /// Validates the tenant entity.
    /// </summary>
    /// <param name="errors">List of validation errors if any.</param>
    /// <returns>True if the tenant is valid; otherwise, false.</returns>
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
    /// Marks the tenant as accessed.
    /// </summary>
    public void MarkAsAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the tenant.
    /// </summary>
    public void Deactivate()
    {
        Status = TenantStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the tenant.
    /// </summary>
    public void Activate()
    {
        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets metadata for the tenant.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public void SetMetadata(string key, string value)
    {
        Metadata ??= new Dictionary<string, string>();
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets metadata value by key.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value if found; otherwise, null.</returns>
    public string? GetMetadata(string key)
    {
        return Metadata?.TryGetValue(key, out var value) == true ? value : null;
    }

    public override string ToString() => $"Tenant {{ TenantId = {TenantId}, Name = {Name}, Description = {Description}, Status = {Status}, CreatedAt = {CreatedAt}, UpdatedAt = {UpdatedAt} }}";
}