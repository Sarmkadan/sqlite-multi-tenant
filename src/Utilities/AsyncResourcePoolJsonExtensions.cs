using System;
using System.Text.Json;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// JSON serialization helpers for <see cref="AsyncResourcePool{T}"/>.
    /// </summary>
    public static class AsyncResourcePoolJsonExtensions
    {
        // Cached options: camelCase naming, no indentation by default.
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Do not write indented by default; indentation can be overridden per call.
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="AsyncResourcePool{T}"/> instance to JSON.
        /// </summary>
        /// <typeparam name="T">The type of resource managed by the pool.</typeparam>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">If true, the output JSON will be indented.</param>
        /// <returns>A JSON string representing the instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson<T>(this AsyncResourcePool<T> value, bool indented = false) where T : class
        {
            ArgumentNullException.ThrowIfNull(value);

            // Clone the cached options to avoid mutating the static instance.
            var options = new JsonSerializerOptions(_options)
            {
                WriteIndented = indented
            };

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into an <see cref="AsyncResourcePool{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of resource managed by the pool.</typeparam>
        /// <param name="json">The JSON representation of the pool.</param>
        /// <returns>The deserialized <see cref="AsyncResourcePool{T}"/> instance, or null if the JSON is empty.</returns>
        public static AsyncResourcePool<T>? FromJson<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<AsyncResourcePool<T>>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into an <see cref="AsyncResourcePool{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of resource managed by the pool.</typeparam>
        /// <param name="json">The JSON representation of the pool.</param>
        /// <param name="value">When this method returns, contains the deserialized value if the operation succeeded; otherwise, null.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryFromJson<T>(string json, out AsyncResourcePool<T>? value) where T : class
        {
            try
            {
                value = FromJson<T>(json);
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
