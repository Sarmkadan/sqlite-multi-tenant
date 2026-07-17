#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Api.Controllers;

/// <summary>
/// Provides extension methods for <see cref="SettingsController"/> to simplify common operations
/// and add useful convenience methods for working with application settings.
/// </summary>
public static class SettingsControllerExtensions
{
    /// <summary>
    /// Gets a setting by key and returns it as a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value.</typeparam>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="key">The setting key.</param>
    /// <returns>An <see cref="IActionResult"/> containing the setting value as type T, or default(T) if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="key"/> is null.</exception>
    public static IActionResult GetSettingAs<T>(this SettingsController controller, string key)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var result = controller.GetSetting(key);

        if (result is OkObjectResult okResult && okResult.Value is ApiResponse<SettingValue> response)
        {
            if (response.Data?.Value is T typedValue)
            {
                return controller.Ok(ApiResponse<T>.Success(typedValue));
            }

            return controller.Ok(ApiResponse<T>.Success(default));
        }

        return result;
    }

    /// <summary>
    /// Sets a setting with a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>An <see cref="IActionResult"/> representing the operation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="key"/> is null.</exception>
    public static IActionResult SetSetting<T>(this SettingsController controller, string key, T value)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var request = new SetSettingRequest { Value = value };
        return controller.SetSetting(key, request);
    }

    /// <summary>
    /// Sets multiple settings with strongly-typed values from a dictionary.
    /// </summary>
    /// <typeparam name="T">The type of values in the dictionary.</typeparam>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="settings">Dictionary of setting keys to values.</param>
    /// <returns>An <see cref="IActionResult"/> representing the batch update result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="settings"/> is null.</exception>
    public static IActionResult UpdateBatchSettings<T>(this SettingsController controller, Dictionary<string, T> settings)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(settings);

        var dictionary = settings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        return controller.UpdateBatchSettings(dictionary);
    }

    /// <summary>
    /// Checks if a setting exists and returns a boolean result.
    /// </summary>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="key">The setting key to check.</param>
    /// <returns>An <see cref="IActionResult"/> containing true if the setting exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="key"/> is null.</exception>
    public static IActionResult SettingExists(this SettingsController controller, string key)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var result = controller.CheckSetting(key);

        return result switch
        {
            OkResult => controller.Ok(ApiResponse<bool>.Success(true)),
            NotFoundResult => controller.Ok(ApiResponse<bool>.Success(false)),
            _ => result
        };
    }

    /// <summary>
    /// Gets all settings and filters them by a predicate.
    /// </summary>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="predicate">Filter predicate to apply to settings.</param>
    /// <returns>An <see cref="IActionResult"/> containing the filtered collection of settings matching the predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="predicate"/> is null.</exception>
    public static IActionResult GetSettingsWhere(this SettingsController controller, Func<SettingValue, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(predicate);

        var allSettingsResult = controller.GetAllSettings();

        if (allSettingsResult is OkObjectResult allOkResult &&
            allOkResult.Value is ApiResponse<Dictionary<string, object>> allResponse)
        {
            var filtered = new List<SettingValue>();

            if (allResponse.Data is not null)
            {
                foreach (var kvp in allResponse.Data)
                {
                    filtered.Add(new SettingValue
                    {
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Type = kvp.Value?.GetType().Name ?? "null"
                    });
                }
            }

            var matching = filtered.Where(predicate).ToList();
            return controller.Ok(ApiResponse<IReadOnlyList<SettingValue>>.Success(matching));
        }

        return allSettingsResult;
    }

    /// <summary>
    /// Gets a setting and attempts to parse it as a specific type.
    /// </summary>
    /// <typeparam name="T">The target type to parse as.</typeparam>
    /// <param name="controller">The settings controller instance.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="parser">Optional custom parser function.</param>
    /// <returns>An <see cref="IActionResult"/> containing the parsed value or default if parsing fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="key"/> is null.</exception>
    public static IActionResult GetSettingAs<T>(
        this SettingsController controller,
        string key,
        Func<string, Type, T>? parser = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var result = controller.GetSetting(key);

        if (result is OkObjectResult okResult && okResult.Value is ApiResponse<SettingValue> response)
        {
            if (response.Data?.Value is string stringValue)
            {
                try
                {
                    T? parsedValue;
                    if (parser is not null)
                    {
                        parsedValue = parser(stringValue, typeof(T));
                    }
                    else
                    {
                        parsedValue = (T)Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
                    }

                    return controller.Ok(ApiResponse<T>.Success(parsedValue));
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
                {
                    return controller.Ok(ApiResponse<T>.Success(default));
                }
            }
            else if (response.Data?.Value is T typedValue)
            {
                return controller.Ok(ApiResponse<T>.Success(typedValue));
            }
        }

        return result;
    }
}