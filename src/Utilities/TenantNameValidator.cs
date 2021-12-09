#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Provides methods for validating and generating tenant names and IDs.
    /// </summary>
    public static class TenantNameValidator
    {
        private const int MinTenantIdLength   = 3;
        private const int MaxTenantIdLength   = 50;
        private const int MinTenantNameLength = 1;
        private const int MaxTenantNameLength = 255;

        private static readonly Regex TenantIdPattern =
            new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private static readonly Regex TenantNamePattern =
            new Regex(@"^[a-zA-Z0-9\s\-'\.]+$", RegexOptions.Compiled);

        private static readonly Regex DatabaseIdentifierPattern =
            new Regex(@"^[a-zA-Z0-9_\-.]+$", RegexOptions.Compiled);

        // FrozenSet gives O(1) lookup vs O(n) array scan; OrdinalIgnoreCase
        // eliminates the ToLower() allocation in the caller.
        private static readonly FrozenSet<string> ReservedIds =
            new[] { "admin", "system", "root", "test", "default", "local", "api",
                    "backup", "restore", "maintenance", "template", "sample" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        // Static array avoids per-call allocation; OrdinalIgnoreCase span search
        // below eliminates the ToUpper() string allocation.
        private static readonly string[] SqlInjectionPatterns =
        {
            "--", "/*", "*/", "xp_", "sp_", "DROP", "DELETE", "UPDATE", "INSERT",
            "CREATE", "ALTER", "EXEC", "EXECUTE", ";", "UNION", "SELECT", "WHERE"
        };

        /// <summary>
        /// Validates a tenant ID.
        /// </summary>
        /// <param name="tenantId">The tenant ID to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> indicating whether the tenant ID is valid.</returns>
        public static ValidationResult ValidateTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return new ValidationResult { IsValid = false, Error = "Tenant ID cannot be empty" };

            var id = tenantId.Trim();

            if (id.Length < MinTenantIdLength)
                return new ValidationResult { IsValid = false, Error = $"Tenant ID must be at least {MinTenantIdLength} characters long" };

            if (id.Length > MaxTenantIdLength)
                return new ValidationResult { IsValid = false, Error = $"Tenant ID must not exceed {MaxTenantIdLength} characters" };

            if (!TenantIdPattern.IsMatch(id))
                return new ValidationResult { IsValid = false, Error = "Tenant ID can only contain letters, numbers, hyphens, and underscores" };

            if (ReservedIds.Contains(id))
                return new ValidationResult { IsValid = false, Error = $"Tenant ID '{id}' is reserved and cannot be used" };

            if (ContainsSqlInjectionPattern(id))
                return new ValidationResult { IsValid = false, Error = "Tenant ID contains invalid patterns" };

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Validates a tenant name.
        /// </summary>
        /// <param name="tenantName">The tenant name to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> indicating whether the tenant name is valid.</returns>
        public static ValidationResult ValidateTenantName(string tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
                return new ValidationResult { IsValid = false, Error = "Tenant name cannot be empty" };

            var name = tenantName.Trim();

            if (name.Length < MinTenantNameLength)
                return new ValidationResult { IsValid = false, Error = $"Tenant name must be at least {MinTenantNameLength} character long" };

            if (name.Length > MaxTenantNameLength)
                return new ValidationResult { IsValid = false, Error = $"Tenant name must not exceed {MaxTenantNameLength} characters" };

            if (!TenantNamePattern.IsMatch(name))
                return new ValidationResult { IsValid = false, Error = "Tenant name contains invalid characters" };

            if (ContainsSqlInjectionPattern(name))
                return new ValidationResult { IsValid = false, Error = "Tenant name contains invalid patterns" };

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Generates a tenant ID from a tenant name.
        /// </summary>
        /// <param name="tenantName">The tenant name to generate a tenant ID from.</param>
        /// <returns>A generated tenant ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tenantName"/> is null or empty.</exception>
        public static string GenerateTenantId(string tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentNullException(nameof(tenantName), "Tenant name cannot be null or empty when generating a tenant ID.");

            var id = tenantName.Trim().ToLower();
            id = Regex.Replace(id, @"\s+", "-");
            id = Regex.Replace(id, @"[^a-z0-9\-_]", "");
            id = Regex.Replace(id, @"-+", "-");
            id = id.Trim('-');

            if (id.Length < MinTenantIdLength)
                id = $"{id}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            if (id.Length > MaxTenantIdLength)
                id = id.Substring(0, MaxTenantIdLength);

            return id;
        }

        /// <summary>
        /// Checks if a string is a valid database identifier.
        /// </summary>
        /// <param name="identifier">The string to check.</param>
        /// <returns>True if the string is a valid database identifier; otherwise, false.</returns>
        public static bool IsValidDatabaseIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            return DatabaseIdentifierPattern.IsMatch(identifier);
        }

        /// <summary>
        /// Checks if a string contains any SQL injection patterns.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns>True if the string contains any SQL injection patterns; otherwise, false.</returns>
        private static bool ContainsSqlInjectionPattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var span = input.AsSpan();
            foreach (var pattern in SqlInjectionPatterns)
            {
                if (span.IndexOf(pattern.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public sealed class ValidationResult 
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets an error message if the validation was not successful.
        /// </summary>
        public string Error { get; set; }
    }
}
