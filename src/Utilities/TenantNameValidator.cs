// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities
{
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

        public static bool IsValidDatabaseIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            return DatabaseIdentifierPattern.IsMatch(identifier);
        }

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

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; }
    }
}
