#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.BackgroundWorkers
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for data retention policy types.
    /// </summary>
    public static class DataRetentionPolicyJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Serializes a <see cref="RetentionPolicyConfig"/> to JSON string.
        /// </summary>
        /// <param name="value">The retention policy configuration to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the policy configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this RetentionPolicyConfig value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="RetentionPolicyConfig"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized retention policy configuration, or null if JSON is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized.</exception>
        public static RetentionPolicyConfig? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<RetentionPolicyConfig>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RetentionPolicyConfig"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized policy configuration if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out RetentionPolicyConfig? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<RetentionPolicyConfig>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Serializes a <see cref="RetentionRule"/> to JSON string.
        /// </summary>
        /// <param name="value">The retention rule to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the retention rule.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this RetentionRule value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="RetentionRule"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized retention rule, or null if JSON is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized.</exception>
        public static RetentionRule? FromJsonToRule(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<RetentionRule>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RetentionRule"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized retention rule if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out RetentionRule? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<RetentionRule>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Serializes a <see cref="RetentionResult"/> to JSON string.
        /// </summary>
        /// <param name="value">The retention result to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the retention result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this RetentionResult value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="RetentionResult"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized retention result, or null if JSON is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized.</exception>
        public static RetentionResult? FromJsonToResult(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<RetentionResult>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RetentionResult"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized retention result if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out RetentionResult? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<RetentionResult>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Serializes a <see cref="RuleExecutionResult"/> to JSON string.
        /// </summary>
        /// <param name="value">The rule execution result to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the rule execution result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this RuleExecutionResult value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="RuleExecutionResult"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized rule execution result, or null if JSON is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized.</exception>
        public static RuleExecutionResult? FromJsonToExecutionResult(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<RuleExecutionResult>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RuleExecutionResult"/> from JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized rule execution result if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out RuleExecutionResult? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<RuleExecutionResult>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}