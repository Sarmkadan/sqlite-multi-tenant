#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a database associated with a tenant
/// </summary>
public sealed class TenantDatabase {
    /// <summary>
    /// The unique identifier for the database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>
    /// The identifier of the tenant that owns this database.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// The name of the database.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The file system path to the database file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// The size of the database in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    /// <summary>
    /// The date and time when the database was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// The date and time when the database was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// The date and time of the last backup performed on the database.
    /// </summary>
    public DateTime? LastBackupAt { get; set; }
    /// <summary>
    /// The version of the database schema.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;
    /// <summary>
    /// Indicates whether the database is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// The number of active connections to the database.
    /// </summary>
    public int ActiveConnectionCount { get; set; }
    /// <summary>
    /// The encryption key used to encrypt the database (if any).
    /// </summary>
    public string? EncryptionKey { get; set; }
    /// <summary>
    /// Indicates whether the database requires encryption.
    /// </summary>
    public bool RequiresEncryption { get; set; }

    // Navigation properties
    /// <summary>
    /// The tenant that owns this database.
    /// </summary>
    public Tenant? Tenant { get; set; }
    /// <summary>
    /// The collection of migrations applied to this database.
    /// </summary>
    public ICollection<Migration> Migrations { get; set; } = new List<Migration>();
    /// <summary>
    /// The collection of backups for this database.
    /// </summary>
    public ICollection<Backup> Backups { get; set; } = new List<Backup>();

    /// <summary>
    /// Indicates whether the database has encryption enabled.
    /// </summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(EncryptionKey);

    /// <summary>
    /// Validates the database entity
    /// </summary>
    /// <param name="errors">The list of validation errors, if any.</param>
    /// <returns>True if the database entity is valid; otherwise, false.</returns>
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
    /// <param name="newSizeBytes">The new size of the database in bytes.</param>
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

    public override string ToString() => $"TenantDatabase {{ DatabaseId = {DatabaseId}, TenantId = {TenantId}, Name = {Name}, FilePath = {FilePath}, SizeBytes = {SizeBytes}, CreatedAt = {CreatedAt} }}";
}
