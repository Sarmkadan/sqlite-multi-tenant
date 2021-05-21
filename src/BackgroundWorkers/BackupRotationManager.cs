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
    // Manages backup file rotation and cleanup to prevent disk space issues
    public sealed class BackupRotationManager {
        private readonly ILogger<BackupRotationManager> _logger;

        public BackupRotationManager(ILogger<BackupRotationManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Rotates backups based on retention policy
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
                var filesToDelete = backupFiles.Where(f => f.CreationTimeUtc < cutoffDate).ToList();

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

        // Verifies backup integrity
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

        // Estimates disk space used by backups
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

        // Gets backup statistics
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

    public sealed class BackupRotationPolicy {
        public TimeSpan MaxBackupAge { get; set; } = TimeSpan.FromDays(30);
        public int MaxBackupCount { get; set; } = 20;
        public long MaxDiskUsage { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB
    }

    public sealed class BackupRotationResult {
        public bool IsSuccessful { get; set; }
        public int TotalBackups { get; set; }
        public int RemainingBackups { get; set; }
        public int DeletedByAge { get; set; }
        public int DeletedByCount { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string Error { get; set; }
    }

    public sealed class BackupVerificationResult {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; }
        public bool IsReadable { get; set; }
        public string Error { get; set; }
    }

    public sealed class BackupStatistics {
        public int TotalBackups { get; set; }
        public DateTime? OldestBackup { get; set; }
        public DateTime? NewestBackup { get; set; }
        public long TotalDiskUsage { get; set; }
        public long AverageBackupSize { get; set; }
    }
}
