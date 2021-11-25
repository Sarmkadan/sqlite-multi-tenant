using System;
using System.Text.Json;

namespace SqliteMultiTenant.Utilities
{
	/// <summary>
	/// JSON serialization helpers for <see cref="DatabaseUtilities"/>.
	/// </summary>
	public static class DatabaseUtilitiesJsonExtensions
	{
		// Cached options: camelCase naming, no indentation by default.
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			// Do not write indented by default; indentation can be overridden per call.
			WriteIndented = false
		};

		/// <summary>
		/// Serializes the <see cref="DatabaseUtilities"/> instance to JSON.
		/// </summary>
		/// <param name="value">The instance to serialize.</param>
		/// <param name="indented">If true, the output JSON will be indented.</param>
		/// <returns>A JSON string representing the instance.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
		public static string ToJson(this DatabaseUtilities value, bool indented = false)
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
		/// Deserializes a JSON string into a <see cref="DatabaseUtilities"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of the utilities.</param>
		/// <returns>The deserialized <see cref="DatabaseUtilities"/> instance, or <see langword="null"/> if the JSON is null or whitespace.</returns>
		/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
		public static DatabaseUtilities? FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			return JsonSerializer.Deserialize<DatabaseUtilities>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into a <see cref="DatabaseUtilities"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of the utilities.</param>
		/// <param name="value">When this method returns, contains the deserialized value if the operation succeeded; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
		public static bool TryFromJson(string json, out DatabaseUtilities? value)
		{
			try
			{
				value = FromJson(json);
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