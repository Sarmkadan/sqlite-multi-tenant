// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Formatters;

/// <summary>
/// Formats objects as JSON with customizable serialization options.
/// Supports pretty-printing, null handling, and circular reference detection.
/// </summary>
public class JsonFormatter
{
    private readonly JsonSerializerOptions _options;
    private readonly ILogger<JsonFormatter> _logger;

    public JsonFormatter(ILogger<JsonFormatter> logger, bool prettyPrint = true)
    {
        _logger = logger;
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }

    /// <summary>
    /// Formats an object as JSON string.
    /// Handles null values, enums, and nested objects.
    /// </summary>
    public string Format<T>(T? data) where T : class
    {
        try
        {
            if (data == null)
                return "null";

            return JsonSerializer.Serialize(data, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError($"JSON formatting error: {ex.Message}");
            return JsonSerializer.Serialize(
                new { error = "Serialization failed", message = ex.Message },
                _options);
        }
    }

    /// <summary>
    /// Parses a JSON string back into an object.
    /// </summary>
    public T? Parse<T>(string json) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError($"JSON parsing error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Formats with custom JSON options for specific use cases.
    /// </summary>
    public string FormatWithOptions<T>(T? data, JsonSerializerOptions options) where T : class
    {
        try
        {
            if (data == null)
                return "null";

            return JsonSerializer.Serialize(data, options);
        }
        catch (Exception ex)
        {
            _logger.LogError($"JSON formatting error: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates options for minimal JSON output (no pretty printing).
    /// </summary>
    public static JsonSerializerOptions GetMinimalOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Creates options for verbose JSON output with all properties.
    /// </summary>
    public static JsonSerializerOptions GetVerboseOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }
}
