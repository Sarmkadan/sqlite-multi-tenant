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
public sealed class Migration
{
    /// <summary>
    /// Gets or sets the unique identifier of the migration
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the database the migration is applied to
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the migration
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the migration
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the migration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the SQL script that applies the migration
    /// </summary>
    public string UpScript { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional SQL script that reverts the migration
    /// </summary>
    public string? DownScript { get; set; }

    /// <summary>
    /// Gets or sets the current status of the migration
    /// </summary>
    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;

    /// <summary>
    /// Gets or sets the UTC date and time when the migration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC date and time when the migration execution started
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the migration completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the migration was rolled back
    /// </summary>
    public DateTime? RolledBackAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the actor that executed the migration
    /// </summary>
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Gets or sets the error message recorded when the migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution time of the migration in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the position of the migration in the execution sequence
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the migration can be rolled back
    /// </summary>
    public bool IsRollbackable { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Gets or sets the database the migration belongs to
    /// </summary>
    public TenantDatabase? Database { get; set; }

    /// <summary>
    /// Validates the migration entity
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(MigrationId))
        {
            errors.Add("MigrationId is required");
        }

        if (string.IsNullOrWhiteSpace(DatabaseId))
        {
            errors.Add("DatabaseId is required");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            errors.Add("Version is required");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Name is required");
        }

        if (string.IsNullOrWhiteSpace(UpScript))
        {
            errors.Add("UpScript is required");
        }

        if (ExecutionTimeMs < 0)
        {
            errors.Add("ExecutionTimeMs cannot be negative");
        }

        if (ExecutionOrder < 0)
        {
            errors.Add("ExecutionOrder cannot be negative");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Marks the migration as started
    /// </summary>
    public void MarkAsStarted(string executedBy)
    {
        ArgumentException.ThrowIfNullOrEmpty(executedBy);
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
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
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

    /// <summary>
    /// Returns a concise, informative string representation of the migration.
    /// </summary>
    public override string ToString()
    {
        return $"Migration {{ MigrationId = {MigrationId}, DatabaseId = {DatabaseId}, Version = {Version}, Name = {Name}, Description = {Description}, UpScript = {UpScript} }}";
    }
}
