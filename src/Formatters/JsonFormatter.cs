#nullable enable
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
public sealed class JsonExportFormatter {
    private readonly JsonSerializerOptions _options;
    private readonly ILogger<JsonExportFormatter> _logger;

    /// <summary>
/// Initializes a new instance of the <see cref="JsonExportFormatter"/> class.
/// </summary>
/// <param name="logger">The logger used for error reporting.</param>
/// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
public JsonExportFormatter(ILogger<JsonExportFormatter> logger, bool prettyPrint = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
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
            if (data is null)
                return "null";

            return JsonSerializer.Serialize(data, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError("JSON formatting error: {Message}", ex.Message);
            return JsonSerializer.Serialize(
                new { error = "Serialization failed", message = ex.Message },
                _options);
        }
    }

    /// <summary>
    /// Parses a JSON string back into an object.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The deserialized object, or null if parsing fails or input is null/whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public T? Parse<T>(string json) where T : class
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError("JSON parsing error: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Formats with custom JSON options for specific use cases.
    /// </summary>
    /// <param name="data">The object to format as JSON.</param>
    /// <param name="options">The JSON serializer options to use.</param>
    /// <returns>The formatted JSON string, or "null" if data is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public string FormatWithOptions<T>(T? data, JsonSerializerOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(options);
        try
        {
            if (data is null)
                return "null";

            return JsonSerializer.Serialize(data, options);
        }
        catch (Exception ex)
        {
            _logger.LogError("JSON formatting error: {Message}", ex.Message);
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
