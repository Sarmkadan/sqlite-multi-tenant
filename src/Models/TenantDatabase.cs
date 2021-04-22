// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a database associated with a tenant
/// </summary>
public class TenantDatabase
{
    public string DatabaseId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastBackupAt { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public bool IsReadOnly { get; set; }
    public int ActiveConnectionCount { get; set; }
    public string? EncryptionKey { get; set; }
    public bool RequiresEncryption { get; set; }

    // Navigation properties
    public Tenant? Tenant { get; set; }
    public ICollection<Migration> Migrations { get; set; } = new List<Migration>();
    public ICollection<Backup> Backups { get; set; } = new List<Backup>();

    /// <summary>
    /// Validates the database entity
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DatabaseId))
            errors.Add("DatabaseId is required");

        if (string.IsNullOrWhiteSpace(TenantId))
            errors.Add("TenantId is required");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(FilePath))
            errors.Add("FilePath is required");

        if (FilePath.Length > 260)
            errors.Add("FilePath exceeds maximum path length");

        if (SizeBytes < 0)
            errors.Add("SizeBytes cannot be negative");

        if (SchemaVersion <= 0)
            errors.Add("SchemaVersion must be greater than zero");

        if (ActiveConnectionCount < 0)
            errors.Add("ActiveConnectionCount cannot be negative");

        return errors.Count == 0;
    }

    /// <summary>
    /// Updates the last backup timestamp
    /// </summary>
    public void UpdateLastBackupTime()
    {
        LastBackupAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the database size
    /// </summary>
    public void UpdateSize(long newSizeBytes)
    {
        if (newSizeBytes >= 0)
        {
            SizeBytes = newSizeBytes;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Increments the active connection count
    /// </summary>
    public void IncrementConnectionCount()
    {
        if (ActiveConnectionCount < 100)
        {
            ActiveConnectionCount++;
        }
    }

    /// <summary>
    /// Decrements the active connection count
    /// </summary>
    public void DecrementConnectionCount()
    {
        if (ActiveConnectionCount > 0)
        {
            ActiveConnectionCount--;
        }
    }

    /// <summary>
    /// Checks if the database has encryption enabled
    /// </summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(EncryptionKey);
}
