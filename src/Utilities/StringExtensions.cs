#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for string operations commonly used across the application.
/// Focuses on validation, transformation, and parsing for database and tenant contexts.
/// All methods handle null/empty inputs gracefully without throwing exceptions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts string to safe database identifier (snake_case with alphanumeric only).
    /// Removes special characters to prevent SQL injection vectors.
    /// Used for dynamic table/schema names derived from tenant data.
    /// </summary>
    /// <param name="value">The string to convert to a safe database identifier.</param>
    /// <returns>A safe database identifier string, or empty string if input is null or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToSafeDatabaseIdentifier(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Convert to lowercase and replace non-alphanumeric with underscore
        var safe = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "_");

        // Remove leading/trailing underscores
        safe = safe.Trim('_');

        // Ensure it doesn't start with a number (invalid in SQL)
        if (safe.Length > 0 && char.IsDigit(safe[0]))
            safe = "_" + safe;

        return safe;
    }

    /// <summary>
    /// Safely truncates string to max length with ellipsis indicator.
    /// Preserves word boundaries when possible to avoid cutting mid-word.
    /// Useful for UI display and log message limiting.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">Maximum length of the resulting string.</param>
    /// <param name="addEllipsis">Whether to add ellipsis (...) when truncating.</param>
    /// <returns>The truncated string, or original if shorter than maxLength.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxLength is negative.</exception>
    public static string SafeTruncate(this string value, int maxLength, bool addEllipsis = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        var truncated = value[..maxLength];
        if (addEllipsis)
            truncated = truncated[..Math.Max(0, maxLength - 3)] + "...";

        return truncated;
    }

    /// <summary>
    /// Validates string is a valid tenant identifier (UUID or slug format).
    /// Accepts both v4 UUIDs and alphanumeric slugs for flexibility.
    /// </summary>
    /// <param name="value">The string to validate as a tenant identifier.</param>
    /// <returns>True if valid tenant identifier, false otherwise.</returns>
    public static bool IsValidTenantIdentifier(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Check for UUID format
        if (Guid.TryParse(value, out _))
            return true;

        // Check for slug format (alphanumeric + hyphen)
        return Regex.IsMatch(value, @"^[a-z0-9\-]+$", RegexOptions.IgnoreCase) && value.Length <= 100;
    }

    /// <summary>
    /// Converts string to enum value with safe fallback.
    /// Case-insensitive parsing for user-friendly input.
    /// Returns default if parsing fails instead of throwing exception.
    /// </summary>
    /// <typeparam name="T">The enum type to convert to.</typeparam>
    /// <param name="value">The string value to parse.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed enum value or defaultValue if parsing fails.</returns>
    public static T ToEnum<T>(this string value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// Escapes string for safe JSON serialization.
    /// Handles quotes, backslashes, and control characters.
    /// Prevents JSON injection attacks.
    /// </summary>
    /// <param name="value">The string to escape for JSON.</param>
    /// <returns>The escaped string safe for JSON serialization.</returns>
    public static string EscapeForJson(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var sb = new StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append($"\\u{(int)c:X4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Checks if string contains any of the forbidden characters.
    /// Used for migration script validation to catch dangerous SQL patterns.
    /// </summary>
    /// <param name="value">The string to check for forbidden characters.</param>
    /// <param name="forbiddenChars">Array of forbidden characters to search for.</param>
    /// <returns>True if any forbidden character is found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="forbiddenChars"/> is null.</exception>
    public static bool ContainsForbiddenCharacters(this string value, string[] forbiddenChars)
    {
        ArgumentNullException.ThrowIfNull(forbiddenChars);

        return forbiddenChars.Any(fc => value.Contains(fc, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Normalizes whitespace: collapses multiple spaces to single space.
    /// Useful for normalizing user-provided descriptions and names.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>The normalized string with whitespace collapsed.</returns>
    public static string NormalizeWhitespace(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Checks if string is a valid file path pattern for backup/restore operations.
    /// Validates against directory traversal attacks (../ patterns).
    /// </summary>
    /// <param name="value">The string to validate as a file path.</param>
    /// <returns>True if valid file path, false otherwise.</returns>
    public static bool IsValidFilePath(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Reject paths containing directory traversal
        if (value.Contains("..") || value.Contains("//"))
            return false;

        // Only allow alphanumeric, slashes, dots, hyphens
        return Regex.IsMatch(value, @"^[a-zA-Z0-9._\-\/]+$");
    }

    /// <summary>
    /// Reverses a string for testing or cryptographic operations.
    /// </summary>
    /// <param name="value">The string to reverse.</param>
    /// <returns>The reversed string, or original if null/empty.</returns>
    public static string Reverse(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}