#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Provides System.Text.Json serialization/deserialization helpers for the <see cref="StringUtilities"/> type.
/// Enables round-trip serialization of type information for reflection and serialization scenarios.
/// </summary>
public static class StringUtilitiesJsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the <see cref="StringUtilities"/> type to a JSON string representation.
    /// </summary>
    /// <param name="_">Dummy parameter for API consistency (StringUtilities is a static class).</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the StringUtilities type metadata.</returns>
    public static string ToJson(object? _ = null, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(DefaultOptions) { WriteIndented = true }
            : DefaultOptions;

        return JsonSerializer.Serialize(typeof(StringUtilities), options);
    }

    /// <summary>
    /// Deserializes a JSON string to retrieve the <see cref="StringUtilities"/> type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The <see cref="Type"/> object representing StringUtilities if successful; otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var type = JsonSerializer.Deserialize<Type>(json, DefaultOptions);
        return type?.FullName == typeof(StringUtilities).FullName ? type : null;
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to retrieve the <see cref="StringUtilities"/> type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the StringUtilities type if successful, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds and represents the StringUtilities type; otherwise false.</returns>
    public static bool TryFromJson(string json, out Type? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            var type = JsonSerializer.Deserialize<Type>(json, DefaultOptions);
            if (type?.FullName == typeof(StringUtilities).FullName)
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
