#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for Backup
/// </summary>
public static class BackupModelTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a Backup instance to JSON string
    /// </summary>
    /// <param name="value">The Backup instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation of the Backup</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this Backup value, bool indented = false) =>
        ToJson(value, indented, _jsonOptions);

    /// <summary>
    /// Deserializes a Backup instance from JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized Backup instance, or null if JSON is invalid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty</exception>
    public static Backup? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<Backup>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a Backup instance from JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized Backup</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public static bool TryFromJson(string json, out Backup? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<Backup>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a Backup instance to JSON string with custom options
    /// </summary>
    /// <param name="value">The Backup instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <param name="options">Custom JSON serialization options</param>
    /// <returns>JSON string representation of the Backup</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="options"/> is null</exception>
    private static string ToJson(Backup value, bool indented, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        var actualOptions = indented
            ? new JsonSerializerOptions(options) { WriteIndented = true }
            : options;

        return JsonSerializer.Serialize(value, actualOptions);
    }
}