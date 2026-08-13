#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;
using System.Diagnostics;

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Background service for database maintenance operations (VACUUM, ANALYZE, REINDEX).
/// Runs on configurable intervals to optimize database performance and reclaim space.
/// SQLite requires explicit maintenance for efficiency.
/// </summary>
public sealed class DatabaseMaintenanceWorker : BackgroundService {
    private readonly ILogger<DatabaseMaintenanceWorker> _logger;
    private readonly ITenantDatabaseMaintenanceService _maintenanceService;
    private readonly TimeSpan _interval;

    public DatabaseMaintenanceWorker(
        ILogger<DatabaseMaintenanceWorker> logger,
        ITenantDatabaseMaintenanceService maintenanceService,
        TimeSpan? interval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
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
                var maintenanceResults = await ExecuteMaintenanceAsync(stoppingToken);

                // Log summary of maintenance operations
                var successfulOperations = maintenanceResults.Count(r => r.IsSuccess);
                var failedOperations = maintenanceResults.Count(r => !r.IsSuccess);
                _logger.LogInformation("Database maintenance completed: {Successful} successful, {Failed} failed out of {Total} operations",
                    successfulOperations, failedOperations, maintenanceResults.Count);

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
    /// Performs database maintenance operations on all tenant databases.
    /// Executes VACUUM to reclaim space, ANALYZE to update statistics, and PRAGMA optimize for performance tuning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    private async Task<List<TenantMaintenanceResult>> ExecuteMaintenanceAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting database maintenance");

        var results = new List<TenantMaintenanceResult>();

        try
        {
            // Execute full maintenance on all tenant databases
            // This performs VACUUM + ANALYZE + PRAGMA optimize
            results = (await _maintenanceService.PerformFullMaintenanceOnAllAsync(cancellationToken)).ToList();

            // Log detailed results
            foreach (var result in results.Where(r => r.IsSuccess))
            {
                _logger.LogInformation("{Operation}", result.OperationSummary);
            }

            foreach (var result in results.Where(r => !r.IsSuccess))
            {
                _logger.LogWarning("Failed: {Operation} - {Error}", result.Operation, result.Error);
            }

            stopwatch.Stop();
            _logger.LogInformation("Database maintenance completed in {ms}ms for {count} tenants",
                stopwatch.ElapsedMilliseconds, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database maintenance");
            // Return partial results if available
            if (results.Count == 0)
            {
                results.Add(new TenantMaintenanceResult
                {
                    Operation = "Database Maintenance",
                    StartedAt = DateTime.UtcNow,
                    Error = ex.Message,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    // Added ToString override for concise representation
    public override string ToString()
    {
        var opts = new DatabaseMaintenanceOptions();
        return $"DatabaseMaintenanceWorker {{ EnableVacuum = {opts.EnableVacuum}, EnableAnalyze = {opts.EnableAnalyze}, EnableReindex = {opts.EnableReindex}, IntervalHours = {opts.IntervalHours}, TimeoutSeconds = {opts.TimeoutSeconds}, DegreeOfParallelism = {opts.DegreeOfParallelism} }}";
    }
}

/// <summary>
/// Configuration for database maintenance operations.
/// Allows tuning maintenance behavior without code changes.
/// </summary>
public sealed class DatabaseMaintenanceOptions {
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
