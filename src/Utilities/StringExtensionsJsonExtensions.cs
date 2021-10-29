#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Provides System.Text.Json serialization/deserialization helpers for the <see cref="StringExtensions"/> type.
/// Enables round-trip serialization of type information for reflection and serialization scenarios.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="StringExtensions"/> type to a JSON string representation.
    /// </summary>
    /// <param name="_">Dummy parameter for API consistency (StringExtensions is a static class).</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the StringExtensions type metadata.</returns>
    public static string ToJson(object? _ = null, bool indented = false)
    {
        var options = new JsonSerializerOptions(_options)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(typeof(StringExtensions), options);
    }

    /// <summary>
    /// Deserializes a JSON string to retrieve the <see cref="StringExtensions"/> type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The <see cref="Type"/> object representing StringExtensions if successful; otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var type = JsonSerializer.Deserialize<Type>(json, _options);
        return type?.FullName == typeof(StringExtensions).FullName ? type : null;
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to retrieve the <see cref="StringExtensions"/> type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the StringExtensions type if successful, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds and represents the StringExtensions type; otherwise false.</returns>
    public static bool TryFromJson(string json, out Type? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            var type = JsonSerializer.Deserialize<Type>(json, _options);
            if (type?.FullName == typeof(StringExtensions).FullName)
            {
                value = type;
                return true;
            }

            value = null;
            return false;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}