#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Configuration;

/// <summary>
/// Extension methods for configuring SQLite Multi-Tenant services
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Adds all multi-tenant services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddSqliteMultiTenant(this IServiceCollection services, string masterConnectionString)
    {
        if (string.IsNullOrWhiteSpace(masterConnectionString))
        {
            throw new ArgumentException("Connection string cannot be empty", nameof(masterConnectionString));
        }

        // Register repositories
        services.AddSingleton<ITenantRepository>(sp =>
            new TenantRepository(masterConnectionString, sp.GetRequiredService<ILogger<TenantRepository>>()));

        services.AddSingleton<IMigrationRepository>(sp =>
            new MigrationRepository(masterConnectionString, sp.GetRequiredService<ILogger<MigrationRepository>>()));

        services.AddSingleton<IBackupRepository>(sp =>
            new BackupRepository(masterConnectionString, sp.GetRequiredService<ILogger<BackupRepository>>()));

        // Register services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IMigrationService, MigrationService>();
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }

    /// <summary>
    /// Adds multi-tenant services with custom configuration
    /// </summary>
    public static IServiceCollection AddSqliteMultiTenant(
        this IServiceCollection services,
        string masterConnectionString,
        Action<SqliteMultiTenantOptions> configureOptions)
    {
        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        var options = new SqliteMultiTenantOptions();
        configureOptions(options);

        // Register options
        services.AddSingleton(options);

        // Use the standard configuration
        return services.AddSqliteMultiTenant(masterConnectionString);
    }
}

/// <summary>
/// Configuration options for multi-tenant system
/// </summary>
public class SqliteMultiTenantOptions
{
    /// <summary>
    /// Default maximum number of pooled connections.
    /// </summary>
    public const int DefaultMaxConnections = 10;

    /// <summary>
    /// Default connection timeout, in seconds.
    /// </summary>
    public const int DefaultConnectionTimeoutSeconds = 30;

    /// <summary>
    /// Default number of days backups are retained before cleanup.
    /// </summary>
    public const int DefaultBackupRetentionDays = 30;

    /// <summary>
    /// Default encryption setting (disabled).
    /// </summary>
    public const bool DefaultEnableEncryption = false;

    /// <summary>
    /// Default directory name for tenant backups.
    /// </summary>
    public const string DefaultBackupDirectory = "backups";

    /// <summary>
    /// Default directory name for tenant databases.
    /// </summary>
    public const string DefaultDatabaseDirectory = "databases";

    /// <summary>
    /// Default logging setting (enabled).
    /// </summary>
    public const bool DefaultEnableLogging = true;

    /// <summary>
    /// Default maximum number of items allowed in a single batch operation (500).
    /// </summary>
    public const int DefaultMaxBatchItems = 500;

    /// <summary>
    /// Default maximum total batch payload size in bytes (1 MB).
    /// </summary>
    public const long DefaultMaxBatchPayloadSizeBytes = 1024 * 1024;

    public int MaxConnections { get; set; } = DefaultMaxConnections;
    public int ConnectionTimeoutSeconds { get; set; } = DefaultConnectionTimeoutSeconds;
    public int BackupRetentionDays { get; set; } = DefaultBackupRetentionDays;
    public bool EnableEncryption { get; set; } = DefaultEnableEncryption;
    public string BackupDirectory { get; set; } = DefaultBackupDirectory;
    public string DatabaseDirectory { get; set; } = DefaultDatabaseDirectory;
    public bool EnableLogging { get; set; } = DefaultEnableLogging;

    /// <summary>
    /// Gets or sets the maximum number of items allowed in a single batch operation.
    /// Defaults to 500 to prevent resource exhaustion attacks.
    /// Set to 0 or negative to disable the limit.
    /// </summary>
    public int MaxBatchItems { get; set; } = DefaultMaxBatchItems;

    /// <summary>
    /// Gets or sets the maximum total payload size in bytes for batch operation parameters.
    /// Defaults to 1 MB to prevent memory exhaustion from large parameter payloads.
    /// Set to 0 or negative to disable the limit.
    /// </summary>
    public long MaxBatchPayloadSizeBytes { get; set; } = DefaultMaxBatchPayloadSizeBytes;

    /// <summary>
    /// Returns a concise, informative representation of the options.
    /// </summary>
    public override string ToString()
        => $"SqliteMultiTenantOptions {{ MaxConnections = {MaxConnections}, ConnectionTimeoutSeconds = {ConnectionTimeoutSeconds}, BackupRetentionDays = {BackupRetentionDays}, EnableEncryption = {EnableEncryption}, BackupDirectory = {BackupDirectory}, DatabaseDirectory = {DatabaseDirectory} }}";
}
