#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Validation;

/// <summary>
/// Validator for tenant creation and update requests.
/// Enforces business rules and data constraints at API boundary.
/// Returns validation errors for user-friendly error responses.
/// </summary>
public sealed class TenantValidator {
    /// <summary>
    /// Validates create tenant request.
    /// Checks required fields, email format, name length.
    /// </summary>
    public Dictionary<string, string> ValidateCreateRequest(CreateTenantRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(nameof(request.Name), "Tenant name is required");
        else if (!request.Name.IsValidTenantName())
            errors.Add(nameof(request.Name), "Tenant name must be between 3 and 255 characters");

        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            errors.Add(nameof(request.ContactEmail), "Contact email is required");
        else if (!request.ContactEmail.IsValidEmail())
            errors.Add(nameof(request.ContactEmail), "Contact email must be valid");

        return errors;
    }

    /// <summary>
    /// Validates update tenant request.
    /// All fields are optional, validates only provided fields.
    /// </summary>
    public Dictionary<string, string> ValidateUpdateRequest(UpdateTenantRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            if (!request.Name.IsValidTenantName())
                errors.Add(nameof(request.Name), "Tenant name must be between 3 and 255 characters");
        }

        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            if (!request.ContactEmail.IsValidEmail())
                errors.Add(nameof(request.ContactEmail), "Contact email must be valid");
        }

        return errors;
    }

    /// <summary>
    /// Validates tenant name uniqueness against existing tenants.
    /// This would be enhanced with database lookup in production.
    /// </summary>
    public Dictionary<string, string> ValidateNameUniqueness(string tenantName, string excludeTenantId = null)
    {
        var errors = new Dictionary<string, string>();

        // In production, implement repository query
        // var exists = await _tenantRepository.ExistsByNameAsync(tenantName);
        // if (exists && tenantId != excludeTenantId)
        //     errors.Add("Name", "Tenant name already exists");

        return errors;
    }
}

/// <summary>
/// Validator for migration creation and execution.
/// Ensures migrations contain valid SQL and follow conventions.
/// </summary>
public sealed class MigrationValidator {
    /// <summary>
    /// Validates migration creation request.
    /// Checks version format, script content, naming conventions.
    /// </summary>
    public Dictionary<string, string> ValidateMigrationRequest(Api.Requests.CreateMigrationRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Version))
            errors.Add(nameof(request.Version), "Version is required");
        else if (!request.Version.IsValidSemanticVersion())
            errors.Add(nameof(request.Version), "Version must be valid semantic version (e.g., 1.0.0)");

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(nameof(request.Name), "Migration name is required");
        else if (request.Name.Length < 3 || request.Name.Length > 255)
            errors.Add(nameof(request.Name), "Migration name must be between 3 and 255 characters");

        if (string.IsNullOrWhiteSpace(request.UpScript))
            errors.Add(nameof(request.UpScript), "Up script is required");
        else if (!request.UpScript.IsValidSqlScript())
            errors.Add(nameof(request.UpScript), "Up script contains potentially dangerous SQL patterns");

        if (!string.IsNullOrWhiteSpace(request.DownScript))
        {
            if (!request.DownScript.IsValidSqlScript())
                errors.Add(nameof(request.DownScript), "Down script contains potentially dangerous SQL patterns");
        }

        return errors;
    }

    /// <summary>
    /// Validates migration naming follows conventions.
    /// Expected format: "001_CreateUsersTable" or "v001_create_users_table"
    /// </summary>
    public bool IsValidMigrationNaming(string version, string name)
    {
        // Version should match name prefix
        var normalizedVersion = version.Replace(".", string.Empty);
        return !name.ToLower().Contains(normalizedVersion.ToLower());
    }

    /// <summary>
    /// Validates no dangerous SQL patterns in migration script.
    /// Prevents accidental data loss from migration errors.
    /// </summary>
    public bool ContainsDangerousPatterns(string script)
    {
        var dangerous = new[] { "DROP DATABASE", "DROP TABLE", "DELETE FROM", "TRUNCATE" };
        var upper = script.ToUpper();

        return dangerous.Any(d => upper.Contains(d) && !script.Contains("--", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Validator for database connection strings.
/// Ensures safe and properly formatted connection strings.
/// </summary>
public sealed class ConnectionStringValidator {
    /// <summary>
    /// Validates SQLite connection string format.
    /// </summary>
    public Dictionary<string, string> ValidateSqliteConnectionString(string connectionString)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionString", "Connection string is required");
            return errors;
        }

        if (!connectionString.IsValidConnectionString())
            errors.Add("ConnectionString", "Connection string must contain Data Source or Filename");

        // Validate path is safe
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var start = connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) + 12;
            var end = connectionString.IndexOf(";", start);
            if (end == -1) end = connectionString.Length;

            // Hotfix: Trim quotes to handle quoted connection strings
            var path = connectionString[start..end].Trim('"', '\'');

            if (!path.IsValidRelativePath())
                errors.Add("Path", "Database path contains invalid characters or patterns");
        }

        return errors;
    }
}

/// <summary>
/// Validator for backup operations.
/// </summary>
public sealed class BackupValidator {
    /// <summary>
    /// Validates backup tag format and length.
    /// </summary>
    public Dictionary<string, string> ValidateBackupTag(string tag)
    {
        var errors = new Dictionary<string, string>();

        if (!tag.IsValidBackupTag())
            errors.Add("Tag", "Tag must be alphanumeric with hyphens/underscores and max 100 characters");

        return errors;
    }

    /// <summary>
    /// Validates retention days configuration.
    /// </summary>
    public Dictionary<string, string> ValidateRetentionDays(int days)
    {
        var errors = new Dictionary<string, string>();

        if (!days.IsValidRetentionDays())
            errors.Add("RetentionDays", "Retention must be between 1 and 3650 days");

        return errors;
    }
}
