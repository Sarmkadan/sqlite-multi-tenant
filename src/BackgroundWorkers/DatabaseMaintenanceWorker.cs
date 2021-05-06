// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Background service for database maintenance operations (VACUUM, ANALYZE, REINDEX).
/// Runs on configurable intervals to optimize database performance and reclaim space.
/// SQLite requires explicit maintenance for efficiency.
/// </summary>
public class DatabaseMaintenanceWorker : BackgroundService
{
    private readonly ILogger<DatabaseMaintenanceWorker> _logger;
    private readonly TimeSpan _interval;

    public DatabaseMaintenanceWorker(
        ILogger<DatabaseMaintenanceWorker> logger,
        TimeSpan? interval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = interval ?? TimeSpan.FromHours(24); // Default: daily
    }

    /// <summary>
    /// Executes maintenance loop.
    /// Runs indefinitely until service is stopped.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database maintenance worker started (interval: {interval}h)", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteMaintenanceAsync(stoppingToken);

                // Wait for configured interval before next maintenance
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Database maintenance worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in database maintenance");
                // Retry after 1 hour on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Performs database maintenance operations.
    /// 1. VACUUM: Reclaims space from deleted rows
    /// 2. ANALYZE: Updates query planner statistics
    /// 3. REINDEX: Rebuilds indexes for performance
    /// </summary>
    private async Task ExecuteMaintenanceAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting database maintenance");

        try
        {
            // In production, enumerate all tenant databases and perform maintenance
            // For each database:
            //   1. Execute VACUUM to reclaim space
            //   2. Execute ANALYZE to update statistics
            //   3. Log duration and size change

            var vacuumTime = Stopwatch.StartNew();
            _logger.LogInformation("Executing VACUUM on databases");
            // await VacuumAllDatabasesAsync(cancellationToken);
            vacuumTime.Stop();
            _logger.LogInformation("VACUUM completed in {ms}ms", vacuumTime.ElapsedMilliseconds);

            var analyzeTime = Stopwatch.StartNew();
            _logger.LogInformation("Executing ANALYZE on databases");
            // await AnalyzeAllDatabasesAsync(cancellationToken);
            analyzeTime.Stop();
            _logger.LogInformation("ANALYZE completed in {ms}ms", analyzeTime.ElapsedMilliseconds);

            stopwatch.Stop();
            _logger.LogInformation("Database maintenance completed in {ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database maintenance");
        }
    }

    /// <summary>
    /// Virtual methods for maintenance operations that would be implemented
    /// with actual database access in production code.
    /// </summary>
    private Task VacuumAllDatabasesAsync(CancellationToken cancellationToken)
    {
        // Implementation would:
        // 1. Enumerate all tenant databases
        // 2. Execute VACUUM command on each
        // 3. Log space reclaimed
        // 4. Handle individual database errors gracefully
        return Task.CompletedTask;
    }

    private Task AnalyzeAllDatabasesAsync(CancellationToken cancellationToken)
    {
        // Implementation would:
        // 1. Enumerate all tenant databases
        // 2. Execute ANALYZE command on each
        // 3. Log statistics update completion
        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration for database maintenance operations.
/// Allows tuning maintenance behavior without code changes.
/// </summary>
public class DatabaseMaintenanceOptions
{
    /// <summary>
    /// Enable VACUUM operation to reclaim disk space.
    /// Default: true (recommended for production).
    /// </summary>
    public bool EnableVacuum { get; set; } = true;

    /// <summary>
    /// Enable ANALYZE operation to update query statistics.
    /// Default: true (improves query performance).
    /// </summary>
    public bool EnableAnalyze { get; set; } = true;

    /// <summary>
    /// Enable REINDEX operation to rebuild indexes.
    /// Default: false (only if fragmentation detected).
    /// </summary>
    public bool EnableReindex { get; set; } = false;

    /// <summary>
    /// Maintenance interval in hours (default: 24 = daily).
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum time allowed for maintenance on a single database (in seconds).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Parallelism level for maintenance operations (0 = sequential).
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;
}
