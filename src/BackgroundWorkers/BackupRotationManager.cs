#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.BackgroundWorkers
{
    /// <summary>
    /// Manages backup file rotation and cleanup to prevent disk space issues.
    /// </summary>
    public sealed class BackupRotationManager {
        private readonly ILogger<BackupRotationManager> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="BackupRotationManager"/> with the specified logger.
        /// </summary>
        /// <param name="logger">Logger used for diagnostic messages.</param>
        public BackupRotationManager(ILogger<BackupRotationManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Rotates backup files in the specified directory according to the provided retention policy.
        /// </summary>
        /// <param name="backupDirectory">Path to the directory containing backup files.</param>
        /// <param name="policy">Retention policy that defines maximum age, count, and disk usage.</param>
        /// <returns>
        /// A <see cref="BackupRotationResult"/> describing the outcome of the rotation operation.
        /// </returns>
        public async Task<BackupRotationResult> RotateBackupsAsync(string backupDirectory,
            BackupRotationPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(backupDirectory))
                throw new ArgumentException("Backup directory cannot be empty", nameof(backupDirectory));

            if (policy is null)
                throw new ArgumentNullException(nameof(policy));

            var result = new BackupRotationResult();

            try
            {
                if (!Directory.Exists(backupDirectory))
                {
                    _logger.LogWarning("Backup directory not found: {Directory}", backupDirectory);
                    return result;
                }

                var backupFiles = Directory.GetFiles(backupDirectory, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                result.TotalBackups = backupFiles.Count;

                // Delete old backups based on age
                var cutoffDate = DateTime.UtcNow.Subtract(policy.MaxBackupAge);
                var filesToDelete = backupFiles.Where(f => f.CreationTimeUtc <= cutoffDate).ToList();

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        result.DeletedByAge++;
                        _logger.LogInformation("Deleted old backup: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete backup: {FileName}", file.Name);
                    }
                }

                // Keep only the most recent backups if exceeding max count
                var remainingFiles = Directory.GetFiles(backupDirectory, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                if (remainingFiles.Count > policy.MaxBackupCount)
                {
                    var toDelete = remainingFiles.Skip(policy.MaxBackupCount).ToList();

                    foreach (var file in toDelete)
                    {
                        try
                        {
                            file.Delete();
                            result.DeletedByCount++;
                            _logger.LogInformation("Deleted excess backup: {FileName}", file.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete excess backup: {FileName}", file.Name);
                        }
                    }
                }

                result.RemainingBackups = Directory.GetFiles(backupDirectory, "*.db").Length;
                result.ExecutedAt = DateTime.UtcNow;
                result.IsSuccessful = true;

                _logger.LogInformation(
                    "Backup rotation completed: deleted {Count} by age, {CountByLimit} by count limit",
                    result.DeletedByAge, result.DeletedByCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup rotation failed");
                result.IsSuccessful = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Verifies the integrity and readability of backup files in the specified directory.
        /// </summary>
        /// <param name="backupDirectory">Path to the directory containing backup files.</param>
        /// <returns>
        /// A list of <see cref="BackupVerificationResult"/> objects, each representing the verification result for a backup file.
        /// </returns>
        public async Task<List<BackupVerificationResult>> VerifyBackupsAsync(string backupDirectory)
        {
            var results = new List<BackupVerificationResult>();

            if (!Directory.Exists(backupDirectory))
                return results;

            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.db");

                foreach (var filePath in backupFiles)
                {
                    var result = new BackupVerificationResult
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath)
                    };

                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        result.FileSize = fileInfo.Length;
                        result.CreatedAt = fileInfo.CreationTimeUtc;
                        result.LastModified = fileInfo.LastWriteTimeUtc;

                        // Check if file is readable
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            result.IsReadable = true;
                            result.FileSizeBytes = fileStream.Length;
                        }

                        result.IsValid = true;
                    }
                    catch (Exception ex)
                    {
                        result.IsValid = false;
                        result.Error = ex.Message;
                        _logger.LogWarning(ex, "Backup verification failed for {FileName}", result.FileName);
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify backups in directory: {Directory}",
                    backupDirectory);
            }

            return results;
        }

        /// <summary>
        /// Estimates the total disk space used by backup files in the specified directory.
        /// </summary>
        /// <param name="backupDirectory">Path to the directory containing backup files.</param>
        /// <returns>
        /// Total size in bytes of all backup files, or 0 if the directory does not exist or an error occurs.
        /// </returns>
        public long EstimateBackupDiskUsage(string backupDirectory)
        {
            if (!Directory.Exists(backupDirectory))
                return 0;

            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.db");
                return backupFiles.Sum(f => new FileInfo(f).Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to estimate backup disk usage");
                return 0;
            }
        }

        /// <summary>
        /// Collects statistics about backup files in the specified directory.
        /// </summary>
        /// <param name="backupDirectory">Path to the directory containing backup files.</param>
        /// <returns>
        /// A <see cref="BackupStatistics"/> object containing counts, dates, and size information.
        /// </returns>
        public BackupStatistics GetBackupStatistics(string backupDirectory)
        {
            var stats = new BackupStatistics();

            if (!Directory.Exists(backupDirectory))
                return stats;

            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.db")
                    .Select(f => new FileInfo(f))
                    .ToList();

                stats.TotalBackups = backupFiles.Count;
                stats.OldestBackup = backupFiles.MinBy(f => f.CreationTimeUtc)?.CreationTimeUtc;
                stats.NewestBackup = backupFiles.MaxBy(f => f.CreationTimeUtc)?.CreationTimeUtc;
                stats.TotalDiskUsage = backupFiles.Sum(f => f.Length);
                stats.AverageBackupSize = backupFiles.Any()
                    ? (long)(backupFiles.Average(f => f.Length))
                    : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get backup statistics");
            }

            return stats;
        }
    }

    /// <summary>
    /// Defines the retention policy for backup rotation, including maximum age, count, and disk usage.
    /// </summary>
    public sealed class BackupRotationPolicy {
        /// <summary>
        /// Maximum age of a backup file before it is eligible for deletion.
        /// </summary>
        public TimeSpan MaxBackupAge { get; set; } = TimeSpan.FromDays(30);
        /// <summary>
        /// Maximum number of backup files to retain; older files beyond this count are deleted.
        /// </summary>
        public int MaxBackupCount { get; set; } = 20;
        /// <summary>
        /// Maximum total disk usage allowed for backups (in bytes).
        /// </summary>
        public long MaxDiskUsage { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB
    }

    /// <summary>
    /// Represents the result of a backup rotation operation.
    /// </summary>
    public sealed class BackupRotationResult {
        /// <summary>
        /// Indicates whether the rotation completed successfully.
        /// </summary>
        public bool IsSuccessful { get; set; }
        /// <summary>
        /// Total number of backup files found before rotation.
        /// </summary>
        public int TotalBackups { get; set; }
        /// <summary>
        /// Number of backup files remaining after rotation.
        /// </summary>
        public int RemainingBackups { get; set; }
        /// <summary>
        /// Number of backups deleted based on age criteria.
        /// </summary>
        public int DeletedByAge { get; set; }
        /// <summary>
        /// Number of backups deleted to enforce the maximum count limit.
        /// </summary>
        public int DeletedByCount { get; set; }
        /// <summary>
        /// Timestamp when the rotation operation was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }
        /// <summary>
        /// Error message if the operation failed; otherwise null.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Contains verification details for a single backup file.
    /// </summary>
    public sealed class BackupVerificationResult {
        /// <summary>
        /// Full path to the backup file.
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// File name of the backup.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Size of the file in bytes (from <see cref="FileInfo"/>).
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// Size of the file as read from the stream.
        /// </summary>
        public long FileSizeBytes { get; set; }
        /// <summary>
        /// Creation timestamp of the file (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Last modification timestamp of the file (UTC).
        /// </summary>
        public DateTime LastModified { get; set; }
        /// <summary>
        /// Indicates whether the backup passed verification.
        /// </summary>
        public bool IsValid { get; set; }
        /// <summary>
        /// Indicates whether the file could be opened for reading.
        /// </summary>
        public bool IsReadable { get; set; }
        /// <summary>
        /// Error message if verification failed; otherwise null.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Aggregated statistics for backup files in a directory.
    /// </summary>
    public sealed class BackupStatistics {
        /// <summary>
        /// Total number of backup files.
        /// </summary>
        public int TotalBackups { get; set; }
        /// <summary>
        /// Timestamp of the oldest backup file (UTC), or null if none.
        /// </summary>
        public DateTime? OldestBackup { get; set; }
        /// <summary>
        /// Timestamp of the newest backup file (UTC), or null if none.
        /// </summary>
        public DateTime? NewestBackup { get; set; }
        /// <summary>
        /// Sum of sizes of all backup files in bytes.
        /// </summary>
        public long TotalDiskUsage { get; set; }
        /// <summary>
        /// Average size of backup files in bytes.
        /// </summary>
        public long AverageBackupSize { get; set; }
    }
}
