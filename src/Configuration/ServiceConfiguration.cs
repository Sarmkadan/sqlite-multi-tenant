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
            throw new ArgumentException("Connection string cannot be empty", nameof(masterConnectionString));

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
            throw new ArgumentNullException(nameof(configureOptions));

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
    public int MaxConnections { get; set; } = 10;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int BackupRetentionDays { get; set; } = 30;
    public bool EnableEncryption { get; set; } = false;
    public string BackupDirectory { get; set; } = "backups";
    public string DatabaseDirectory { get; set; } = "databases";
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items allowed in a single batch operation.
    /// Defaults to 500 to prevent resource exhaustion attacks.
    /// Set to 0 or negative to disable the limit.
    /// </summary>
    public int MaxBatchItems { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum total payload size in bytes for batch operation parameters.
    /// Defaults to 1 MB to prevent memory exhaustion from large parameter payloads.
    /// Set to 0 or negative to disable the limit.
    /// </summary>
    public long MaxBatchPayloadSizeBytes { get; set; } = 1024 * 1024; // 1 MB

    /// <summary>
    /// Returns a concise, informative representation of the options.
    /// </summary>
    public override string ToString()
        => $"SqliteMultiTenantOptions {{ MaxConnections = {MaxConnections}, ConnectionTimeoutSeconds = {ConnectionTimeoutSeconds}, BackupRetentionDays = {BackupRetentionDays}, EnableEncryption = {EnableEncryption}, BackupDirectory = {BackupDirectory}, DatabaseDirectory = {DatabaseDirectory} }}";
}
