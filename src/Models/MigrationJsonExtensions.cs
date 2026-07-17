#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="Migration"/>.
/// </summary>
/// <remarks>
/// This class cannot be inherited.
/// </remarks>
public static class MigrationJsonExtensions
{
    /// <summary>
    /// Gets the default JSON serialization options for <see cref="Migration"/> objects.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Gets the JSON serialization options for <see cref="Migration"/> objects with indentation enabled.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes a <see cref="Migration"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The migration to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the migration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this Migration value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="Migration"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="Migration"/> instance, or null if JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized.</exception>
    public static Migration? FromJson(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Migration>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="Migration"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized <see cref="Migration"/>, or null on failure.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out Migration? value)
    {
        value = null;

        return !string.IsNullOrWhiteSpace(json)
            && TryDeserialize(json, out value);
    }

    /// <summary>
    /// Attempts to deserialize JSON with error handling.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">The deserialized result.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    private static bool TryDeserialize(string json, out Migration? result)
    {
        result = null;

        try
        {
            result = JsonSerializer.Deserialize<Migration>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}