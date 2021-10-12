#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides validation helpers for the Tenant class
/// </summary>
public static class TenantValidation
{
    /// <summary>
    /// Validates a Tenant instance and returns a list of validation errors
    /// </summary>
    /// <param name="value">The tenant to validate</param>
    /// <returns>Read-only list of validation error messages (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this Tenant value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate TenantId
        if (string.IsNullOrWhiteSpace(value.TenantId))
        {
            errors.Add("TenantId is required");
        }
        else if (value.TenantId.Length > TenantConstants.MaxTenantIdLength)
        {
            errors.Add($"TenantId exceeds maximum length of {TenantConstants.MaxTenantIdLength} characters");
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add("Name is required");
        }
        else if (value.Name.Length > TenantConstants.MaxTenantNameLength)
        {
            errors.Add($"Name exceeds maximum length of {TenantConstants.MaxTenantNameLength} characters");
        }

        // Validate Status
        if (!Enum.IsDefined(typeof(TenantStatus), value.Status))
        {
            errors.Add("Status is invalid");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            errors.Add("CreatedAt must be set to a valid date");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAt cannot be in the future");
        }

        // Validate UpdatedAt
        if (value.UpdatedAt == default)
        {
            errors.Add("UpdatedAt must be set to a valid date");
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("UpdatedAt cannot be in the future");
        }

        // Validate CreatedAt vs UpdatedAt
        if (value.CreatedAt > value.UpdatedAt)
        {
            errors.Add("CreatedAt cannot be after UpdatedAt");
        }

        // Validate LastAccessedAt
        if (value.LastAccessedAt.HasValue && value.LastAccessedAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("LastAccessedAt cannot be in the future");
        }

        // Validate ContactEmail if provided
        if (value.ContactEmail is not null)
        {
            if (string.IsNullOrWhiteSpace(value.ContactEmail))
            {
                errors.Add("ContactEmail must be a valid email address or null");
            }
            else if (value.ContactEmail.Length > 254)
            {
                errors.Add("ContactEmail exceeds maximum length of 254 characters");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(value.ContactEmail,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                errors.Add("ContactEmail must be a valid email address format");
            }
        }

        // Validate DatabasePath if provided
        if (value.DatabasePath is not null)
        {
            if (string.IsNullOrWhiteSpace(value.DatabasePath))
            {
                errors.Add("DatabasePath must be a valid path or null");
            }
            else if (value.DatabasePath.Length > TenantConstants.MaxDatabasePathLength)
            {
                errors.Add($"DatabasePath exceeds maximum length of {TenantConstants.MaxDatabasePathLength} characters");
            }
        }

        // Validate IsDataIsolated
        // No specific validation needed for boolean

        // Validate MaxConnections
        if (value.MaxConnections <= 0)
        {
            errors.Add("MaxConnections must be greater than zero");
        }
        else if (value.MaxConnections > 1000)
        {
            errors.Add("MaxConnections cannot exceed 1000");
        }

        // Validate Metadata if provided
        if (value.Metadata is not null)
        {
            if (value.Metadata.Count > 1000)
            {
                errors.Add("Metadata dictionary cannot contain more than 1000 entries");
            }

            foreach (var kvp in value.Metadata)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    errors.Add("Metadata key cannot be null or empty");
                }
                else if (kvp.Key.Length > 128)
                {
                    errors.Add("Metadata key exceeds maximum length of 128 characters");
                }

                if (kvp.Value is not null && kvp.Value.Length > 1024)
                {
                    errors.Add("Metadata value exceeds maximum length of 1024 characters");
                }
            }
        }

        // Validate Databases collection
        if (value.Databases is null)
        {
            errors.Add("Databases collection cannot be null");
        }
        else
        {
            // Individual database validation is handled by TenantDatabase.Validate
            if (value.Databases.Any(db => db is null))
            {
                errors.Add("Databases collection contains null entries");
            }
        }

        // Validate Settings collection
        if (value.Settings is null)
        {
            errors.Add("Settings collection cannot be null");
        }
        else
        {
            // Individual settings validation is handled by TenantSettings.Validate
            if (value.Settings.Any(s => s is null))
            {
                errors.Add("Settings collection contains null entries");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a Tenant instance is valid
    /// </summary>
    /// <param name="value">The tenant to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this Tenant value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures a Tenant instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The tenant to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if value is invalid with validation errors</exception>
    public static void EnsureValid(this Tenant value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Tenant validation failed: {string.Join("; ", errors)}");
        }
    }
}
