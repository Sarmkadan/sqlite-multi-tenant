#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Tenants
{
    /// <summary>
    /// Provides extension methods for <see cref="TenantRecoveryService"/> to enhance recovery operations
    /// with additional convenience and batch functionality.
    /// </summary>
    public static class TenantRecoveryServiceExtensions
    {
        /// <summary>
        /// Attempts to repair multiple tenant databases in sequence.
        /// </summary>
        /// <param name="service">The tenant recovery service.</param>
        /// <param name="tenantIds">Collection of tenant IDs to repair. Must not be null or empty.</param>
        /// <returns>Number of successfully repaired databases.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="tenantIds"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantIds"/> is empty.</exception>
        public static async Task<int> RepairDatabasesAsync(this TenantRecoveryService service, string[] tenantIds)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(tenantIds);

            if (tenantIds.Length == 0)
            {
                throw new ArgumentException("At least one tenant ID must be provided", nameof(tenantIds));
            }

            var successCount = 0;

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    var result = await service.RepairDatabaseAsync(tenantId);
                    if (result)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    service.Log?.LogError(ex, "Failed to repair database for tenant: {TenantId}", tenantId);
                }
            }

            return successCount;
        }

        /// <summary>
        /// Restores multiple tenant databases from their respective backups.
        /// </summary>
        /// <param name="service">The tenant recovery service.</param>
        /// <param name="restoreSpecs">Collection of tenant ID and backup path pairs. Must not be null or empty.</param>
        /// <returns>Number of successfully restored databases.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="restoreSpecs"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="restoreSpecs"/> is empty.</exception>
        public static async Task<int> RestoreFromBackupsAsync(this TenantRecoveryService service,
            (string TenantId, string BackupPath)[] restoreSpecs)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(restoreSpecs);

            if (restoreSpecs.Length == 0)
            {
                throw new ArgumentException("At least one restore specification must be provided", nameof(restoreSpecs));
            }

            var successCount = 0;

            foreach (var (tenantId, backupPath) in restoreSpecs)
            {
                try
                {
                    var result = await service.RestoreFromBackupAsync(tenantId, backupPath);
                    if (result)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    service.Log?.LogError(ex, "Failed to restore backup for tenant: {TenantId}", tenantId);
                }
            }

            return successCount;
        }

        /// <summary>
        /// Performs point-in-time recovery for multiple tenants simultaneously.
        /// </summary>
        /// <param name="service">The tenant recovery service.</param>
        /// <param name="recoveryRequests">Collection of recovery specifications. Must not be null or empty.</param>
        /// <returns>Number of successful point-in-time recoveries.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="recoveryRequests"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="recoveryRequests"/> is empty.</exception>
        public static async Task<int> PointInTimeRecoveryAsync(this TenantRecoveryService service,
            (string TenantId, DateTime TargetTime, string BackupDirectory)[] recoveryRequests)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(recoveryRequests);

            if (recoveryRequests.Length == 0)
            {
                throw new ArgumentException("At least one recovery request must be provided", nameof(recoveryRequests));
            }

            var successCount = 0;

            foreach (var (tenantId, targetTime, backupDirectory) in recoveryRequests)
            {
                try
                {
                    var result = await service.PointInTimeRecoveryAsync(tenantId, targetTime, backupDirectory);
                    if (result)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    service.Log?.LogError(ex, "Failed point-in-time recovery for tenant: {TenantId}", tenantId);
                }
            }

            return successCount;
        }

        /// <summary>
        /// Cleans up stale backups for multiple tenants with a single retention policy.
        /// </summary>
        /// <param name="service">The tenant recovery service.</param>
        /// <param name="tenantIds">Collection of tenant IDs to cleanup. Must not be null or empty.</param>
        /// <param name="retentionPeriod">Time period after which backups are considered stale.</param>
        /// <returns>Total number of deleted backup files across all tenants.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="tenantIds"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantIds"/> is empty.</exception>
        public static async Task<int> CleanupStaleBackupsAsync(this TenantRecoveryService service,
            string[] tenantIds, TimeSpan retentionPeriod)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(tenantIds);

            if (tenantIds.Length == 0)
            {
                throw new ArgumentException("At least one tenant ID must be provided", nameof(tenantIds));
            }

            var totalDeleted = 0;

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    var deletedCount = await service.CleanupStaleBackupsAsync(tenantId, retentionPeriod);
                    totalDeleted += deletedCount;
                }
                catch (Exception ex)
                {
                    service.Log?.LogError(ex, "Failed to cleanup backups for tenant: {TenantId}", tenantId);
                }
            }

            return totalDeleted;
        }
    }
}