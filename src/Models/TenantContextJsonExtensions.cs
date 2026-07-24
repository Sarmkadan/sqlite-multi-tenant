#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="TenantContext"/> to convert between JSON and TenantContext objects.
/// </summary>
public static class TenantContextJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        MaxDepth = 100,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new TenantContextJsonConverter() }
    };

    /// <summary>
    /// Serializes a TenantContext to a JSON string
    /// </summary>
    /// <param name="value">The TenantContext to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the TenantContext</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this TenantContext value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a TenantContext from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized TenantContext, or null if JSON is null or empty</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized</exception>
    public static TenantContext? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TenantContext>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a TenantContext from a JSON string
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized TenantContext if successful</param>
    /// <returns>True if deserialization succeeded; otherwise, false</returns>
    public static bool TryFromJson(string json, out TenantContext? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TenantContext>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

/// <summary>
/// Custom JSON converter for <see cref="TenantContext"/> that handles <see cref="Dictionary{string, object}"/> serialization.
/// Handles conversion of dynamic context data with proper type mapping for JSON values.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by JSON serializer")]
internal sealed class TenantContextJsonConverter : JsonConverter<TenantContext>
{
    public override TenantContext Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Cannot deserialize null TenantContext");
        }

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var context = new TenantContext
        {
            TenantId = root.GetProperty("tenantId").GetString() ?? string.Empty,
            TenantName = root.GetProperty("tenantName").GetString(),
            UserId = root.GetProperty("userId").GetString(),
            UserEmail = root.GetProperty("userEmail").GetString(),
            EstablishedAt = root.GetProperty("establishedAt").GetDateTime(),
            CreatedAt = root.GetProperty("createdAt").GetDateTime(),
            RequestId = root.GetProperty("requestId").GetString(),
            ConnectionId = root.GetProperty("connectionId").GetString(),
            DatabasePath = root.GetProperty("databasePath").GetString(),
            IsValid = root.GetProperty("isValid").GetBoolean()
        };

        if (root.TryGetProperty("contextData", out var contextDataElement) &&
            contextDataElement.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in contextDataElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetInt64(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    _ => JsonSerializer.Deserialize<object>(prop.Value.GetRawText(), options)!
                };
            }
            context.ContextData = dict;
        }

        return context;
    }

    public override void Write(
        Utf8JsonWriter writer,
        TenantContext value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();

        writer.WriteString("tenantId", value.TenantId);
        if (!string.IsNullOrEmpty(value.TenantName))
        {
            writer.WriteString("tenantName", value.TenantName);
        }
        if (!string.IsNullOrEmpty(value.UserId))
        {
            writer.WriteString("userId", value.UserId);
        }
        if (!string.IsNullOrEmpty(value.UserEmail))
        {
            writer.WriteString("userEmail", value.UserEmail);
        }

        writer.WriteString("establishedAt", value.EstablishedAt);
        writer.WriteString("createdAt", value.CreatedAt);
        if (!string.IsNullOrEmpty(value.RequestId))
        {
            writer.WriteString("requestId", value.RequestId);
        }
        if (!string.IsNullOrEmpty(value.ConnectionId))
        {
            writer.WriteString("connectionId", value.ConnectionId);
        }
        if (!string.IsNullOrEmpty(value.DatabasePath))
        {
            writer.WriteString("databasePath", value.DatabasePath);
        }
        if (value.ContextData is { Count: > 0 })
        {
            writer.WritePropertyName("contextData");
            JsonSerializer.Serialize(writer, value.ContextData, options);
        }

        writer.WriteBoolean("isValid", value.IsValid);

        writer.WriteEndObject();
    }
}