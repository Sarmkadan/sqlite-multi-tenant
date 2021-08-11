#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for common validation patterns in the application.
/// Used at system boundaries (controllers, services) to validate user input.
/// Returns bool to enable fluent validation chains.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates email address format using RFC 5322 simplified pattern.
    /// More permissive than strict RFC but catches obvious errors.
    /// </summary>
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates UUID format (v4 preferred).
    /// Accepts both with and without hyphens (e.g., both formats valid).
    /// </summary>
    public static bool IsValidUuid(this string uuid)
    {
        return Guid.TryParse(uuid, out _);
    }

    /// <summary>
    /// Validates a semantic version string (e.g., "1.0.0", "2.3.4-beta").
    /// Allows prerelease and build metadata per SemVer 2.0.0 spec.
    /// </summary>
    public static bool IsValidSemanticVersion(this string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var pattern = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?(\+[a-zA-Z0-9.]+)?$";
        return Regex.IsMatch(version, pattern);
    }

    /// <summary>
    /// Validates database name contains only safe characters.
    /// Prevents SQL injection by restricting to alphanumeric + underscore.
    /// </summary>
    public static bool IsValidDatabaseName(this string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
            return false;

        return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Validates tenant name is non-empty and reasonable length.
    /// Used to prevent storage bloat from extremely long names.
    /// </summary>
    public static bool IsValidTenantName(this string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length >= 3 && name.Length <= 255;
    }

    /// <summary>
    /// Validates a file path is within allowed directory and doesn't traverse up.
    /// Prevents "../../" attack vectors in user-provided paths.
    /// </summary>
    public static bool IsValidRelativePath(this string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
            return false;

        // Only allow forward/backward slashes, alphanumeric, dots, hyphens, spaces, and unicode characters
        // Hotfix: allow spaces and unicode characters in database path
        return Regex.IsMatch(path, @"^[\p{L}\p{N}._\-/\\ ]+$");
    }

    /// <summary>
    /// Validates SQL script is not empty and doesn't contain dangerous patterns.
    /// Catches obvious SQL injection attempts without full parsing.
    /// </summary>
    public static bool IsValidSqlScript(this string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return false;

        // Reject scripts with dangerous patterns (basic check)
        var dangerousPatterns = new[] { "DROP DATABASE", "DELETE FROM", "TRUNCATE" };
        var upperScript = script.ToUpper();

        return !dangerousPatterns.Any(pattern => upperScript.Contains(pattern));
    }

    /// <summary>
    /// Validates a port number is in valid range (1-65535).
    /// Used for connection string validation.
    /// </summary>
    public static bool IsValidPort(this int port)
    {
        return port >= 1 && port <= 65535;
    }

    /// <summary>
    /// Validates connection string contains required components.
    /// Checks for Data Source or Filename (SQLite) and basic format.
    /// </summary>
    public static bool IsValidConnectionString(this string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        // For SQLite: check for Data Source or Filename
        var hasDataSource = connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                           connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase);

        return hasDataSource;
    }

    /// <summary>
    /// Validates backup tag is within acceptable length and characters.
    /// Tags are used for categorization and searching.
    /// </summary>
    public static bool IsValidBackupTag(this string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag.Length > 100)
            return false;

        return Regex.IsMatch(tag, @"^[a-zA-Z0-9\-_]+$");
    }

    /// <summary>
    /// Checks if a collection is null or empty.
    /// Enables fluent validation for lists and arrays.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
    {
        return collection is null || !collection.Any();
    }

    /// <summary>
    /// Validates retention days is reasonable (between 1 and 3650 days ~10 years).
    /// Prevents configuration errors from setting unrealistic retention.
    /// </summary>
    public static bool IsValidRetentionDays(this int days)
    {
        return days >= 1 && days <= 3650;
    }

    /// <summary>
    /// Validates connection timeout is within reasonable bounds.
    /// Prevents extremely long timeouts that could cause application hangs.
    /// </summary>
    public static bool IsValidConnectionTimeout(this int timeoutSeconds)
    {
        return timeoutSeconds >= 1 && timeoutSeconds <= 300;
    }
}
