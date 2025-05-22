// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities
{
    // Validates tenant identifiers and names according to business rules
    public static class TenantNameValidator
    {
        private const int MinTenantIdLength = 3;
        private const int MaxTenantIdLength = 50;
        private const int MinTenantNameLength = 1;
        private const int MaxTenantNameLength = 255;

        // Valid characters for tenant ID: alphanumeric, hyphens, underscores
        private static readonly Regex TenantIdPattern = new Regex(@"^[a-zA-Z0-9_-]+$");

        // Valid characters for tenant name: alphanumeric, spaces, hyphens, apostrophes
        private static readonly Regex TenantNamePattern = new Regex(@"^[a-zA-Z0-9\s\-'\.]+$");

        // Reserved tenant IDs that cannot be used
        private static readonly string[] ReservedIds = new[]
        {
            "admin", "system", "root", "test", "default", "local", "api", "admin", "api",
            "backup", "restore", "maintenance", "template", "sample"
        };

        // Validates a tenant ID
        public static ValidationResult ValidateTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant ID cannot be empty"
                };
            }

            var id = tenantId.Trim().ToLower();

            if (id.Length < MinTenantIdLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"Tenant ID must be at least {MinTenantIdLength} characters long"
                };
            }

            if (id.Length > MaxTenantIdLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"Tenant ID must not exceed {MaxTenantIdLength} characters"
                };
            }

            if (!TenantIdPattern.IsMatch(id))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant ID can only contain letters, numbers, hyphens, and underscores"
                };
            }

            if (ReservedIds.Contains(id))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"Tenant ID '{id}' is reserved and cannot be used"
                };
            }

            // Check for SQL injection patterns
            if (ContainsSqlInjectionPattern(id))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant ID contains invalid patterns"
                };
            }

            return new ValidationResult { IsValid = true };
        }

        // Validates a tenant name
        public static ValidationResult ValidateTenantName(string tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant name cannot be empty"
                };
            }

            var name = tenantName.Trim();

            if (name.Length < MinTenantNameLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"Tenant name must be at least {MinTenantNameLength} character long"
                };
            }

            if (name.Length > MaxTenantNameLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = $"Tenant name must not exceed {MaxTenantNameLength} characters"
                };
            }

            if (!TenantNamePattern.IsMatch(name))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant name contains invalid characters"
                };
            }

            // Check for SQL injection patterns
            if (ContainsSqlInjectionPattern(name))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Error = "Tenant name contains invalid patterns"
                };
            }

            return new ValidationResult { IsValid = true };
        }

        // Generates a tenant ID from a tenant name
        public static string GenerateTenantId(string tenantName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentNullException(nameof(tenantName), "Tenant name cannot be null or empty when generating a tenant ID.");

            // Convert to lowercase, replace spaces with hyphens, remove invalid characters
            var id = tenantName.Trim().ToLower();
            id = Regex.Replace(id, @"\s+", "-");
            id = Regex.Replace(id, @"[^a-z0-9\-_]", "");
            id = Regex.Replace(id, @"-+", "-");
            id = id.Trim('-');

            // Ensure minimum length
            if (id.Length < MinTenantIdLength)
            {
                id = $"{id}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // Ensure it doesn't exceed maximum length
            if (id.Length > MaxTenantIdLength)
            {
                id = id.Substring(0, MaxTenantIdLength);
            }

            return id;
        }

        // Checks if a tenant ID is in a valid format for database operations
        public static bool IsValidDatabaseIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Must be alphanumeric, hyphens, underscores, dots only
            return Regex.IsMatch(identifier, @"^[a-zA-Z0-9_\-.]+$");
        }

        // Detects potential SQL injection patterns
        private static bool ContainsSqlInjectionPattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var sqlPatterns = new[]
            {
                "--", "/*", "*/", "xp_", "sp_", "DROP", "DELETE", "UPDATE", "INSERT",
                "CREATE", "ALTER", "EXEC", "EXECUTE", ";", "UNION", "SELECT", "WHERE"
            };

            var upperInput = input.ToUpper();

            foreach (var pattern in sqlPatterns)
            {
                if (upperInput.Contains(pattern))
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
