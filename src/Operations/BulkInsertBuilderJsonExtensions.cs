#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Operations
{
	/// <summary>
	/// Provides System.Text.Json serialization extensions for <see cref="BulkInsertBuilder"/>.
	/// </summary>
	public static class BulkInsertBuilderJsonExtensions
	{
		private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false,
			ReferenceHandler = ReferenceHandler.IgnoreCycles,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		/// <summary>
		/// Serializes the <see cref="BulkInsertBuilder"/> to a JSON string.
		/// </summary>
		/// <param name="value">The builder instance to serialize.</param>
		/// <param name="indented">Whether to format the JSON with indentation.</param>
		/// <returns>A JSON string representation of the builder.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
		public static string ToJson(this BulkInsertBuilder value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			var options = indented
				? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
				: _jsonOptions;

			return JsonSerializer.Serialize(value, options);
		}

		/// <summary>
		/// Deserializes a JSON string to a <see cref="BulkInsertBuilder"/> instance.
		/// </summary>
		/// <param name="json">The JSON string to deserialize.</param>
		/// <returns>A <see cref="BulkInsertBuilder"/> instance if deserialization succeeds; otherwise, null.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
		/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
		public static BulkInsertBuilder? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			return JsonSerializer.Deserialize<BulkInsertBuilder>(json, _jsonOptions);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string to a <see cref="BulkInsertBuilder"/> instance.
		/// </summary>
		/// <param name="json">The JSON string to deserialize.</param>
		/// <param name="value">Receives the deserialized instance, or null on failure.</param>
		/// <returns>True if deserialization succeeded; otherwise, false.</returns>
		public static bool TryFromJson(string json, out BulkInsertBuilder? value)
		{
			value = null;

			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}

			try
			{
				value = JsonSerializer.Deserialize<BulkInsertBuilder>(json, _jsonOptions);
				return true;
			}
			catch (JsonException)
			{
				return false;
			}
		}
	}
}