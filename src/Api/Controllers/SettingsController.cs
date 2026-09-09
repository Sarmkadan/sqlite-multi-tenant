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
    private readonly AppConfigManager _configManager;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        AppConfigManager configManager,
        ILogger<SettingsController> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets all application settings.
    /// </summary>
    /// <returns>A dictionary containing all setting key-value pairs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object>>), StatusCodes.Status200OK)]
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
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>The setting value and type information if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SettingValue>), StatusCodes.Status200OK)]
    public IActionResult GetSetting(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
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
    /// <param name="key">The key of the setting to set.</param>
    /// <param name="request">The setting value to apply.</param>
    /// <returns>The updated setting value and type information.</returns>
    [HttpPost("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SettingValue>), StatusCodes.Status200OK)]
    public IActionResult SetSetting(string key, [FromBody] SetSettingRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(request);
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
    /// <param name="settings">A dictionary of setting keys and values to update.</param>
    /// <returns>The result of the batch update operation including counts and any errors.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<BatchSettingUpdateResult>), StatusCodes.Status200OK)]
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
    /// <param name="key">The key of the setting to remove.</param>
    /// <returns>A success message indicating the setting was removed.</returns>
    [HttpDelete("{key}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult RemoveSetting(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
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
    /// <param name="key">The key of the setting to check.</param>
    /// <returns>HTTP 200 if the setting exists; otherwise, HTTP 404.</returns>
    [HttpHead("{key}")]
    public IActionResult CheckSetting(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _logger.LogInformation("Setting existence check: {Key}", key);

        if (_configManager.Contains(key))
            return Ok();

        return NotFound();
    }

    /// <summary>
    /// Gets application information.
    /// </summary>
    /// <returns>Application name, version, start time, uptime, and current timestamp.</returns>
    [HttpGet("app/info")]
    [ProducesResponseType(typeof(ApiResponse<AppInfo>), StatusCodes.Status200OK)]
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

/// <summary>
/// Represents a request to set a configuration value.
/// </summary>
public sealed class SetSettingRequest {
    /// <summary>
    /// The value to set for the configuration key.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents a setting value along with its key and type information.
/// </summary>
public sealed class SettingValue {
    /// <summary>
    /// The key of the setting.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of the setting.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// The .NET type name of the setting value.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a batch settings update operation.
/// </summary>
public sealed class BatchSettingUpdateResult {
    /// <summary>
    /// The number of settings that were successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// The number of settings that failed to update.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// The total number of settings attempted to update.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// A list of error messages for any settings that failed to update.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// The timestamp when the batch update operation completed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents application information including name, version, and runtime statistics.
/// </summary>
public sealed class AppInfo {
    /// <summary>
    /// The name of the application.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The version of the application.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The start time of the application process.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The amount of time the application has been running.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// The timestamp when this information was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}