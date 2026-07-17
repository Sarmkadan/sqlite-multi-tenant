#nullable enable

using System;
using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// System.Text.Json serialization helpers for DateTime values with DateTimeExtensions-specific formatting.
/// All methods preserve UTC semantics and use ISO 8601 formatting for consistency.
/// </summary>
public static class DateTimeExtensionsJsonExtensions
{
    // Cached options: camelCase naming, no indentation by default.
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Do not write indented by default; indentation can be overridden per call.
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a DateTime value to JSON using ISO 8601 format.
    /// The DateTime is converted to UTC before serialization to ensure consistency.
    /// </summary>
    /// <param name="value">The DateTime value to serialize.</param>
    /// <param name="indented">If true, the output JSON will be indented.</param>
    /// <returns>A JSON string representing the DateTime value.</returns>
    public static string ToJson(this DateTime value, bool indented = false)
    {
        // Clone the cached options to avoid mutating the static instance.
        var options = new JsonSerializerOptions(_options)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a DateTime value.
    /// The JSON is expected to contain an ISO 8601 formatted DateTime string.
    /// </summary>
    /// <param name="json">The JSON string containing the DateTime value.</param>
    /// <returns>The deserialized DateTime value, or null if the JSON is empty or invalid.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or whitespace.</exception>
    /// <exception cref="JsonException">Thrown if the JSON cannot be deserialized as a DateTime.</exception>
    public static DateTime? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<DateTime>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a DateTime value.
    /// The JSON is expected to contain an ISO 8601 formatted DateTime string.
    /// </summary>
    /// <param name="json">The JSON string containing the DateTime value.</param>
    /// <param name="value">When this method returns, contains the deserialized value if the operation succeeded; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or whitespace.</exception>
    public static bool TryFromJson(string json, out DateTime? value)
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
