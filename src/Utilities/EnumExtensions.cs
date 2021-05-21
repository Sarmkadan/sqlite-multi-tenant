#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static string GetDisplayName<T>(this T enumValue) where T : Enum
    {
        var value = enumValue.ToString();
        // Insert space before capital letters (except first)
        return System.Text.RegularExpressions.Regex.Replace(value, "([A-Z])", " $1").Trim();
    }

    /// <summary>
    /// Safely parses string to enum value with fallback.
    /// Returns default value if parsing fails instead of throwing.
    /// </summary>
    public static T ParseSafe<T>(this string value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// Checks if enum has a specific attribute.
    /// </summary>
    public static bool HasAttribute<T, TAttribute>(this T enumValue)
        where T : Enum
        where TAttribute : Attribute
    {
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        return fieldInfo?.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
    }

    /// <summary>
    /// Gets custom attribute from enum value.
    /// </summary>
    public static TAttribute GetAttribute<T, TAttribute>(this T enumValue)
        where T : Enum
        where TAttribute : Attribute
    {
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        return fieldInfo?.GetCustomAttributes(typeof(TAttribute), false).FirstOrDefault() as TAttribute;
    }

    /// <summary>
    /// Gets all values of an enum type.
    /// </summary>
    public static IEnumerable<T> GetAllValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    /// <summary>
    /// Checks if string is valid enum value.
    /// </summary>
    public static bool IsValidEnumValue<T>(this string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, ignoreCase: true, out _);
    }

    /// <summary>
    /// Converts enum to its description attribute if available.
    /// Falls back to display name if no description attribute.
    /// </summary>
    public static string GetDescription<T>(this T enumValue) where T : Enum
    {
        var attr = enumValue.GetAttribute<T, System.ComponentModel.DescriptionAttribute>();
        return attr?.Description ?? enumValue.GetDisplayName();
    }
}
