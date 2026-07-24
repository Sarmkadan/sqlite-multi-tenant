#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Api.Requests;

/// <summary>
/// Request DTO for creating a new tenant.
/// Minimalist design: only required fields, optional descriptions.
/// Validation happens in controller to provide meaningful error messages.
/// </summary>
public sealed class CreateTenantRequest {
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating existing tenant metadata.
/// All fields optional to allow partial updates.
/// Immutable fields (TenantId, CreatedAt) are not exposed here.
/// </summary>
public sealed class UpdateTenantRequest {
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for creating a database migration.
/// Validation ensures up/down scripts are non-empty and version is semantic.
/// Down script is optional for irreversible migrations.
/// </summary>
public sealed class CreateMigrationRequest {
    public string DatabaseId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UpScript { get; set; } = string.Empty;
    public string DownScript { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for querying migrations with filters.
/// Supports filtering by status (pending, applied, failed) for UI dashboards.
/// </summary>
public sealed class QueryMigrationsRequest {
    public string DatabaseId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}

/// <summary>
/// Request DTO for backup restore operation.
/// Requires explicit confirmation to prevent accidental data loss.
/// Target database must be different from source to implement dry-run patterns.
/// </summary>
public sealed class RestoreBackupRequest {
    public string BackupId { get; set; } = string.Empty;
    public string TargetDatabaseId { get; set; } = string.Empty;
    public bool ConfirmRestore { get; set; } = false;
    public string RestoredBy { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for paginated list queries.
/// Reusable across all list endpoints with consistent pagination semantics.
/// </summary>
public sealed class PaginationRequest {
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public int GetOffset() => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Request DTO for batch operations (e.g., apply migrations to multiple tenants).
/// Supports async processing with job ID returned for polling.
/// </summary>
public sealed class BatchOperationRequest {
    public List<string> ResourceIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request DTO for webhook configuration.
/// Enables event-driven integrations (e.g., notify on backup completion).
/// </summary>
public sealed class WebhookSubscriptionRequest {
    public string Url { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Request DTO for applying migrations to multiple databases with fault isolation.
/// </summary>
public sealed class ApplyMigrationsToMultipleRequest {
    public List<string> DatabaseIds { get; set; } = new();
    public string AppliedBy { get; set; } = string.Empty;
}
