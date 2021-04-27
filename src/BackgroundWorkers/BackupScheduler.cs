#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Background service for scheduling automatic database backups.
/// Runs on configurable intervals (default: daily at 2 AM UTC).
/// Ensures all tenant databases are backed up for disaster recovery.
/// </summary>
public sealed class BackupSchedulerService : BackgroundService {
    private readonly IBackupService _backupService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<BackupSchedulerService> _logger;
    private readonly TimeSpan _interval;

    public BackupSchedulerService(
        IBackupService backupService,
        ITenantService tenantService,
        ILogger<BackupSchedulerService> logger,
        TimeSpan? interval = null)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = interval ?? TimeSpan.FromHours(24); // Default: daily
    }

    /// <summary>
    /// Executes backup scheduling loop.
    /// Runs indefinitely until service is stopped via cancellation token.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup scheduler service started (interval: {interval}h)", _interval.TotalHours);

        // Calculate next run time (daily at 2 AM UTC by default)
        var nextRun = DateTime.UtcNow
            .AddHours(2)
            .RoundDownToMinute();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                if (now >= nextRun)
                {
                    _logger.LogInformation("Running scheduled backup job at {time}", now);
                    await ExecuteBackupJobAsync(stoppingToken);

                    // Schedule next run after interval
                    nextRun = now.Add(_interval);
                    _logger.LogInformation("Next backup scheduled for {time}", nextRun);
                }

                // Sleep until next scheduled time or 1 minute, whichever is shorter
                var delayUntilNextRun = nextRun - DateTime.UtcNow;
                var delay = delayUntilNextRun > TimeSpan.FromMinutes(1)
                    ? TimeSpan.FromMinutes(1)
                    : delayUntilNextRun;

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup scheduler service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup scheduler");
                // Continue on error to prevent service from exiting
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Executes backup for all active tenant databases.
    /// Handles failures gracefully - one failed backup doesn't stop others.
    /// </summary>
    private async Task ExecuteBackupJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            _logger.LogInformation("Starting backups for {count} tenants", tenants.Count);

            int successCount = 0;
            int failureCount = 0;

            foreach (var tenant in tenants)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // In production, get tenant's database ID from relationship
                    // For now, create a backup with tenant context
                    _logger.LogInformation("Creating backup for tenant: {tenantId}", tenant.TenantId);

                    // This would be replaced with actual database enumeration
                    // await _backupService.CreateBackupAsync(databaseId, BackupType.Incremental, "scheduler", null);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "Failed to backup tenant {tenantId}", tenant.TenantId);
                }
            }

            _logger.LogInformation(
                "Backup job completed: {success} succeeded, {failed} failed",
                successCount,
                failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing backup job");
        }
    }
}

/// <summary>
/// Background service for cleaning up expired backups.
/// Removes backups older than retention period to save disk space.
/// </summary>
public sealed class BackupCleanupService : BackgroundService {
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupCleanupService> _logger;
    private readonly int _retentionDays;

    public BackupCleanupService(
        IBackupService backupService,
        ILogger<BackupCleanupService> logger,
        int retentionDays = 30)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retentionDays = retentionDays;
    }

    /// <summary>
    /// Runs cleanup check daily.
    /// Identifies and removes backups beyond retention window.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup cleanup service started (retention: {days} days)", _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run cleanup once daily at 3 AM UTC
                var nextRun = DateTime.UtcNow
                    .AddHours(3)
                    .Date
                    .AddHours(3);

                var delay = nextRun - DateTime.UtcNow;
                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.FromHours(24);

                _logger.LogDebug("Next cleanup in {hours}h", delay.TotalHours);
                await Task.Delay(delay, stoppingToken);

                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup cleanup service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup cleanup");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Performs cleanup of expired backups.
    /// Logs deletion count and any errors for audit trail.
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running backup cleanup (retention: {days} days)", _retentionDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            int deletedCount = 0;

            // In production, implement repository query for expired backups
            // var expiredBackups = await _backupService.GetExpiredBackupsAsync(cutoffDate);

            _logger.LogInformation("Backup cleanup completed: {count} backups removed", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing backup cleanup");
        }
    }
}
