#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="TenantStorageInfo"/>.
/// </summary>
public static class TenantStorageInfoJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes a <see cref="TenantStorageInfo"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The tenant storage information to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the tenant storage information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this TenantStorageInfo value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="TenantStorageInfo"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized tenant storage information, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static TenantStorageInfo? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<TenantStorageInfo>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="TenantStorageInfo"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized tenant storage information if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out TenantStorageInfo? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<TenantStorageInfo>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}