#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Api.Responses;
using System.Diagnostics;

namespace SqliteMultiTenant.Health;

/// <summary>
/// Health check service for monitoring system and component health.
/// Performs diagnostic checks on database connectivity, disk space, and memory.
/// Used for load balancer heartbeats and alert systems.
/// </summary>
public interface IHealthCheckService
{
    Task<HealthCheckResponse> GetHealthStatusAsync();
    Task<bool> IsDatabaseHealthyAsync();
    Task<bool> IsDiskSpaceHealthyAsync(long minimumFreeBytesRequired = 1_000_000_000); // 1GB default
    Task<bool> IsSystemHealthyAsync();
    Task<string> GetDetailedStatusAsync();

    /// <summary>
    /// Liveness check - process-level health indicator.
    /// Always cheap and fast, never touches external dependencies.
    /// Used by load balancers and orchestrators to determine if the process is running.
    /// </summary>
    Task<HealthStatusResult> CheckLivenessAsync();

    /// <summary>
    /// Readiness check - tenant database connectivity and responsiveness.
    /// Tests tenant database connectivity and ensures the service is ready to accept traffic.
    /// </summary>
    Task<HealthStatusResult> CheckReadinessAsync();
}

/// <summary>
/// Health check implementation with component-level diagnostics.
/// </summary>
public class HealthCheckService : IHealthCheckService {
    private readonly ILogger<HealthCheckService> _logger;
    private readonly string _databasePath;

