#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SqliteMultiTenant.Monitoring
{
    /// <summary>
    /// Provides System.Text.Json serialization and deserialization extensions for report data structures
    /// used by <see cref="ReportGenerator"/>.
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
        /// Serializes a <see cref="SystemHealthSummary"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="SystemHealthSummary"/> instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the <see cref="SystemHealthSummary"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this SystemHealthSummary value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Serializes a collection of <see cref="OperationStatistics"/> instances to a JSON string.
        /// </summary>
        /// <param name="value">The collection of <see cref="OperationStatistics"/> instances to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this System.Collections.Generic.IEnumerable<OperationStatistics> value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Serializes a collection of <see cref="PerformanceMetric"/> instances to a JSON string.
        /// </summary>
        /// <param name="value">The collection of <see cref="PerformanceMetric"/> instances to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this System.Collections.Generic.IEnumerable<PerformanceMetric> value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="SystemHealthSummary"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="SystemHealthSummary"/> instance, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static SystemHealthSummary? FromJsonToHealthSummary(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<SystemHealthSummary>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string to a collection of <see cref="OperationStatistics"/> instances.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A collection of <see cref="OperationStatistics"/> instances, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static System.Collections.Generic.IEnumerable<OperationStatistics>? FromJsonToOperationStatistics(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<System.Collections.Generic.List<OperationStatistics>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string to a collection of <see cref="PerformanceMetric"/> instances.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A collection of <see cref="PerformanceMetric"/> instances, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static System.Collections.Generic.IEnumerable<PerformanceMetric>? FromJsonToPerformanceMetrics(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<System.Collections.Generic.List<PerformanceMetric>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="SystemHealthSummary"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized <see cref="SystemHealthSummary"/> instance, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(this string json, out SystemHealthSummary? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<SystemHealthSummary>(json, _jsonOptions);
                return value is not null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a collection of <see cref="OperationStatistics"/> instances.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized collection of <see cref="OperationStatistics"/> instances, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(this string json, out System.Collections.Generic.IEnumerable<OperationStatistics>? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                var result = JsonSerializer.Deserialize<System.Collections.Generic.List<OperationStatistics>>(json, _jsonOptions);
                value = result;
                return value is not null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a collection of <see cref="PerformanceMetric"/> instances.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized collection of <see cref="PerformanceMetric"/> instances, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(this string json, out System.Collections.Generic.IEnumerable<PerformanceMetric>? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                var result = JsonSerializer.Deserialize<System.Collections.Generic.List<PerformanceMetric>>(json, _jsonOptions);
                value = result;
                return value is not null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}