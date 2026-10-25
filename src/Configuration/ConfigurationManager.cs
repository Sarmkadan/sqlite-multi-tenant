#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// Centralized configuration management service with support for multiple sources.
/// Manages application settings with validation, type conversion, and default values.
/// Supports hot-reload of configuration changes without restart.
/// </summary>
public interface IConfigurationManager
{
    T Get<T>(string key, T defaultValue);
    void Set<T>(string key, T value);
    bool TryGet<T>(string key, out T? value);
    void Remove(string key);
    bool Contains(string key);
    Dictionary<string, object> GetAll();
}

public sealed class ConfigurationManager : IConfigurationManager
{
    private readonly Dictionary<string, object> _configuration;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly ConfigurationValidator _validator;
    private readonly Microsoft.Extensions.Configuration.IConfiguration? _appConfiguration;
    private readonly MultiTenantOptions? _multiTenantOptions;

    /// <summary>
    /// Creates a configuration manager backed by an <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
    /// source and validated <see cref="MultiTenantOptions"/>.
    /// </summary>
    public ConfigurationManager(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<ConfigurationManager> logger,
        Microsoft.Extensions.Options.IOptions<MultiTenantOptions> multiTenantOptions)
    {
        _appConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (multiTenantOptions is null)
        {
            throw new ArgumentNullException(nameof(multiTenantOptions));
        }

        var options = multiTenantOptions.Value ?? throw new ArgumentNullException(nameof(multiTenantOptions));

        if (options.DefaultMaxConnections <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultMaxConnections), "DefaultMaxConnections must be greater than 0.");
        }

        if (string.IsNullOrEmpty(options.BasePath))
        {
            throw new ArgumentException("BasePath cannot be null or empty.", nameof(options.BasePath));
        }

        if (!Directory.Exists(options.BasePath))
        {
            throw new DirectoryNotFoundException($"BasePath '{options.BasePath}' does not exist.");
        }

        _multiTenantOptions = options;
        _configuration = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _semaphore = new SemaphoreSlim(1);
        _validator = new ConfigurationValidator();

        _logger.LogInformation("Multi-tenant options validated successfully.");
    }

    /// <summary>
    /// Gets a configuration section from the underlying <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> source.
    /// </summary>
    public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
    {
        _logger.LogInformation("Getting configuration section: {Key}", key);
        if (_appConfiguration is null)
        {
            throw new InvalidOperationException("This ConfigurationManager instance was not created with an IConfiguration source.");
        }

        return _appConfiguration.GetSection(key);
    }

    /// <summary>
    /// Gets a tenant-specific setting, falling back to the global setting when not overridden.
    /// </summary>
    public string? GetTenantSetting(string tenantId, string key)
    {
        if (_appConfiguration is null)
        {
            throw new InvalidOperationException("This ConfigurationManager instance was not created with an IConfiguration source.");
        }

        var tenantValue = _appConfiguration[$"Tenants:{tenantId}:Settings:{key}"];
        if (!string.IsNullOrEmpty(tenantValue))
        {
            return tenantValue;
        }

        return _appConfiguration[$"GlobalSettings:{key}"];
    }

    /// <summary>
    /// Gets the validated multi-tenant options associated with this manager.
    /// </summary>
    public MultiTenantOptions GetMultiTenantOptions()
    {
        if (_multiTenantOptions is null)
        {
            throw new InvalidOperationException("This ConfigurationManager instance was not created with MultiTenantOptions.");
        }

        return _multiTenantOptions;
    }

    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        _logger = logger;
        _configuration = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _semaphore = new SemaphoreSlim(1);
        _validator = new ConfigurationValidator();
    }

    /// <summary>
    /// Gets a configuration value with a default fallback.
    /// </summary>
    public T Get<T>(string key, T defaultValue)
    {
        try
        {
            if (TryGet(key, out T? value))
            {
                return value!;
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting configuration '{Key}': {Message}", key, ex.Message);
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        try
        {
            _semaphore.Wait();

            if (value is null)
            {
                _configuration.Remove(key);
                _logger.LogInformation("Configuration removed: {Key}", key);
            }
            else
            {
                _configuration[key] = value;
                _logger.LogInformation("Configuration set: {Key}", key);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Tries to get a configuration value.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        try
        {
            _semaphore.Wait();

            if (_configuration.TryGetValue(key, out var configValue))
            {
                // Type conversion
                if (configValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                // Try to convert
                try
                {
                    value = (T?)Convert.ChangeType(configValue, typeof(T));
                    return value is not null;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes a configuration key.
    /// </summary>
    public void Remove(string key)
    {
        try
        {
            _semaphore.Wait();
            _configuration.Remove(key);
            _logger.LogInformation("Configuration removed: {Key}", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    public bool Contains(string key)
    {
        try
        {
            _semaphore.Wait();
            return _configuration.ContainsKey(key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets all configuration values.
    /// </summary>
    public Dictionary<string, object> GetAll()
    {
        try
        {
            _semaphore.Wait();
            return new Dictionary<string, object>(_configuration, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Loads configuration from a dictionary.
    /// </summary>
    public void LoadFromDictionary(Dictionary<string, object> settings)
    {
        try
        {
            _semaphore.Wait();

            foreach (var kvp in settings)
            {
                _configuration[kvp.Key] = kvp.Value;
            }

            _logger.LogInformation("Loaded {Count} configuration entries", settings.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Exports all configuration values.
    /// </summary>
    public Dictionary<string, object> ExportConfiguration()
    {
        return GetAll();
    }
}

/// <summary>
/// Validates configuration values.
/// </summary>
public sealed class ConfigurationValidator
{
    public bool ValidateConnectionString(string? connectionString)
    {
        return !string.IsNullOrWhiteSpace(connectionString) &&
               connectionString.Contains("Data Source");
    }

    public bool ValidatePort(int port)
    {
        return port > 0 && port < 65535;
    }

    public bool ValidateRetentionDays(int days)
    {
        return days > 0;
    }

    public bool ValidateFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Configuration settings for the application.
/// </summary>
public sealed class AppConfiguration
{
    /// <summary>Default value of <see cref="MaxConnections"/>.</summary>
    public const int DefaultMaxConnections = 20;

    /// <summary>Default value of <see cref="ConnectionTimeoutSeconds"/>.</summary>
    public const int DefaultConnectionTimeoutSeconds = 30;

    /// <summary>Default value of <see cref="BackupRetentionDays"/>.</summary>
    public const int DefaultBackupRetentionDays = 30;

    /// <summary>Default value of <see cref="EnableLogging"/>.</summary>
    public const bool DefaultEnableLogging = true;

    /// <summary>Default value of <see cref="EnableAuiting"/>.</summary>
    public const bool DefaultEnableAuditing = true;

    /// <summary>Default value of <see cref="AuditRetentionDays"/>.</summary>
    public const int DefaultAuditRetentionDays = 90;

    /// <summary>Default value of <see cref="MaxCacheItems"/>.</summary>
    public const int DefaultMaxCacheItems = 1000;

    /// <summary>Default value of <see cref="CacheExpirationMinutes"/>.</summary>
    public const int DefaultCacheExpirationMinutes = 60;

    /// <summary>Master database connection string. May contain credentials; it is excluded from <see cref="ToString"/>.</summary>
    public string MasterConnectionString { get; set; } = string.Empty;

    /// <summary>Directory holding tenant database files.</summary>
    public string DatabaseDirectory { get; set; } = string.Empty;

    /// <summary>Directory holding backup files.</summary>
    public string BackupDirectory { get; set; } = string.Empty;

    /// <summary>Maximum number of pooled connections.</summary>
    public int MaxConnections { get; set; } = DefaultMaxConnections;

    /// <summary>Connection timeout in seconds.</summary>
    public int ConnectionTimeoutSeconds { get; set; } = DefaultConnectionTimeoutSeconds;

    /// <summary>Number of days backups are retained.</summary>
    public int BackupRetentionDays { get; set; } = DefaultBackupRetentionDays;

    /// <summary>Whether database encryption is enabled.</summary>
    public bool EnableEncryption { get; set; }

    /// <summary>Whether logging is enabled.</summary>
    public bool EnableLogging { get; set; } = DefaultEnableLogging;

    /// <summary>Whether auditing is enabled.</summary>
    public bool EnableAuiting { get; set; } = DefaultEnableAuditing;

    /// <summary>Number of days audit entries are retained.</summary>
    public int AuditRetentionDays { get; set; } = DefaultAuditRetentionDays;

    /// <summary>Maximum number of items held in the cache.</summary>
    public int MaxCacheItems { get; set; } = DefaultMaxCacheItems;

    /// <summary>Cache entry expiration in minutes.</summary>
    public int CacheExpirationMinutes { get; set; } = DefaultCacheExpirationMinutes;

    /// <summary>
    /// Returns a summary of the non-secret option values.
    /// <see cref="MasterConnectionString"/> is deliberately omitted because it may contain credentials.
    /// </summary>
    /// <returns>A single-line representation of the configuration.</returns>
    public override string ToString()
    {
        return $"AppConfiguration(DatabaseDirectory={DatabaseDirectory}, BackupDirectory={BackupDirectory}, " +
               $"MaxConnections={MaxConnections}, ConnectionTimeoutSeconds={ConnectionTimeoutSeconds}, " +
               $"BackupRetentionDays={BackupRetentionDays}, EnableEncryption={EnableEncryption}, " +
               $"EnableLogging={EnableLogging}, EnableAuiting={EnableAuiting}, " +
               $"AuditRetentionDays={AuditRetentionDays}, MaxCacheItems={MaxCacheItems}, " +
               $"CacheExpirationMinutes={CacheExpirationMinutes})";
    }
}
