#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.DataOperations
{
    /// <summary>
    /// Provides System.Text.Json serialization and deserialization extensions for QueryBuilder.
    /// </summary>
    public static class QueryBuilderJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        /// <summary>
        /// Serializes the QueryBuilder instance to a JSON string.
        /// </summary>
        /// <param name="value">The QueryBuilder instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the QueryBuilder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        public static string ToJson(this QueryBuilder value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions)
                {
                    WriteIndented = true
                }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a QueryBuilder instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A QueryBuilder instance populated from the JSON data, or null if the JSON is null or empty.</returns>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static QueryBuilder? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<QueryBuilder>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a QueryBuilder instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized QueryBuilder instance if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryFromJson(string json, out QueryBuilder? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                value = JsonSerializer.Deserialize<QueryBuilder>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
