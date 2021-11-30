#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for enum operations.
/// Provides safe enum parsing, display names, and conversion utilities.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Converts enum to its display name (handles PascalCase to Title Case).
    /// Example: TenantStatus.Active -> "Active"
    /// </summary>
    /// <param name="enumValue">The enum value to convert.</param>
    /// <returns>The display name with spaces between words.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumValue"/> is <see langword="null"/>.</exception>
    public static string GetDisplayName<T>(this T enumValue) where T : Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        var value = enumValue.ToString();
        // Insert space before capital letters (except first)
        return Regex.Replace(value, "([A-Z])", " $1").Trim();
    }

    /// <summary>
    /// Safely parses string to enum value with fallback.
    /// Returns default value if parsing fails instead of throwing.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed enum value or default value if parsing fails.</returns>
    public static T ParseSafe<T>(this string value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Checks if enum has a specific attribute.
    /// </summary>
    /// <param name="enumValue">The enum value to check.</param>
    /// <returns><see langword="true"/> if the attribute exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumValue"/> is <see langword="null"/>.</exception>
    public static bool HasAttribute<T, TAttribute>(this T enumValue)
        where T : Enum
        where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        return fieldInfo is not null && fieldInfo.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
    }

    /// <summary>
    /// Gets custom attribute from enum value.
    /// </summary>
    /// <param name="enumValue">The enum value to get attribute from.</param>
    /// <returns>The attribute instance or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumValue"/> is <see langword="null"/>.</exception>
    public static TAttribute? GetAttribute<T, TAttribute>(this T enumValue)
        where T : Enum
        where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        return fieldInfo?.GetCustomAttributes(typeof(TAttribute), false).FirstOrDefault() as TAttribute;
    }

    /// <summary>
    /// Gets all values of an enum type.
    /// </summary>
    /// <returns>An enumerable of all enum values.</returns>
    public static IEnumerable<T> GetAllValues<T>() where T : Enum
        => Enum.GetValues(typeof(T)).Cast<T>();

    /// <summary>
    /// Checks if string is valid enum value.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <returns><see langword="true"/> if the string represents a valid enum value; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidEnumValue<T>(this string? value) where T : struct, Enum
        => !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, ignoreCase: true, out _);

    /// <summary>
    /// Converts enum to its description attribute if available.
    /// Falls back to display name if no description attribute.
    /// </summary>
    /// <param name="enumValue">The enum value to convert.</param>
    /// <returns>The description attribute value or display name if no description exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumValue"/> is <see langword="null"/>.</exception>
    public static string GetDescription<T>(this T enumValue) where T : Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        var attr = enumValue.GetAttribute<T, DescriptionAttribute>();
        return attr?.Description ?? enumValue.GetDisplayName();
    }
}