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
    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the tenant.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contact email for the tenant.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    public override string ToString() => $"CreateTenantRequest {{ Name = {Name}, Description = {Description}, ContactEmail = {ContactEmail} }}";
}

/// <summary>
/// Request DTO for updating existing tenant metadata.
/// All fields optional to allow partial updates.
/// Immutable fields (TenantId, CreatedAt) are not exposed here.
/// </summary>
public sealed class UpdateTenantRequest {
    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the tenant.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contact email for the tenant.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for creating a database migration.
/// Validation ensures up/down scripts are non-empty and version is semantic.
/// Down script is optional for irreversible migrations.
/// </summary>
public sealed class CreateMigrationRequest {
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the migration version (semantic versioning).
    /// </summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the migration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the up migration script.
    /// </summary>
    public string UpScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the down migration script.
    /// </summary>
    public string DownScript { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for querying migrations with filters.
/// Supports filtering by status (pending, applied, failed) for UI dashboards.
/// </summary>
public sealed class QueryMigrationsRequest {
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the migration status to filter by.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the maximum number of records to return.
    /// </summary>
    public int Limit { get; set; } = 100;
    /// <summary>
    /// Gets or sets the number of records to skip.
    /// </summary>
    public int Offset { get; set; } = 0;
}

/// <summary>
/// Request DTO for backup restore operation.
/// Requires explicit confirmation to prevent accidental data loss.
/// Target database must be different from source to implement dry-run patterns.
/// </summary>
public sealed class RestoreBackupRequest {
    /// <summary>
    /// Gets or sets the backup identifier.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target database identifier.
    /// </summary>
    public string TargetDatabaseId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether to confirm the restore operation.
    /// </summary>
    public bool ConfirmRestore { get; set; } = false;
    /// <summary>
    /// Gets or sets the user who performed the restore.
    /// </summary>
    public string RestoredBy { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for paginated list queries.
/// Reusable across all list endpoints with consistent pagination semantics.
/// </summary>
public sealed class PaginationRequest {
    /// <summary>
    /// Gets or sets the page number (starting from 1).
    /// </summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 50;

    public int GetOffset() => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Request DTO for batch operations (e.g., apply migrations to multiple tenants).
/// Supports async processing with job ID returned for polling.
/// </summary>
public sealed class BatchOperationRequest {
    /// <summary>
    /// Gets or sets the list of resource identifiers to operate on.
    /// </summary>
    public List<string> ResourceIds { get; set; } = new();
    /// <summary>
    /// Gets or sets the operation to perform.
    /// </summary>
    public string Operation { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the operation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request DTO for webhook configuration.
/// Enables event-driven integrations (e.g., notify on backup completion).
/// </summary>
public sealed class WebhookSubscriptionRequest {
    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type of event to subscribe to.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the webhook is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the HTTP headers to include with the webhook request.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Request DTO for applying migrations to multiple databases with fault isolation.
/// </summary>
public sealed class ApplyMigrationsToMultipleRequest {
    /// <summary>
    /// Gets or sets the list of database identifiers to apply migrations to.
    /// </summary>
    public List<string> DatabaseIds { get; set; } = new();
    /// <summary>
    /// Gets or sets the user who applied the migrations.
    /// </summary>
    public string AppliedBy { get; set; } = string.Empty;
}