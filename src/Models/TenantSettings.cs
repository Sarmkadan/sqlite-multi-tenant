#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents configuration settings for a tenant
/// </summary>
public sealed class TenantSettings {
    /// <summary>
    /// Unique identifier for the setting
    /// </summary>
    public string SettingId { get; set; } = string.Empty;
    /// <summary>
    /// Identifier of the tenant this setting belongs to
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Key or name of the setting
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;
    /// <summary>
    /// Value of the setting
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;
    /// <summary>
    /// Optional description of the setting
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Data type of the setting value (e.g., string, int, bool)
    /// </summary>
    public string? DataType { get; set; }
    /// <summary>
    /// Indicates if the setting value is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }
    /// <summary>
    /// Timestamp when the setting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Timestamp when the setting was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// User who last modified the setting
    /// </summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>
    /// Indicates if the setting is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Navigation property to the tenant this setting belongs to
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    /// Validates the settings entity
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SettingId))
            errors.Add("SettingId is required");

        if (string.IsNullOrWhiteSpace(TenantId))
            errors.Add("TenantId is required");

        if (string.IsNullOrWhiteSpace(SettingKey))
            errors.Add("SettingKey is required");

        if (string.IsNullOrWhiteSpace(SettingValue))
            errors.Add("SettingValue is required");

        if (SettingKey.Length > 256)
            errors.Add("SettingKey exceeds maximum length");

        return errors.Count == 0;
    }

    /// <summary>
    /// Updates the setting value
    /// </summary>
    public void UpdateValue(string newValue, string? modifiedBy = null)
    {
        SettingValue = newValue;
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Toggles the active state
    /// </summary>
    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the setting value as the specified type
    /// </summary>
    public T GetValue<T>() where T : IConvertible
    {
        try
        {
            return (T)Convert.ChangeType(SettingValue, typeof(T));
        }
        catch
        {
            throw new InvalidOperationException($"Cannot convert '{SettingValue}' to type {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Sets the setting value from a generic type
    /// </summary>
    public void SetValue<T>(T value, string? modifiedBy = null) where T : IConvertible
    {
        SettingValue = value?.ToString() ?? string.Empty;
        DataType = typeof(T).Name;
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
