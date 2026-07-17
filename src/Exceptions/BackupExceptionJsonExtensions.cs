#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="BackupException"/>
/// </summary>
public static class BackupExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <summary>
    /// Serializes a <see cref="BackupException"/> to a JSON string
    /// </summary>
    /// <param name="value">The exception to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the exception</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this BackupException value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? GetIndentedOptions() : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a <see cref="BackupException"/> from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized exception if successful; otherwise, null</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static BackupException? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonSerializer.Deserialize<BackupException>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BackupException"/> from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized exception if successful</param>
    /// <returns>True if deserialization succeeded; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static bool TryFromJson(string json, out BackupException? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<BackupException>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static JsonSerializerOptions GetIndentedOptions()
    {
        ArgumentNullException.ThrowIfNull(_jsonOptions);
        return new JsonSerializerOptions(_jsonOptions) { WriteIndented = true };
    }
}