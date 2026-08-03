#nullable enable
using System;

namespace SqliteMultiTenant.Models
{
    /// <summary>
    /// Extension methods for <see cref="TenantSettings"/>.
    /// </summary>
    public static class TenantSettingsExtensions
    {
        /// <summary>
        /// Attempts to retrieve the setting value converted to the specified type.
        /// If conversion fails, returns the supplied <paramref name="defaultValue"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the setting value to.</typeparam>
        /// <param name="settings">The <see cref="TenantSettings"/> instance.</param>
        /// <param name="defaultValue">The value to return when conversion fails.</param>
        /// <returns>The converted value or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        public static T GetValueOrDefault<T>(this TenantSettings settings, T defaultValue = default) where T : IConvertible
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                return settings.GetValue<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets the setting value as a <see cref="string"/>.
        /// Returns <paramref name="defaultValue"/> if conversion fails.
        /// </summary>
        /// <param name="settings">The <see cref="TenantSettings"/> instance.</param>
        /// <param name="defaultValue">The fallback string value.</param>
        /// <returns>The string representation of the setting value.</returns>
        public static string GetString(this TenantSettings settings, string defaultValue = "")
        {
            return settings.GetValueOrDefault(defaultValue);
        }

        /// <summary>
        /// Gets the setting value as an <see cref="int"/>.
        /// Returns <paramref name="defaultValue"/> if conversion fails.
        /// </summary>
        /// <param name="settings">The <see cref="TenantSettings"/> instance.</param>
        /// <param name="defaultValue">The fallback integer value.</param>
        /// <returns>The integer representation of the setting value.</returns>
        public static int GetInt(this TenantSettings settings, int defaultValue = 0)
        {
            return settings.GetValueOrDefault(defaultValue);
        }

        /// <summary>
        /// Gets the setting value as a <see cref="bool"/>.
        /// Returns <paramref name="defaultValue"/> if conversion fails.
        /// </summary>
        /// <param name="settings">The <see cref="TenantSettings"/> instance.</param>
        /// <param name="defaultValue">The fallback boolean value.</param>
        /// <returns>The boolean representation of the setting value.</returns>
        public static bool GetBool(this TenantSettings settings, bool defaultValue = false)
        {
            return settings.GetValueOrDefault(defaultValue);
        }

        /// <summary>
        /// Merges the values from <paramref name="source"/> into <paramref name="target"/>.
        /// Non‑null and non‑empty string values from the source overwrite the target's values.
        /// </summary>
        /// <param name="target">The <see cref="TenantSettings"/> instance to receive the merged values.</param>
        /// <param name="source">The <see cref="TenantSettings"/> instance providing values.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
        public static void Merge(this TenantSettings target, TenantSettings source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) return;

            if (!string.IsNullOrWhiteSpace(source.SettingId)) target.SettingId = source.SettingId;
            if (!string.IsNullOrWhiteSpace(source.TenantId)) target.TenantId = source.TenantId;
            if (!string.IsNullOrWhiteSpace(source.SettingKey)) target.SettingKey = source.SettingKey;
            if (!string.IsNullOrWhiteSpace(source.SettingValue)) target.SettingValue = source.SettingValue;
            if (!string.IsNullOrWhiteSpace(source.Description)) target.Description = source.Description;
            if (!string.IsNullOrWhiteSpace(source.DataType)) target.DataType = source.DataType;

            target.IsEncrypted = source.IsEncrypted;
            target.CreatedAt = source.CreatedAt;
            target.UpdatedAt = source.UpdatedAt;
            if (!string.IsNullOrWhiteSpace(source.LastModifiedBy)) target.LastModifiedBy = source.LastModifiedBy;
            target.IsActive = source.IsActive;

            if (source.Tenant != null) target.Tenant = source.Tenant;
        }
    }
}
