#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a database migration for a tenant
/// </summary>
public sealed class Migration {
    public string MigrationId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UpScript { get; set; } = string.Empty;
    public string? DownScript { get; set; }
    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
    public string? ExecutedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsRollbackable { get; set; } = true;

    // Navigation properties
    public TenantDatabase? Database { get; set; }

    /// <summary>
    /// Validates the migration entity
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(MigrationId))
            errors.Add("MigrationId is required");

        if (string.IsNullOrWhiteSpace(DatabaseId))
            errors.Add("DatabaseId is required");

        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("Version is required");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(UpScript))
            errors.Add("UpScript is required");

        if (ExecutionTimeMs < 0)
            errors.Add("ExecutionTimeMs cannot be negative");

        if (ExecutionOrder < 0)
            errors.Add("ExecutionOrder cannot be negative");

        return errors.Count == 0;
    }

    /// <summary>
    /// Marks the migration as started
    /// </summary>
    public void MarkAsStarted(string executedBy)
    {
        Status = MigrationStatus.Running;
        ExecutedAt = DateTime.UtcNow;
        ExecutedBy = executedBy;
    }

    /// <summary>
    /// Marks the migration as completed
    /// </summary>
    public void MarkAsCompleted(long executionTimeMs)
    {
        Status = MigrationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ExecutionTimeMs = executionTimeMs;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks the migration as failed
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = MigrationStatus.Failed;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Marks the migration as rolled back
    /// </summary>
    public void MarkAsRolledBack(long executionTimeMs)
    {
        Status = MigrationStatus.RolledBack;
        RolledBackAt = DateTime.UtcNow;
        ExecutionTimeMs = executionTimeMs;
        ErrorMessage = null;
    }

    /// <summary>
    /// Checks if the migration can be rolled back
    /// </summary>
    public bool CanRollback()
    {
        return IsRollbackable &&
               !string.IsNullOrEmpty(DownScript) &&
               Status == MigrationStatus.Completed &&
               RolledBackAt is null;
    }

    /// <summary>
    /// Gets the display name for the migration
    /// </summary>
    public string GetDisplayName()
    {
        return $"{Version}_{Name}";
    }
}
