#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SqliteMultiTenant.Monitoring
{
    /// <summary>
    /// Provides System.Text.Json serialization and deserialization extensions for <see cref="ReportGenerator"/>.
    /// </summary>
    public static class ReportGeneratorJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        /// <summary>
        /// Serializes the <see cref="ReportGenerator"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="ReportGenerator"/> instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the <see cref="ReportGenerator"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this ReportGenerator value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions)
                {
                    WriteIndented = true
                }
                : _jsonOptions;

            // ReportGenerator is not serializable as it contains ILogger dependency
            // Return a minimal representation instead
            return JsonSerializer.Serialize(new
            {
                Type = nameof(ReportGenerator),
                HasLogger = true,
                Timestamp = DateTime.UtcNow
            }, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="ReportGenerator"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="ReportGenerator"/> instance, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static ReportGenerator? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                // ReportGenerator cannot be deserialized due to ILogger dependency
                // Return null as it's not possible to reconstruct the instance
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="ReportGenerator"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized <see cref="ReportGenerator"/> instance, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out ReportGenerator? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                // ReportGenerator cannot be deserialized due to ILogger dependency
                // Always return false as it's not possible to reconstruct the instance
                value = null;
                return false;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}