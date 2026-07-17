#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;

namespace SqliteMultiTenant.Security
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="EncryptionKeyManager"/>.
    /// </summary>
    public static class EncryptionKeyManagerJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes an <see cref="EncryptionKeyManager"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="EncryptionKeyManager"/> instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the <see cref="EncryptionKeyManager"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this EncryptionKeyManager value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes an <see cref="EncryptionKeyManager"/> instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An <see cref="EncryptionKeyManager"/> instance, or <c>null</c> if the JSON is empty or whitespace.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
 /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static EncryptionKeyManager? FromJson(string json)
        {
	ArgumentNullException.ThrowIfNull(json);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<EncryptionKeyManager>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize an <see cref="EncryptionKeyManager"/> instance from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized <see cref="EncryptionKeyManager"/> instance, or <c>null</c> on failure.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryFromJson(string json, out EncryptionKeyManager? value)
        {
	ArgumentNullException.ThrowIfNull(json);

            value = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<EncryptionKeyManager>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}