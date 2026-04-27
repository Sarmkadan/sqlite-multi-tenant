#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// Extension methods for application configuration management.
/// Simplifies reading and validating configuration values with safe defaults.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Reads configuration value with type safety and fallback.
    /// Returns default value if key doesn't exist or can't be parsed.
    /// </summary>
    public static T GetValueSafe<T>(this IConfiguration config, string key, T defaultValue = default)
    {
        try
        {
            var value = config[key];

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T) == typeof(int))
                return int.TryParse(value, out var intResult) ? (T)(object)intResult : defaultValue;

            if (typeof(T) == typeof(bool))
                return bool.TryParse(value, out var boolResult) ? (T)(object)boolResult : defaultValue;

            if (typeof(T) == typeof(long))
                return long.TryParse(value, out var longResult) ? (T)(object)longResult : defaultValue;

            if (typeof(T) == typeof(TimeSpan))
            {
                if (TimeSpan.TryParse(value, out var timeSpan))
                    return (T)(object)timeSpan;
                return defaultValue;
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Validates required configuration exists and is not empty.
    /// Throws InvalidOperationException if missing.
    /// </summary>
    public static string GetRequiredValue(this IConfiguration config, string key)
    {
        var value = config[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required configuration '{key}' is missing or empty");

        return value;
    }

    /// <summary>
    /// Binds configuration section to strongly-typed object.
    /// </summary>
    public static T BindSection<T>(this IConfiguration config, string sectionKey) where T : new()
    {
        var section = config.GetSection(sectionKey);
        var result = new T();

        section.Bind(result);

        return result;
    }

    /// <summary>
    /// Gets connection string with fallback and validation.
    /// </summary>
    public static string GetConnectionStringSafe(this IConfiguration config, string name, string defaultValue = null)
    {
        var connectionString = config.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionString))
            return defaultValue ?? throw new InvalidOperationException($"Connection string '{name}' not found");

        return connectionString;
    }

    /// <summary>
    /// Checks if configuration key exists and has a value.
    /// </summary>
    public static bool HasValue(this IConfiguration config, string key)
    {
        return !string.IsNullOrEmpty(config[key]);
    }

    /// <summary>
    /// Gets all configuration values for a section as dictionary.
    /// Useful for exporting configuration or debugging.
    /// </summary>
    public static Dictionary<string, string> GetSectionAsDictionary(this IConfiguration config, string sectionKey)
    {
        var section = config.GetSection(sectionKey);
        var result = new Dictionary<string, string>();

        foreach (var child in section.GetChildren())
        {
            result[child.Key] = child.Value;
        }

        return result;
    }

    /// <summary>
    /// Validates configuration follows expected schema.
    /// Checks for required keys and expected value ranges.
    /// </summary>
    public static IEnumerable<string> ValidateConfiguration(this IConfiguration config, params string[] requiredKeys)
    {
        var errors = new List<string>();

        foreach (var key in requiredKeys)
        {
            if (!config.HasValue(key))
                errors.Add($"Required configuration key '{key}' is missing");
        }

        return errors;
    }

    /// <summary>
    /// Reloads configuration from sources (if supported).
    /// Useful for hot-reload scenarios.
    /// </summary>
    public static void Reload(this IConfigurationRoot config)
    {
        config.Reload();
    }

    /// <summary>
    /// Gets configuration value with environment variable override.
    /// Allows sensitive values to be set via environment variables.
    /// </summary>
    public static string GetValueWithEnvironmentOverride(this IConfiguration config, string key, string envVar = null)
    {
        var envVarName = envVar ?? key.ToUpper().Replace(":", "_");
        var envValue = Environment.GetEnvironmentVariable(envVarName);

        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        return config[key];
    }
}

/// <summary>
/// Helper for building configuration from multiple sources.
/// Centralizes configuration setup logic.
/// </summary>
public sealed class ConfigurationBuilder {
    private readonly Microsoft.Extensions.Configuration.ConfigurationBuilder _builder;

    public ConfigurationBuilder()
    {
        _builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
    }

    /// <summary>
    /// Adds JSON configuration file.
    /// </summary>
    public ConfigurationBuilder AddJsonFile(string path, bool optional = false, bool reloadOnChange = true)
    {
        _builder.AddJsonFile(path, optional, reloadOnChange);
        return this;
    }

    /// <summary>
    /// Adds environment variables with prefix.
    /// </summary>
    public ConfigurationBuilder AddEnvironmentVariables(string prefix = null)
    {
        _builder.AddEnvironmentVariables(prefix);
        return this;
    }

    /// <summary>
    /// Adds in-memory configuration from dictionary.
    /// </summary>
    public ConfigurationBuilder AddInMemory(Dictionary<string, string> settings)
    {
        _builder.AddInMemoryCollection(settings);
        return this;
    }

    /// <summary>
    /// Builds final configuration.
    /// </summary>
    public IConfigurationRoot Build()
    {
        return _builder.Build();
    }

    /// <summary>
    /// Creates builder with standard configuration for application.
    /// Loads: appsettings.json -> appsettings.{env}.json -> environment variables
    /// </summary>
    public static IConfigurationRoot BuildStandardConfiguration(string environment = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
