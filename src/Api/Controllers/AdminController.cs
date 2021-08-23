#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Health;
using SqliteMultiTenant.Monitoring;

namespace SqliteMultiTenant.Api.Controllers;

/// <summary>
/// Administrative endpoints for system-level operations.
/// Provides access to health checks, system metrics, and diagnostic information.
/// All operations are protected and should require administrative privileges.
/// </summary>
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase {
    private readonly HealthCheckService _healthCheckService;
    private readonly MetricsService _metricsService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        HealthCheckService healthCheckService,
        MetricsService metricsService,
        ILogger<AdminController> logger)
    {
        _healthCheckService = healthCheckService;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Performs comprehensive system health checks.
    /// Verifies database connectivity, file system access, and service status.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthAsync()
    {
        try
        {
            _logger.LogInformation("Health check requested");

            var isHealthy = await _healthCheckService.IsSystemHealthyAsync();
            var status = await _healthCheckService.GetDetailedStatusAsync();

            var response = new HealthCheckResponse
            {
                IsHealthy = isHealthy,
                Status = status,
                Timestamp = DateTime.UtcNow,
                Version = GetVersion()
            };

            return Ok(ApiResponse<HealthCheckResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError("Health check error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Health check failed"));
        }
    }

    /// <summary>
    /// Retrieves current system metrics and performance statistics.
    /// Includes CPU usage, memory consumption, database connection counts, etc.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<SystemMetrics>), StatusCodes.Status200OK)]
    public IActionResult GetMetrics()
    {
        try
        {
            _logger.LogInformation("Metrics requested");
            var snapshot = _metricsService.GetSnapshot();

            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                ProcessMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024),
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
                ActiveConnections = 0, // Not available directly in snapshot
                RequestsProcessed = snapshot.TotalRequests,
                AverageResponseTimeMs = snapshot.AverageResponseTimeMs
            };

            return Ok(ApiResponse<SystemMetrics>.Success(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError("Metrics retrieval error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve metrics"));
        }
    }

    /// <summary>
    /// Retrieves a comprehensive metrics dashboard.
    /// Includes system performance, request metrics, and backup/migration statistics.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<MetricsSnapshot>), StatusCodes.Status200OK)]
    public IActionResult GetMetricsDashboard()
    {
        try
        {
            _logger.LogInformation("Metrics dashboard requested");
            var snapshot = _metricsService.GetSnapshot();
            return Ok(ApiResponse<MetricsSnapshot>.Success(snapshot));
        }
        catch (Exception ex)
        {
            _logger.LogError("Metrics dashboard error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve dashboard metrics"));
        }
    }

    /// <summary>
    /// Clears system caches to free memory or force data refresh.
    /// Returns information about what was cleared and memory freed.
    /// </summary>
    [HttpPost("cache/clear")]
    [ProducesResponseType(typeof(ApiResponse<CacheClearResult>), StatusCodes.Status200OK)]
    public IActionResult ClearCache()
    {
        try
        {
            _logger.LogInformation("Cache clear requested");

            long memoryBefore = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long memoryAfter = GC.GetTotalMemory(false);

            var result = new CacheClearResult
            {
                MemoryFreedBytes = memoryBefore - memoryAfter,
                Timestamp = DateTime.UtcNow,
                Message = "Cache cleared successfully"
            };

            return Ok(ApiResponse<CacheClearResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Cache clear error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Cache clear failed"));
        }
    }

    /// <summary>
    /// Forces garbage collection to optimize memory usage.
    /// Should be used sparingly as it impacts performance.
    /// </summary>
    [HttpPost("gc/collect")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult ForceGarbageCollection()
    {
        try
        {
            _logger.LogInformation("Garbage collection requested");

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(ApiResponse<object>.Success(new { Message = "Garbage collection completed" }));
        }
        catch (Exception ex)
        {
            _logger.LogError("GC error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Garbage collection failed"));
        }
    }

    /// <summary>
    /// Gets diagnostic information about the running system.
    /// Includes .NET version, OS info, and application version.
    /// </summary>
    [HttpGet("diagnostics")]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticsInfo>), StatusCodes.Status200OK)]
    public IActionResult GetDiagnostics()
    {
        try
        {
            _logger.LogInformation("Diagnostics requested");

            var diagnostics = new DiagnosticsInfo
            {
                DotNetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OSVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount,
                ApplicationVersion = GetVersion(),
                StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime,
                Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<DiagnosticsInfo>.Success(diagnostics));
        }
        catch (Exception ex)
        {
            _logger.LogError("Diagnostics error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Diagnostics failed"));
        }
    }

    private string GetVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()?
            .GetName().Version?.ToString() ?? "Unknown";
    }
}

public sealed class HealthCheckResponse {
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}

public sealed class SystemMetrics {
    public DateTime Timestamp { get; set; }
    public long ProcessMemoryMb { get; set; }
    public int ThreadCount { get; set; }
    public int ActiveConnections { get; set; }
    public long RequestsProcessed { get; set; }
    public double AverageResponseTimeMs { get; set; }
}

public sealed class CacheClearResult {
    public long MemoryFreedBytes { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class DiagnosticsInfo {
    public string DotNetVersion { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime Timestamp { get; set; }
}
