#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Provides System.Text.Json serialization/deserialization helpers for string values.
/// All methods preserve culture-invariant behavior and handle null/empty inputs gracefully.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    // Cached options: camelCase naming, no indentation by default.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Do not write indented by default; indentation can be overridden per call.
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a string value to JSON.
    /// </summary>
    /// <param name="value">The string value to serialize.</param>
    /// <param name="indented">If true, the output JSON will be indented.</param>
    /// <returns>A JSON string representing the string value, or null if the input is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static string? ToJson(this string? value, bool indented = false)
    {
        if (value is null)
        {
            return null;
        }

        var options = new JsonSerializerOptions(_options)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a string value.
    /// </summary>
    /// <param name="json">The JSON string containing the string value.</param>
    /// <returns>The deserialized string value, or null if the JSON is empty or represents null.</returns>
    /// <exception cref="ArgumentException">Thrown if json is null or whitespace.</exception>
    public static string? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<string>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a string value.
    /// </summary>
    /// <param name="json">The JSON string containing the string value.</param>
    /// <param name="value">When this method returns, contains the deserialized value if the operation succeeded; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out string? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}