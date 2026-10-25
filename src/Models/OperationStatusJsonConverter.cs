#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =========================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Models;

/// <summary>
/// JSON converter for <see cref="OperationStatus"/> enum.
/// </summary>
public sealed class OperationStatusJsonConverter : JsonConverter<OperationStatus>
{
    /// <summary>
    /// Reads and converts the JSON to/from <see cref="OperationStatus"/>.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeToConvert"/> is null.</exception>
    public override OperationStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        var value = reader.GetString();

        return value?.ToLowerInvariant() switch
        {
            "pending" => OperationStatus.Pending,
            "running" => OperationStatus.Running,
            "succeeded" => OperationStatus.Succeeded,
            "failed" => OperationStatus.Failed,
            _ => throw new JsonException($"Unknown OperationStatus value: {value}")
        };
    }

    /// <summary>
    /// Writes the value to JSON.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">Serializer options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is null.</exception>
    public override void Write(
        Utf8JsonWriter writer,
        OperationStatus value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
