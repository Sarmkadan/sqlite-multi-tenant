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
    /// <typeparam name="T">The target type to parse the value as.</typeparam>
    /// <param name="config">The configuration instance.</param>
    /// <param name="key">The configuration key to read.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed value or default if parsing fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
    public static T GetValueSafe<T>(this IConfiguration config, string key, T defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var value = config[key];

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return value switch
            {
                { } v when typeof(T) == typeof(string) => (T)(object)v,
                { } v when typeof(T) == typeof(int) && int.TryParse(v, out var intResult) => (T)(object)intResult,
                { } v when typeof(T) == typeof(bool) && bool.TryParse(v, out var boolResult) => (T)(object)boolResult,
                { } v when typeof(T) == typeof(long) && long.TryParse(v, out var longResult) => (T)(object)longResult,
                { } v when typeof(T) == typeof(TimeSpan) && TimeSpan.TryParse(v, out var timeSpan) => (T)(object)timeSpan,
                _ => defaultValue
            };
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Validates required configuration exists and is not empty.
    /// Throws <see cref="ArgumentNullException"/> if config or key is null.
    /// Throws <see cref="ArgumentException"/> if key is empty or whitespace.
    /// Throws <see cref="InvalidOperationException"/> if value is missing or empty.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="key">The configuration key to validate.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the configuration value is missing or empty.</exception>
    public static string GetRequiredValue(this IConfiguration config, string key)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = config[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required configuration '{key}' is missing or empty");

        return value;
    }

    /// <summary>
    /// Binds configuration section to strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to bind to. Must have a parameterless constructor.</typeparam>
    /// <param name="config">The configuration instance.</param>
    /// <param name="sectionKey">The configuration section key to bind.</param>
    /// <returns>The bound instance of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sectionKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="sectionKey"/> is empty or whitespace.</exception>
    public static T BindSection<T>(this IConfiguration config, string sectionKey) where T : new()
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(sectionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        var section = config.GetSection(sectionKey);
        var result = new T();

        section.Bind(result);

        return result;
    }

    /// <summary>
    /// Gets connection string with fallback and validation.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="name">The connection string name.</param>
    /// <param name="defaultValue">The default value to return if connection string is not found.</param>
    /// <returns>The connection string value or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is empty or whitespace.</exception>
    public static string GetConnectionStringSafe(this IConfiguration config, string name, string defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var connectionString = config.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionString))
            return defaultValue ?? throw new InvalidOperationException($"Connection string '{name}' not found");

        return connectionString;
    }

    /// <summary>
    /// Checks if configuration key exists and has a value.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>True if the key exists and has a non-empty value; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
    public static bool HasValue(this IConfiguration config, string key)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(key);

        return !string.IsNullOrEmpty(config[key]);
    }

    /// <summary>
    /// Gets all configuration values for a section as dictionary.
    /// Useful for exporting configuration or debugging.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="sectionKey">The configuration section key to extract.</param>
    /// <returns>A dictionary containing all key-value pairs in the section.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sectionKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="sectionKey"/> is empty or whitespace.</exception>
    public static Dictionary<string, string> GetSectionAsDictionary(this IConfiguration config, string sectionKey)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(sectionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

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
    /// <param name="config">The configuration instance.</param>
    /// <param name="requiredKeys">Array of required configuration keys.</param>
    /// <returns>Collection of validation error messages. Empty if validation passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="requiredKeys"/> is null.</exception>
    public static IEnumerable<string> ValidateConfiguration(this IConfiguration config, params string[] requiredKeys)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(requiredKeys);

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
    /// <param name="config">The configuration root instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    public static void Reload(this IConfigurationRoot config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Reload();
    }

    /// <summary>
    /// Gets configuration value with environment variable override.
    /// Allows sensitive values to be set via environment variables.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="key">The configuration key to read.</param>
    /// <param name="envVar">Optional environment variable name override. If null, derived from key.</param>
    /// <returns>The environment variable value if set, otherwise the configuration value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
    public static string GetValueWithEnvironmentOverride(this IConfiguration config, string key, string envVar = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(key);

        var envVarName = envVar ?? key.ToUpperInvariant().Replace(":", "_");
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
public sealed class ConfigurationBuilder
{
    private readonly Microsoft.Extensions.Configuration.ConfigurationBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class.
    /// </summary>
    public ConfigurationBuilder()
    {
        _builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
    }

    /// <summary>
    /// Adds JSON configuration file.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether to reload the file if it changes.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder AddJsonFile(string path, bool optional = false, bool reloadOnChange = true)
    {
        _builder.AddJsonFile(path, optional, reloadOnChange);
        return this;
    }

    /// <summary>
    /// Adds environment variables with prefix.
    /// </summary>
    /// <param name="prefix">The prefix to filter environment variables.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder AddEnvironmentVariables(string prefix = null)
    {
        _builder.AddEnvironmentVariables(prefix);
        return this;
    }

    /// <summary>
    /// Adds in-memory configuration from dictionary.
    /// </summary>
    /// <param name="settings">The dictionary containing configuration key-value pairs.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
    public ConfigurationBuilder AddInMemory(Dictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _builder.AddInMemoryCollection(settings);
        return this;
    }

    /// <summary>
    /// Builds final configuration.
    /// </summary>
    /// <returns>The built configuration root.</returns>
    public IConfigurationRoot Build()
    {
        return _builder.Build();
    }

    /// <summary>
    /// Creates builder with standard configuration for application.
    /// Loads: appsettings.json -> appsettings.{env}.json -> environment variables
    /// </summary>
    /// <param name="environment">Optional environment name. If null, reads from ASPNETCORE_ENVIRONMENT.</param>
    /// <returns>The built configuration root.</returns>
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
