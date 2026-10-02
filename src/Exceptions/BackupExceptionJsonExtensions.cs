#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides JSON serialization and deserialization helpers for <see cref="BackupException"/>.
/// </summary>
public static class BackupExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes a <see cref="BackupException"/> to a JSON string using camelCase
    /// property names for the message, backup ID, and database ID.
    /// </summary>
    /// <param name="exception">The exception to serialize.</param>
    /// <param name="indented">When true, the JSON output is formatted with indentation and newlines.</param>
    /// <returns>The JSON representation of the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string ToJson(this BackupException exception, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var options = new JsonSerializerOptions(SerializerOptions)
        {
            WriteIndented = indented
        };

        var payload = new
        {
            exception.Message,
            exception.BackupId,
            exception.DatabaseId
        };

        return JsonSerializer.Serialize(payload, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="BackupException"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// The deserialized <see cref="BackupException"/>, or null when <paramref name="json"/>
    /// is empty, whitespace, or cannot be parsed as JSON.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static BackupException? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            var backupId = root.TryGetProperty("backupId", out var b) ? b.GetString() : null;
            var databaseId = root.TryGetProperty("databaseId", out var d) ? d.GetString() : null;
            return new BackupException(message, backupId, databaseId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="BackupException"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">
    /// When this method returns true, contains the deserialized exception; otherwise null.
    /// </param>
    /// <returns>True when deserialization succeeds; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out BackupException? result)
    {
        ArgumentNullException.ThrowIfNull(json);

        result = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            result = FromJson(json);
            return result is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