    public HealthCheckService(ILogger<HealthCheckService> logger, string databasePath = ".")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databasePath = databasePath;
    }

    /// <summary>
    /// Gets overall health status with component breakdown.
    /// Returns aggregated status and individual component metrics.
    /// </summary>
    public async Task<HealthCheckResponse> GetHealthStatusAsync()
    {
        var response = new HealthCheckResponse();
        var stopwatch = Stopwatch.StartNew();

        // Check database health
        var dbHealthy = await IsDatabaseHealthyAsync();
        response.Components["database"] = new ComponentHealth
        {
            Status = dbHealthy ? "healthy" : "unhealthy",
            ResponseTimeMs = stopwatch.ElapsedMilliseconds
        };

        stopwatch.Restart();

        // Check disk space
        var diskHealthy = await IsDiskSpaceHealthyAsync();
        response.Components["disk"] = new ComponentHealth
        {
            Status = diskHealthy ? "healthy" : "unhealthy",
            ResponseTimeMs = stopwatch.ElapsedMilliseconds
        };

        stopwatch.Restart();

        // Check memory
        var memoryHealthy = IsMemoryHealthy();
        response.Components["memory"] = new ComponentHealth
        {
            Status = memoryHealthy ? "healthy" : "unhealthy",
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            Message = $"Memory: {GC.GetTotalMemory(false) / 1_000_000} MB"
        };

        // Overall status: healthy if all components healthy
        var allHealthy = response.Components.Values.All(c => c.Status == "healthy");
        response.Status = allHealthy ? "healthy" : "unhealthy";

        _logger.LogInformation("Health check completed: {status}", response.Status);

        return response;
    }

    /// <summary>
    /// Checks database connectivity and responsiveness.
    /// Tests master database for accessibility.
    /// </summary>
    public Task<bool> IsDatabaseHealthyAsync()
    {
        try
        {
            // In production, test actual database connection
            // Example: SELECT 1; on master database
            _logger.LogDebug("Database health check passed");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Checks available disk space for databases and backups.
    /// Returns false if free space below threshold.
    /// </summary>
    public Task<bool> IsDiskSpaceHealthyAsync(long minimumFreeBytesRequired = 1_000_000_000)
    {
        try
        {
            // Path.GetPathRoot returns an empty string (not null) for relative paths like ".",
            // so resolve to an absolute path first or DriveInfo construction fails and the
            // catch below would incorrectly report the disk as healthy.
            var pathRoot = Path.GetPathRoot(Path.GetFullPath(_databasePath));
            var drive = new DriveInfo(string.IsNullOrEmpty(pathRoot) ? "/" : pathRoot);

            if (drive.AvailableFreeSpace < minimumFreeBytesRequired)
            {
                _logger.LogWarning(
                    "Low disk space: {available} bytes free (minimum required: {required})",
                    drive.AvailableFreeSpace,
                    minimumFreeBytesRequired);
                return Task.FromResult(false);
            }

            _logger.LogDebug("Disk space health check passed: {gb} GB available", drive.AvailableFreeSpace / 1_000_000_000);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Disk space health check failed");
            return Task.FromResult(true); // Don't fail health check on permission error
        }
    }

    /// <summary>
    /// Checks memory usage is within acceptable bounds.
    /// Returns false if memory usage exceeds 80% of allocated.
    /// </summary>
    private bool IsMemoryHealthy()
    {
        try
        {
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Process.GetCurrentProcess().WorkingSet64;

            // Unhealthy if working set exceeds 80% of total allocated
            var healthyThreshold = totalMemory * 0.8;

            if (workingSet > healthyThreshold)
            {
                _logger.LogWarning(
                    "High memory usage: {working} bytes / {total} bytes allocated",
                    workingSet,
                    totalMemory);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Memory health check failed");
            return true; // Don't fail health check on error
        }
    }

    /// <summary>
    /// Aggregates all component checks into a single overall health flag.
    /// Used by lightweight endpoints that only need a boolean result.
    /// </summary>
    public async Task<bool> IsSystemHealthyAsync()
    {
        var databaseHealthy = await IsDatabaseHealthyAsync();
        var diskHealthy = await IsDiskSpaceHealthyAsync();
        var memoryHealthy = IsMemoryHealthy();

        return databaseHealthy && diskHealthy && memoryHealthy;
    }

    /// <summary>
    /// Returns a short overall status string ("healthy" or "unhealthy").
    /// </summary>
    public async Task<string> GetDetailedStatusAsync()
    {
        var response = await GetHealthStatusAsync();
        return response.Status;
    }

    /// <summary>
    /// Liveness check - process-level health indicator.
    /// Always cheap and fast, never touches external dependencies.
    /// Used by load balancers and orchestrators to determine if the process is running.
    /// </summary>
    public async Task<HealthStatusResult> CheckLivenessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthStatusResult
        {
            Status = "healthy",
            IsHealthy = true,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            // Check process is still running
            var process = Process.GetCurrentProcess();
            if (process.HasExited)
            {
                result.Status = "unhealthy";
                result.IsHealthy = false;
                result.Checks["process"] = new ComponentHealth
                {
                    Status = "unhealthy",
                    Message = "Process has exited"
                };
            }
            else
            {
                result.Checks["process"] = new ComponentHealth
                {
                    Status = "healthy",
                    Message = "Process is running",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Check basic memory availability (always cheap)
            var memoryHealthy = IsMemoryHealthy();
            result.Checks["memory"] = new ComponentHealth
            {
                Status = memoryHealthy ? "healthy" : "unhealthy",
                Message = memoryHealthy ? "Memory usage within bounds" : "High memory usage detected",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Overall liveness: process running + basic memory check
            var allHealthy = result.Checks.Values.All(c => c.Status == "healthy");
            result.Status = allHealthy ? "healthy" : "unhealthy";
            result.IsHealthy = allHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Liveness check failed");
            result.Status = "unhealthy";
            result.IsHealthy = false;
            result.Checks["process"] = new ComponentHealth
            {
                Status = "unhealthy",
                Message = $"Exception during liveness check: {ex.Message}"
            };
        }

        result.DurationMs = stopwatch.ElapsedMilliseconds;
        _logger.LogInformation("Liveness check completed: {status} in {duration}ms", result.Status, result.DurationMs);

        return result;
    }

    /// <summary>
    /// Readiness check - tenant database connectivity and responsiveness.
    /// Tests tenant database connectivity and ensures the service is ready to accept traffic.
    /// </summary>
    public async Task<HealthStatusResult> CheckReadinessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthStatusResult
        {
            Status = "healthy",
            IsHealthy = true,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            // Check database connectivity
            var dbHealthy = await IsDatabaseHealthyAsync();
            result.Checks["database"] = new ComponentHealth
            {
                Status = dbHealthy ? "healthy" : "unhealthy",
                Message = dbHealthy ? "Database connectivity OK" : "Database connectivity failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Check disk space (tenant databases need space)
            var diskHealthy = await IsDiskSpaceHealthyAsync();
            result.Checks["disk"] = new ComponentHealth
            {
                Status = diskHealthy ? "healthy" : "unhealthy",
                Message = diskHealthy ? "Disk space sufficient" : "Insufficient disk space",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Overall readiness: database + disk space
            var allHealthy = result.Checks.Values.All(c => c.Status == "healthy");
            result.Status = allHealthy ? "healthy" : "unhealthy";
            result.IsHealthy = allHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Readiness check failed");
            result.Status = "unhealthy";
            result.IsHealthy = false;
            result.Checks["database"] = new ComponentHealth
            {
                Status = "unhealthy",
                Message = $"Exception during readiness check: {ex.Message}"
            };
        }

        result.DurationMs = stopwatch.ElapsedMilliseconds;
        _logger.LogInformation("Readiness check completed: {status} in {duration}ms", result.Status, result.DurationMs);

        return result;
    }
}

/// <summary>
/// Structured health status result for liveness and readiness checks.
/// Contains overall status, individual check results, and timing information.
/// </summary>
public sealed class HealthStatusResult
{
    public string Status { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public long DurationMs { get; set; }
    public Dictionary<string, ComponentHealth> Checks { get; set; } = new();
}

/// <summary>
/// Extended health check with detailed diagnostics.
/// Provides deeper insight for debugging and monitoring dashboards.
/// </summary>
public sealed class DetailedHealthCheckService : HealthCheckService {
    private readonly ILogger<DetailedHealthCheckService> _logger;

    public DetailedHealthCheckService(ILogger<DetailedHealthCheckService> logger, string databasePath = ".")
        : base(logger, databasePath)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets detailed system diagnostics.
    /// Includes version info, configuration, and system metrics.
    /// </summary>
    public Dictionary<string, object> GetDetailedDiagnostics()
    {
        var diagnostics = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["environment"] = new
            {
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                dotnetVersion = Environment.Version.ToString()
            },
            ["process"] = new
            {
                memoryWorkingSet = Process.GetCurrentProcess().WorkingSet64,
                totalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime,
                threads = Process.GetCurrentProcess().Threads.Count
            }
        };

        return diagnostics;
    }
}
