#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a backup of a tenant database
/// </summary>
public sealed class Backup {
    public override string ToString() => $"Backup {{ BackupId = {BackupId}, DatabaseId = {DatabaseId}, BackupPath = {BackupPath}, BackupType = {BackupType}, Status = {Status}, CreatedAt = {CreatedAt} }}";
    /// <summary>
    /// Gets or sets the unique identifier for the backup
    /// </summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the identifier of the database associated with the backup
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file system path where the backup is stored
    /// </summary>
    public string BackupPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type of backup (Full, Differential, etc.)
    /// </summary>
    public BackupType BackupType { get; set; } = BackupType.Full;
    /// <summary>
    /// Gets or sets the current status of the backup operation
    /// </summary>
    public BackupStatus Status { get; set; } = BackupStatus.Pending;
    /// <summary>
    /// Gets or sets the date and time when the backup was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the date and time when the backup was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the backup was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
    /// <summary>
    /// Gets or sets the size of the backup in bytes
    /// </summary>
    public long SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the original size of the database before backup in bytes
    /// </summary>
    public long OriginalSizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the compression ratio as a percentage (0-100)
    /// </summary>
    public int CompressionRatio { get; set; }
    /// <summary>
    /// Gets or sets the user who created the backup
    /// </summary>
    public string? CreatedBy { get; set; }
    /// <summary>
    /// Gets or sets the user who verified the backup
    /// </summary>
    public string? VerifiedBy { get; set; }
    /// <summary>
    /// Gets or sets any error message associated with the backup
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets the duration of the backup operation in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the backup is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the backup has been verified
    /// </summary>
    public bool IsVerified { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the backup expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Gets or sets comma-separated tags associated with the backup
    /// </summary>
    public string? Tags { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the tenant database associated with this backup
    /// </summary>
    public TenantDatabase? Database { get; set; }

    /// <summary>
    /// Validates the backup entity
    /// </summary>
    /// <param name="errors">List of validation errors if any</param>
    /// <returns>True if the backup entity is valid, false otherwise</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BackupId))
            errors.Add("BackupId is required");

        if (string.IsNullOrWhiteSpace(DatabaseId))
            errors.Add("DatabaseId is required");

        if (string.IsNullOrWhiteSpace(BackupPath))
            errors.Add("BackupPath is required");

        if (SizeBytes < 0)
            errors.Add("SizeBytes cannot be negative");

        if (OriginalSizeBytes < 0)
            errors.Add("OriginalSizeBytes cannot be negative");

        if (CompressionRatio < 0 || CompressionRatio > 100)
            errors.Add("CompressionRatio must be between 0 and 100");

        if (DurationMs < 0)
            errors.Add("DurationMs cannot be negative");

        return errors.Count == 0;
    }

    /// <summary>
    /// Marks the backup as started
    /// </summary>
    /// <param name="createdBy">The user who initiated the backup</param>
    public void MarkAsStarted(string createdBy)
    {
        ArgumentException.ThrowIfNullOrEmpty(createdBy);
        Status = BackupStatus.InProgress;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Marks the backup as completed
    /// </summary>
    /// <param name="sizeBytes">The size of the backup in bytes</param>
    /// <param name="durationMs">The duration of the backup operation in milliseconds</param>
    public void MarkAsCompleted(long sizeBytes, long durationMs)
    {
        Status = BackupStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        SizeBytes = sizeBytes;
        DurationMs = durationMs;
        ErrorMessage = null;

        if (OriginalSizeBytes > 0)
        {
            CompressionRatio = (int)((1 - (double)sizeBytes / OriginalSizeBytes) * 100);
        }
    }

    /// <summary>
    /// Marks the backup as failed
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure</param>
    public void MarkAsFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        Status = BackupStatus.Failed;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Marks the backup as verified
    /// </summary>
    /// <param name="verifiedBy">The user who verified the backup</param>
    public void MarkAsVerified(string verifiedBy)
    {
        ArgumentException.ThrowIfNullOrEmpty(verifiedBy);
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = verifiedBy;
        Status = BackupStatus.Verified;
    }

    /// <summary>
    /// Sets the expiration date for the backup
    /// </summary>
    /// <param name="expirationDate">The date and time when the backup expires</param>
    public void SetExpiration(DateTime expirationDate)
    {
        ExpiresAt = expirationDate;
    }

    /// <summary>
    /// Checks if the backup has expired
    /// </summary>
    /// <returns>True if the backup has expired, false otherwise</returns>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;

    /// <summary>
    /// Adds a tag to the backup
    /// </summary>
    /// <param name="tag">The tag to add</param>
    public void AddTag(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);
        if (string.IsNullOrEmpty(Tags))
        {
            Tags = tag;
        }
        else
        {
            Tags += $",{tag}";
        }
    }

    /// <summary>
    /// Gets all tags associated with the backup as a list
    /// </summary>
    /// <returns>A list of tags</returns>
    public List<string> GetTags()
    {
        return string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : Tags.Split(',').Select(t => t.Trim()).ToList();
    }
}