// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a backup of a tenant database
/// </summary>
public class Backup
{
    public string BackupId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public BackupType BackupType { get; set; } = BackupType.Full;
    public BackupStatus Status { get; set; } = BackupStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public long SizeBytes { get; set; }
    public long OriginalSizeBytes { get; set; }
    public int CompressionRatio { get; set; }
    public string? CreatedBy { get; set; }
    public string? VerifiedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Tags { get; set; }

    // Navigation properties
    public TenantDatabase? Database { get; set; }

    /// <summary>
    /// Validates the backup entity
    /// </summary>
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
    public void MarkAsStarted(string createdBy)
    {
        Status = BackupStatus.InProgress;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Marks the backup as completed
    /// </summary>
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
    public void MarkAsFailed(string errorMessage)
    {
        Status = BackupStatus.Failed;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Marks the backup as verified
    /// </summary>
    public void MarkAsVerified(string verifiedBy)
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = verifiedBy;
        Status = BackupStatus.Verified;
    }

    /// <summary>
    /// Sets the expiration date for the backup
    /// </summary>
    public void SetExpiration(DateTime expirationDate)
    {
        ExpiresAt = expirationDate;
    }

    /// <summary>
    /// Checks if the backup has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;

    /// <summary>
    /// Adds tags to the backup
    /// </summary>
    public void AddTag(string tag)
    {
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
    /// Gets all tags as a list
    /// </summary>
    public List<string> GetTags()
    {
        return string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : Tags.Split(',').Select(t => t.Trim()).ToList();
    }
}
