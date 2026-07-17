#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Extension methods for <see cref="TenantSettingsEdgeCaseTests"/> providing utility methods for testing
/// type conversions, validation scenarios, and edge cases.
/// </summary>
public static class TenantSettingsEdgeCaseTestsExtensions
{
    /// <summary>
    /// Creates a test <see cref="TenantSettings"/> instance with default valid values.
    /// </summary>
    /// <param name="settingId">The setting ID to use.</param>
    /// <param name="tenantId">The tenant ID to use.</param>
    /// <param name="settingKey">The setting key to use.</param>
    /// <param name="settingValue">The setting value to use.</param>
    /// <returns>A new <see cref="TenantSettings"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settingId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="settingId"/> is empty.</exception>
    public static TenantSettings CreateValidSettings(
        this TenantSettingsEdgeCaseTests _,
        string settingId = "test-setting-id",
        string tenantId = "test-tenant-id",
        string settingKey = "test-setting-key",
        string settingValue = "test-value")
    {
        ArgumentException.ThrowIfNullOrEmpty(settingId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(settingKey);

        return new TenantSettings
        {
            SettingId = settingId,
            TenantId = tenantId,
            SettingKey = settingKey,
            SettingValue = settingValue
        };
    }

    /// <summary>
    /// Creates a test <see cref="TenantSettings"/> instance with the specified data type.
    /// </summary>
    /// <param name="dataType">The data type name (e.g., "Int32", "Boolean", "String").</param>
    /// <param name="settingValue">The setting value to use.</param>
    /// <returns>A new <see cref="TenantSettings"/> instance with the specified data type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dataType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="dataType"/> is empty.</exception>
    public static TenantSettings CreateSettingsWithDataType(
        this TenantSettingsEdgeCaseTests _,
        string dataType,
        string settingValue = "test-value")
    {
        ArgumentException.ThrowIfNullOrEmpty(dataType);

        return new TenantSettings
        {
            SettingId = "test-id",
            TenantId = "test-tenant",
            SettingKey = "test-key",
            SettingValue = settingValue,
            DataType = dataType
        };
    }

    /// <summary>
    /// Validates that the setting is active and returns the validation result.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="settings">The settings to validate.</param>
    /// <param name="errors">When this method returns, contains the validation error messages if validation fails; otherwise, an empty list.</param>
    /// <returns><see langword="true"/> if validation passes; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool ValidateAndGetErrors(
        this TenantSettingsEdgeCaseTests test,
        TenantSettings settings,
        out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var isValid = settings.Validate(out var validationErrors);
        errors = validationErrors.AsReadOnly();
        return isValid;
    }

    /// <summary>
    /// Gets the value as a specific type with culture-invariant parsing.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="settings">The settings instance.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when conversion fails.</exception>
    public static T GetValueWithCulture<T>(
        this TenantSettingsEdgeCaseTests _,
        TenantSettings settings)
        where T : struct, IConvertible
    {
        ArgumentNullException.ThrowIfNull(settings);

        var value = settings.GetValue<T>();
        return value;
    }

    /// <summary>
    /// Creates a settings instance with a numeric value of the specified type.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The numeric value.</param>
    /// <param name="modifiedBy">The user who modified the setting.</param>
    /// <returns>A new <see cref="TenantSettings"/> instance with the numeric value set.</returns>
    public static TenantSettings CreateNumericSettings<T>(
        this TenantSettingsEdgeCaseTests _,
        T value,
        string? modifiedBy = null)
        where T : struct, IConvertible, IComparable, IFormattable
    {
        var settings = new TenantSettings();
        settings.SetValue(value, modifiedBy ?? "test-user");
        return settings;
    }

    /// <summary>
    /// Gets all validation error messages as a formatted string.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="settings">The settings to validate.</param>
    /// <returns>Formatted error messages joined by newline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string GetValidationErrorMessages(
        this TenantSettingsEdgeCaseTests test,
        TenantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _ = test.ValidateAndGetErrors(settings, out var errors);
        return string.Join(Environment.NewLine, errors);
    }

    /// <summary>
    /// Updates the setting value and verifies the timestamp was updated.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="settings">The settings to update.</param>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="expectedBefore">The expected timestamp before update.</param>
    /// <returns>The updated settings instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static TenantSettings UpdateAndVerifyTimestamp(
        this TenantSettingsEdgeCaseTests test,
        TenantSettings settings,
        string newValue,
        DateTime expectedBefore)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.UpdateValue(newValue, "test-updater");
        settings.UpdatedAt.Should().BeOnOrAfter(expectedBefore);

        return settings;
    }

    /// <summary>
    /// Creates a collection of settings with various data types for batch testing.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="count">Number of settings to create.</param>
    /// <returns>List of <see cref="TenantSettings"/> instances.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static IReadOnlyList<TenantSettings> CreateSettingsCollection(
        this TenantSettingsEdgeCaseTests test,
        int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }

        var settingsList = new List<TenantSettings>(count);
        for (var i = 0; i < count; i++)
        {
            settingsList.Add(test.CreateValidSettings(
                settingId: $"setting-{i}",
                settingKey: $"key-{i}",
                settingValue: $"value-{i}"
            ));
        }

        return settingsList.AsReadOnly();
    }

    /// <summary>
    /// Gets the value as a nullable type, returning null for empty strings.
    /// </summary>
    /// <typeparam name="T">The nullable type.</typeparam>
    /// <param name="settings">The settings instance.</param>
    /// <returns>The parsed value or null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static T? GetNullableValue<T>(
        this TenantSettingsEdgeCaseTests _,
        TenantSettings settings)
        where T : struct, IConvertible
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrEmpty(settings.SettingValue))
        {
            return null;
        }

        return settings.GetValue<T>();
    }

    /// <summary>
    /// Creates a settings instance with a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="modifiedBy">The user who modified the setting.</param>
    /// <returns>A new <see cref="TenantSettings"/> instance with the boolean value set.</returns>
    public static TenantSettings CreateBooleanSettings(
        this TenantSettingsEdgeCaseTests _,
        bool value,
        string? modifiedBy = null)
    {
        var settings = new TenantSettings();
        settings.SetValue(value, modifiedBy ?? "test-user");
        return settings;
    }
}