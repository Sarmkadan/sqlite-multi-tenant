#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Event raised after a tenant has been renamed. The database has been copied and verified,
/// the registry points to the new identifier, and the original file has been removed.
/// </summary>
public sealed class TenantRenamedEvent : DomainEvent
{
    /// <summary>Gets or sets the identifier the tenant had before the rename.</summary>
    public string OldTenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier the tenant has after the rename.</summary>
    public string NewTenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the database file path after the rename.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <inheritdoc />
    public TenantRenamedEvent() : base(nameof(TenantRenamedEvent)) { }
}
