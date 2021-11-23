#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tenants
{
    // Provides disaster recovery capabilities for tenant databases
    // Includes point-in-time recovery, backup restoration, and corruption repair
    public sealed class TenantRecoveryService {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<TenantRecoveryService> _logger;

        public TenantRecoveryService(ITenantRepository tenantRepository, ILogger<TenantRecoveryService> logger)
        {
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

    /// <summary>
    /// Gets the logger instance for this service.
    /// </summary>
    public ILogger<TenantRecoveryService> Log => _logger;

        // Attempts to repair a corrupted database file
        public async Task<bool> RepairDatabaseAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant is null)
                {
                    _logger.LogWarning("Tenant not found for repair: {TenantId}", tenantId);
                    return false;
                }

                var dbPath = tenant.DatabasePath;
                if (!File.Exists(dbPath))
                {
                    _logger.LogWarning("Database file not found: {DbPath}", dbPath);
                    return false;
                }

                // Create a backup before attempting repair
                var backupPath = $"{dbPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Copy(dbPath, backupPath, overwrite: true);

                try
                {
                    using (var connection = new SQLiteConnection($"Data Source={dbPath};"))
                    {
                        await connection.OpenAsync();

                        // Run PRAGMA integrity_check
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA integrity_check";
                            var result = await command.ExecuteScalarAsync();

                            if (result?.ToString() == "ok")
                            {
                                _logger.LogInformation("Database is already healthy: {TenantId}", tenantId);
                                File.Delete(backupPath);
                                return true;
                            }
                        }

                        // Attempt repair using VACUUM
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "VACUUM";
                            await command.ExecuteNonQueryAsync();
                        }

                        // Verify repair
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA integrity_check";
                            var result = await command.ExecuteScalarAsync();

                            if (result?.ToString() == "ok")
                            {
                                _logger.LogInformation("Database repair successful: {TenantId}", tenantId);
                                File.Delete(backupPath);
                                return true;
                            }
                        }
                    }
                }
                catch (Exception repairEx)
                {
                    _logger.LogError(repairEx, "Repair failed, keeping backup: {TenantId}", tenantId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database repair failed for tenant: {TenantId}", tenantId);
                return false;
            }
        }

        // Restores database from a backup
        public async Task<bool> RestoreFromBackupAsync(string tenantId, string backupPath)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                throw new ArgumentException("Backup file not found", nameof(backupPath));

            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant is null)
                {
                    _logger.LogWarning("Tenant not found for restore: {TenantId}", tenantId);
                    return false;
                }

                var originalPath = tenant.DatabasePath;

                // Create a safety backup of current state
                var safetyBackupPath = $"{originalPath}.pre_restore.{DateTime.UtcNow:yyyyMMddHHmmss}";
                if (File.Exists(originalPath))
                {
                    File.Copy(originalPath, safetyBackupPath, overwrite: true);
                }

                try
                {
                    // Restore from backup
                    File.Copy(backupPath, originalPath, overwrite: true);

                    // Verify restored database
                    using (var connection = new SQLiteConnection($"Data Source={originalPath};"))
                    {
                        await connection.OpenAsync();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA integrity_check";
                            var result = await command.ExecuteScalarAsync();

                            if (result?.ToString() == "ok")
                            {
                                _logger.LogInformation("Database restored successfully: {TenantId}", tenantId);
                                File.Delete(safetyBackupPath);
                                return true;
                            }
                        }
                    }

                    // Restore failed, revert to original
                    if (File.Exists(safetyBackupPath))
                    {
                        File.Copy(safetyBackupPath, originalPath, overwrite: true);
                    }
                }
                catch (Exception restoreEx)
                {
                    _logger.LogError(restoreEx, "Restore operation failed: {TenantId}", tenantId);

                    // Revert to safety backup
                    if (File.Exists(safetyBackupPath))
                    {
                        File.Copy(safetyBackupPath, originalPath, overwrite: true);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup restoration failed for tenant: {TenantId}", tenantId);
                return false;
            }
        }

        // Removes orphaned backup files
        public async Task<int> CleanupStaleBackupsAsync(string tenantId, TimeSpan retentionPeriod)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant is null)
                    return 0;

                var dbPath = tenant.DatabasePath;
                var dbDir = Path.GetDirectoryName(dbPath);
                var filePattern = $"{Path.GetFileName(dbPath)}.backup.*";

                var backupFiles = Directory.GetFiles(dbDir, filePattern);
                var cutoffTime = DateTime.UtcNow.Subtract(retentionPeriod);
                var deletedCount = 0;

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffTime)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete backup file: {File}", file);
                        }
                    }
                }

                _logger.LogInformation("Cleaned up {Count} stale backups for tenant: {TenantId}",
                    deletedCount, tenantId);

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup backups for tenant: {TenantId}", tenantId);
                return 0;
            }
        }

        // Performs a point-in-time recovery (simulated via backup restore)
        public async Task<bool> PointInTimeRecoveryAsync(string tenantId, DateTime targetTime,
            string backupDirectory)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            if (!Directory.Exists(backupDirectory))
                throw new DirectoryNotFoundException($"Backup directory not found: {backupDirectory}");

            try
            {
                // Find backup closest to target time
                var backupFiles = Directory.GetFiles(backupDirectory,
                    $"{tenantId}_*.backup");

                DateTime? closestBackupTime = null;
                string closestBackupPath = null;

                foreach (var backupFile in backupFiles)
                {
                    if (DateTime.TryParse(
                        Path.GetFileNameWithoutExtension(backupFile).Split('_')[1],
                        out var backupTime))
                    {
                        if (backupTime <= targetTime)
                        {
                            if (closestBackupTime is null ||
                                (targetTime - backupTime) < (targetTime - closestBackupTime))
                            {
                                closestBackupTime = backupTime;
                                closestBackupPath = backupFile;
                            }
                        }
                    }
                }

                if (closestBackupPath is null)
                {
                    _logger.LogWarning(
                        "No backup found for point-in-time recovery to {TargetTime} for tenant {TenantId}",
                        targetTime, tenantId);
                    return false;
                }

                _logger.LogInformation(
                    "Restoring tenant {TenantId} to point in time {TargetTime} using backup from {BackupTime}",
                    tenantId, targetTime, closestBackupTime);

                return await RestoreFromBackupAsync(tenantId, closestBackupPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Point-in-time recovery failed for tenant {TenantId} to {TargetTime}",
                    tenantId, targetTime);
                return false;
            }
        }
    }
}
