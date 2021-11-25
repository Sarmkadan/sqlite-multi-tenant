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
    /// <param name="email">The email address to validate.</param>
    /// <returns><see langword="true"/> if the email is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="email"/> is <see langword="null"/>.</exception>
    public static bool IsValidEmail(this string email)
    {
        ArgumentNullException.ThrowIfNull(email);

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
    /// <param name="uuid">The UUID string to validate.</param>
    /// <returns><see langword="true"/> if the UUID is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="uuid"/> is <see langword="null"/>.</exception>
    public static bool IsValidUuid(this string uuid)
    {
        ArgumentNullException.ThrowIfNull(uuid);
        return Guid.TryParse(uuid, out _);
    }

    /// <summary>
    /// Validates a semantic version string (e.g., "1.0.0", "2.3.4-beta").
    /// Allows prerelease and build metadata per SemVer 2.0.0 spec.
    /// </summary>
    /// <param name="version">The semantic version string to validate.</param>
    /// <returns><see langword="true"/> if the version is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="version"/> is <see langword="null"/>.</exception>
    public static bool IsValidSemanticVersion(this string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (string.IsNullOrWhiteSpace(version))
            return false;

        var pattern = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?(\+[a-zA-Z0-9.]+)?$";
        return Regex.IsMatch(version, pattern);
    }

    /// <summary>
    /// Validates database name contains only safe characters.
    /// Prevents SQL injection by restricting to alphanumeric + underscore.
    /// </summary>
    /// <param name="name">The database name to validate.</param>
    /// <returns><see langword="true"/> if the database name is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public static bool IsValidDatabaseName(this string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
            return false;

        return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Validates tenant name is non-empty and reasonable length.
    /// Used to prevent storage bloat from extremely long names.
    /// </summary>
    /// <param name="name">The tenant name to validate.</param>
    /// <returns><see langword="true"/> if the tenant name is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public static bool IsValidTenantName(this string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return !string.IsNullOrWhiteSpace(name) && name.Length >= 3 && name.Length <= 255;
    }

    /// <summary>
    /// Validates a file path is within allowed directory and doesn't traverse up.
    /// Prevents "../../" attack vectors in user-provided paths.
    /// </summary>
    /// <param name="path">The relative file path to validate.</param>
    /// <returns><see langword="true"/> if the path is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <see langword="null"/>.</exception>
    public static bool IsValidRelativePath(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
            return false;

        // Only allow forward/backward slashes, alphanumeric, dots, hyphens, spaces, and unicode characters
        return Regex.IsMatch(path, @"^[\p{L}\p{N}._\-/\\ ]+$");
    }

    /// <summary>
    /// Validates SQL script is not empty and doesn't contain dangerous patterns.
    /// Catches obvious SQL injection attempts without full parsing.
    /// </summary>
    /// <param name="script">The SQL script to validate.</param>
    /// <returns><see langword="true"/> if the script is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is <see langword="null"/>.</exception>
    public static bool IsValidSqlScript(this string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        if (string.IsNullOrWhiteSpace(script))
            return false;

        // Reject scripts with dangerous patterns (basic check)
        var dangerousPatterns = new[] { "DROP DATABASE", "DELETE FROM", "TRUNCATE" };
        var upperScript = script.ToUpperInvariant();

        return !dangerousPatterns.Any(pattern => upperScript.Contains(pattern, StringComparison.Ordinal));
    }

    /// <summary>
    /// Validates a port number is in valid range (1-65535).
    /// Used for connection string validation.
    /// </summary>
    /// <param name="port">The port number to validate.</param>
    /// <returns><see langword="true"/> if the port is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidPort(this int port)
    {
        return port >= 1 && port <= 65535;
    }

    /// <summary>
    /// Validates connection string contains required components.
    /// Checks for Data Source or Filename (SQLite) and basic format.
    /// </summary>
    /// <param name="connectionString">The connection string to validate.</param>
    /// <returns><see langword="true"/> if the connection string is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidConnectionString(this string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

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
    /// <param name="tag">The backup tag to validate.</param>
    /// <returns><see langword="true"/> if the tag is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is <see langword="null"/>.</exception>
    public static bool IsValidBackupTag(this string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (string.IsNullOrWhiteSpace(tag) || tag.Length > 100)
            return false;

        return Regex.IsMatch(tag, @"^[a-zA-Z0-9\-_]+$");
    }

    /// <summary>
    /// Checks if a collection is null or empty.
    /// Enables fluent validation for lists and arrays.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <returns><see langword="true"/> if the collection is null or empty; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
    {
        return collection is null || !collection.Any();
    }

    /// <summary>
    /// Validates retention days is reasonable (between 1 and 3650 days ~10 years).
    /// Prevents configuration errors from setting unrealistic retention.
    /// </summary>
    /// <param name="days">The number of retention days to validate.</param>
    /// <returns><see langword="true"/> if the retention days are valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidRetentionDays(this int days)
    {
        return days >= 1 && days <= 3650;
    }

    /// <summary>
    /// Validates connection timeout is within reasonable bounds.
    /// Prevents extremely long timeouts that could cause application hangs.
    /// </summary>
    /// <param name="timeoutSeconds">The connection timeout in seconds to validate.</param>
    /// <returns><see langword="true"/> if the timeout is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidConnectionTimeout(this int timeoutSeconds)
    {
        return timeoutSeconds >= 1 && timeoutSeconds <= 300;
    }
}
