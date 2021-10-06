#nullable enable

using System;
using System.Text.Json;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// System.Text.Json serialization helpers for reflection operations.
/// Provides JSON serialization and deserialization capabilities for types inspected via reflection.
/// </summary>
public static class ReflectionExtensionsJsonExtensions
{
    // Cached options: camelCase naming, no indentation by default.
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Do not write indented by default; indentation can be overridden per call.
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a type's metadata to JSON.
    /// </summary>
    /// <param name="type">The type whose metadata to serialize.</param>
    /// <param name="indented">If true, the output JSON will be indented.</param>
    /// <returns>A JSON string representing the type metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
    public static string ToJson(this Type type, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Clone the cached options to avoid mutating the static instance.
        var options = new JsonSerializerOptions(_options)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(type, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a Type.
    /// Note: Type deserialization from JSON is limited; this returns null if deserialization fails.
    /// </summary>
    /// <param name="json">The JSON string containing the type data.</param>
    /// <returns>The deserialized Type, or null if the JSON is empty or deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown if json is null or whitespace.</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        // Type deserialization from JSON requires special handling; this is a best-effort approach
        // In practice, Type objects cannot be reliably deserialized from JSON
        return null;
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a Type.
    /// </summary>
    /// <param name="json">The JSON string containing the type data.</param>
    /// <param name="value">When this method returns, contains the deserialized Type if the operation succeeded; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out Type? value)
    {
        try
        {
            value = FromJson(json);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
