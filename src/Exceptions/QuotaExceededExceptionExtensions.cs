#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="QuotaExceededException"/>.
/// </summary>
public static class QuotaExceededExceptionExtensions
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
    /// Serializes a <see cref="QuotaExceededException"/> instance to a JSON string.
    /// </summary>
    /// <param name="exception">The exception to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string ToJson(this QuotaExceededException exception, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var dto = new
        {
            Message = exception.Message,
            TenantId = exception.TenantId,
            QuotaBytes = exception.QuotaBytes,
            CurrentSizeBytes = exception.CurrentSizeBytes,
            DeltaBytes = exception.DeltaBytes,
        };

        return JsonSerializer.Serialize(dto, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="QuotaExceededException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized exception, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static QuotaExceededException? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var dto = JsonSerializer.Deserialize<ExceptionDto>(json, _jsonOptions);
        if (dto is null)
        {
            return null;
        }

        return new QuotaExceededException(
            dto.Message ?? "Quota exceeded",
            dto.TenantId ?? "unknown",
            dto.QuotaBytes,
            dto.CurrentSizeBytes,
            dto.DeltaBytes);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="QuotaExceededException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="exception">Receives the deserialized exception if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out QuotaExceededException? exception)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            exception = FromJson(json);
            return exception is not null;
        }
        catch (JsonException)
        {
            exception = null;
            return false;
        }
    }

    private sealed class ExceptionDto
    {
        public string? Message { get; set; }
        public string? TenantId { get; set; }
        public long QuotaBytes { get; set; }
        public long CurrentSizeBytes { get; set; }
        public long DeltaBytes { get; set; }
    }
}
