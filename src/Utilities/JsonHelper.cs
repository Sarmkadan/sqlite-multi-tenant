#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Helper methods for JSON serialization and deserialization.
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions CompactOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Serializes an object to formatted JSON.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="indented">Whether to use indented JSON.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string Serialize<T>(T obj, bool indented = true)
        {
            try
            {
                var options = indented ? DefaultOptions : CompactOptions;
                return JsonSerializer.Serialize(obj, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to serialize object to JSON", ex);
            }
        }

        /// <summary>
        /// Deserializes JSON to an object.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be empty", nameof(json));

            try
            {
                return JsonSerializer.Deserialize<T>(json, DefaultOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize JSON", ex);
            }
        }

        /// <summary>
        /// Deserializes JSON to a dynamic object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized dynamic object.</returns>
        public static dynamic DeserializeDynamic(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse JSON", ex);
            }
        }

        /// <summary>
        /// Merges two JSON objects (shallow merge).
        /// </summary>
        /// <param name="json1">The first JSON string to merge.</param>
        /// <param name="json2">The second JSON string to merge.</param>
        /// <returns>The merged JSON string.</returns>
        public static string MergeJson(string json1, string json2)
        {
            try
            {
                using var doc1 = JsonDocument.Parse(json1);
                using var doc2 = JsonDocument.Parse(json2);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var merged = MergeElements(doc1.RootElement, doc2.RootElement);
                return JsonSerializer.Serialize(merged, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to merge JSON objects", ex);
            }
        }

        /// <summary>
        /// Extracts a property from JSON.
        /// </summary>
        /// <typeparam name="T">The type of the property to extract.</typeparam>
        /// <param name="json">The JSON string to extract from.</param>
        /// <param name="propertyPath">The path to the property to extract.</param>
        /// <returns>The extracted property value.</returns>
        public static T GetProperty<T>(string json, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var element = TraversePath(doc.RootElement, propertyPath);

                if (element.ValueKind == JsonValueKind.Undefined)
                    return default;

                if (typeof(T) == typeof(string))
                    return (T)(object)element.GetString();

                return JsonSerializer.Deserialize<T>(element.GetRawText(), DefaultOptions);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Validates JSON format.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>True if the JSON is valid, false otherwise.</returns>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clones an object by serializing and deserializing.
        /// </summary>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>The cloned object.</returns>
        public static T DeepClone<T>(T obj)
        {
            var json = Serialize(obj);
            return Deserialize<T>(json);
        }

        /// <summary>
        /// Pretty prints JSON.
        /// </summary>
        /// <param name="json">The JSON string to pretty print.</param>
        /// <returns>The pretty printed JSON string.</returns>
        public static string PrettyPrint(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Serialize(doc.RootElement, options);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Minifies JSON.
        /// </summary>
        /// <param name="json">The JSON string to minify.</param>
        /// <returns>The minified JSON string.</returns>
        public static string Minify(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Serialize(doc.RootElement, options);
            }
            catch
            {
                return json;
            }
        }

        private static JsonElement MergeElements(JsonElement element1, JsonElement element2)
        {
            if (element1.ValueKind == JsonValueKind.Object &&
                element2.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object>();

                foreach (var prop in element1.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value;
                }

                foreach (var prop in element2.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value;
                }

                return JsonSerializer.SerializeToElement(dict);
            }

            return element2;
        }

        private static JsonElement TraversePath(JsonElement element, string path)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty(part, out var property))
                    {
                        element = property;
                    }
                    else
                    {
                        return default;
                    }
                }
                else
                {
                    return default;
                }
            }

            return element;
        }
    }
}
