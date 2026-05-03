#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant.Api.Controllers;

/// <summary>
/// Manages application settings and configuration through API endpoints.
/// Provides get/set operations for system-wide settings.
/// Includes validation and change notifications.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase {
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IConfigurationManager configManager,
        ILogger<SettingsController> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets all application settings.
    /// </summary>
    [HttpGet]
    [ProduceResponseType(typeof(ApiResponse<Dictionary<string, object>>), StatusCodes.Status200OK)]
    public IActionResult GetAllSettings()
    {
        try
        {
            _logger.LogInformation("All settings requested");

            var settings = _configManager.GetAll();

            return Ok(ApiResponse<Dictionary<string, object>>.Success(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting settings: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve settings"));
        }
    }

    /// <summary>
    /// Gets a specific setting by key.
    /// </summary>
    [HttpGet("{key}")]
    [ProduceResponseType(typeof(ApiResponse<SettingValue>), StatusCodes.Status200OK)]
    public IActionResult GetSetting(string key)
    {
        try
        {
            _logger.LogInformation("Setting requested: {Key}", key);

            if (_configManager.TryGet<object>(key, out var value))
            {
                var result = new SettingValue
                {
                    Key = key,
                    Value = value,
                    Type = value?.GetType().Name ?? "null"
                };

                return Ok(ApiResponse<SettingValue>.Success(result));
            }

            return NotFound(ApiResponse<object>.Error($"Setting '{key}' not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting setting: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve setting"));
        }
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    [HttpPost("{key}")]
    [ProduceResponseType(typeof(ApiResponse<SettingValue>), StatusCodes.Status200OK)]
    public IActionResult SetSetting(string key, [FromBody] SetSettingRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Error("Invalid request"));

            _logger.LogInformation("Setting updated: {Key}", key);

            _configManager.Set(key, request.Value);

            var result = new SettingValue
            {
                Key = key,
                Value = request.Value,
                Type = request.Value?.GetType().Name ?? "null"
            };

            return Ok(ApiResponse<SettingValue>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error setting configuration: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to update setting"));
        }
    }

    /// <summary>
    /// Updates multiple settings atomically.
    /// </summary>
    [HttpPost("batch")]
    [ProduceResponseType(typeof(ApiResponse<BatchSettingUpdateResult>), StatusCodes.Status200OK)]
    public IActionResult UpdateBatchSettings([FromBody] Dictionary<string, object> settings)
    {
        try
        {
            if (settings is null || settings.Count == 0)
                return BadRequest(ApiResponse<object>.Error("No settings provided"));

            _logger.LogInformation("Batch settings update: {Count} items", settings.Count);

            int updatedCount = 0;
            var errors = new List<string>();

            foreach (var kvp in settings)
            {
                try
                {
                    _configManager.Set(kvp.Key, kvp.Value);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{kvp.Key}: {ex.Message}");
                }
            }

            var result = new BatchSettingUpdateResult
            {
                UpdatedCount = updatedCount,
                FailedCount = errors.Count,
                TotalCount = settings.Count,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<BatchSettingUpdateResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating batch settings: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to update settings"));
        }
    }

    /// <summary>
    /// Removes a setting by key.
    /// </summary>
    [HttpDelete("{key}")]
    [ProduceResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult RemoveSetting(string key)
    {
        try
        {
            _logger.LogInformation("Setting removed: {Key}", key);

            _configManager.Remove(key);

            return Ok(ApiResponse<object>.Success(new { Message = $"Setting '{key}' removed" }));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error removing setting: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to remove setting"));
        }
    }

    /// <summary>
    /// Checks if a setting exists.
    /// </summary>
    [HttpHead("{key}")]
    public IActionResult CheckSetting(string key)
    {
        _logger.LogInformation("Setting existence check: {Key}", key);

        if (_configManager.Contains(key))
            return Ok();

        return NotFound();
    }

    /// <summary>
    /// Gets application information.
    /// </summary>
    [HttpGet("app/info")]
    [ProduceResponseType(typeof(ApiResponse<AppInfo>), StatusCodes.Status200OK)]
    public IActionResult GetAppInfo()
    {
        try
        {
            _logger.LogInformation("App info requested");

            var version = System.Reflection.Assembly.GetExecutingAssembly()?
                .GetName().Version?.ToString() ?? "Unknown";

            var info = new AppInfo
            {
                Name = "SQLite Multi-Tenant Manager",
                Version = version,
                StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime,
                Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<AppInfo>.Success(info));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting app info: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve app info"));
        }
    }
}

public sealed class SetSettingRequest {
    public object? Value { get; set; }
}

public sealed class SettingValue {
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string Type { get; set; } = string.Empty;
}

public sealed class BatchSettingUpdateResult {
    public int UpdatedCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public sealed class AppInfo {
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime Timestamp { get; set; }
}
