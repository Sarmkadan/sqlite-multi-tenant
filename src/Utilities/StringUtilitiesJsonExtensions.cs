#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for string operations.
/// Includes utilities for hashing, case conversion, sanitization, and validation.
/// </summary>
public static class StringUtilitiesJsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a string to JSON with optional sanitization for safe HTML output.
    /// </summary>
    /// <param name="value">The string value to serialize.</param>
    /// <param name="sanitizeForHtml">Whether to sanitize the string for safe HTML output.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the input value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this string value, bool sanitizeForHtml = false, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var sanitized = sanitizeForHtml
            ? StringUtilities.SanitizeForHtml(value)
            : value;

        var options = indented
            ? new JsonSerializerOptions(DefaultOptions) { WriteIndented = true }
            : DefaultOptions;

        return JsonSerializer.Serialize(sanitized, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a string value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized string value, or null if the JSON is null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to a string.</exception>
    public static string? FromJson(this string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<string>(json, DefaultOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a string value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized string value if successful.</param>
    /// <returns>True if deserialization succeeds; otherwise false.</returns>
    public static bool TryFromJson(this string json, out string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<string>(json, DefaultOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a string to JSON with SHA256 hash representation.
    /// </summary>
    /// <param name="value">The string value to serialize with hash.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON object containing both the original value and its SHA256 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJsonWithHash(this string value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var hash = StringUtilities.ComputeSha256Hash(value);
        var data = new { Value = value, Hash = hash };

        var options = indented
            ? new JsonSerializerOptions(DefaultOptions) { WriteIndented = true }
            : DefaultOptions;

        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Serializes a string to JSON with case conversion to snake_case.
    /// </summary>
    /// <param name="value">The string value to serialize with case conversion.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON object containing both the original value and its snake_case conversion.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJsonWithSnakeCase(this string value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var snakeCase = StringUtilities.ToSnakeCase(value);
        var data = new { Original = value, SnakeCase = snakeCase };

        var options = indented
            ? new JsonSerializerOptions(DefaultOptions) { WriteIndented = true }
            : DefaultOptions;

        return JsonSerializer.Serialize(data, options);
    }
}