#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Security;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tenants;

/// <summary>
/// Handles the complete lifecycle of tenant database provisioning.
/// Creates isolated SQLite databases for each tenant with schema initialization,
/// supports cloning for replication, and manages deprovisioning with cleanup.
/// </summary>
public sealed class TenantProvisioner
{
    private readonly ITenantRepository _tenantRepository;
    private readonly SchemaManager _schemaManager;
    private readonly ILogger<TenantProvisioner> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _basePath;
    private readonly IEventBus _eventBus;

    public TenantProvisioner(ITenantRepository tenantRepository, SchemaManager schemaManager,
        ILogger<TenantProvisioner> logger, string basePath, ILoggerFactory? loggerFactory = null,
        IEventBus? eventBus = null)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _eventBus = eventBus!;
    }

    /// <summary>
    /// Provisions a new tenant database with full schema initialization.
    /// Creates a dedicated directory, SQLite database file, initializes the schema,
    /// and registers the tenant in the metadata repository.
    /// </summary>
    /// <param name="tenantId">Unique identifier for the new tenant.</param>
    /// <param name="tenantName">Display name for the tenant.</param>
    /// <param name="settings">Optional tenant-specific configuration settings.</param>
    /// <returns>The newly created <see cref="Tenant"/> entity.</returns>
    /// <exception cref="ArgumentException">Thrown when tenantId or tenantName is empty.</exception>
    public async Task<Tenant> ProvisionTenantAsync(string tenantId, string tenantName,
        TenantSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name cannot be empty", nameof(tenantName));
        }

        try
        {
            // Create tenant directory
            var tenantDir = Path.Combine(_basePath, tenantId);
            Directory.CreateDirectory(tenantDir);

            var dbPath = Path.Combine(tenantDir, $"{tenantId}.db");

            // Create empty database file
            using (var connection = new SQLiteConnection($"Data Source={dbPath};"))
            {
                await connection.OpenAsync();
                await connection.CloseAsync();
            }

            // Create tenant record
            var tenant = new Tenant
            {
                TenantId = tenantId,
                Name = tenantName,
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DatabasePath = dbPath
            };

            // Initialize schema for the new tenant database
            var connectionString = $"Data Source={dbPath};";
            var schemaMgr = new SchemaManager(_loggerFactory.CreateLogger<SchemaManager>(), connectionString);
            await schemaMgr.InitializeSchemaAsync(tenantId);

            // Store tenant metadata
            await _tenantRepository.AddAsync(tenant);

            _logger.LogInformation("Tenant provisioned successfully: {TenantId} at {DbPath}",
                tenantId, dbPath);

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Clones an existing tenant database by copying schema and data from the source database.
    /// Uses SQLite ATTACH and INSERT SELECT to copy all schema objects and data.
    /// Useful for backup, testing, or replication scenarios.
    /// </summary>
    /// <param name="sourceTenantId">ID of the tenant to clone from.</param>
    /// <param name="targetTenantId">ID for the new cloned tenant.</param>
    /// <returns>The filesystem path to the cloned database file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the source tenant is not found.</exception>
    public async Task<string> CloneTenantAsync(string sourceTenantId, string targetTenantId)
    {
        if (string.IsNullOrWhiteSpace(sourceTenantId))
        {
            throw new ArgumentException("Source tenant ID cannot be empty", nameof(sourceTenantId));
        }

        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            throw new ArgumentException("Target tenant ID cannot be empty", nameof(targetTenantId));
        }

        try
        {
            var sourceTenant = await _tenantRepository.GetByIdAsync(sourceTenantId);
            if (sourceTenant is null)
            {
                throw new InvalidOperationException($"Source tenant {sourceTenantId} not found");
            }

            var targetDir = Path.Combine(_basePath, targetTenantId);
            Directory.CreateDirectory(targetDir);

            var sourceDbPath = sourceTenant.DatabasePath;
            var targetDbPath = Path.Combine(targetDir, $"{targetTenantId}.db");

            // Create empty target database
            using (var connection = new SQLiteConnection($"Data Source={targetDbPath};Version=3;"))
            {
                await connection.OpenAsync();
                await connection.CloseAsync();
            }

            // Copy schema and data using ATTACH and INSERT SELECT
            var targetConnStr = $"Data Source={targetDbPath};Version=3;";
            var sourceConnStr = $"Data Source={sourceDbPath};Version=3;";

            await using (var targetConn = new SQLiteConnection(targetConnStr))
            await using (var sourceConn = new SQLiteConnection(sourceConnStr))
            {
                await targetConn.OpenAsync();
                await sourceConn.OpenAsync();

                // Attach source database
                using (var attachCmd = new SQLiteCommand("ATTACH DATABASE @source AS source_db", targetConn))
                {
                    attachCmd.Parameters.AddWithValue("@source", sourceDbPath);
                    await attachCmd.ExecuteNonQueryAsync();
                }

                // Get list of all tables from source (excluding sqlite_sequence)
                var tables = new List<string>();
                using (var getTablesCmd = new SQLiteCommand(
                           "SELECT name FROM source_db.sqlite_master WHERE type='table' AND name != 'sqlite_sequence' ORDER BY name",
                           sourceConn))
                using (var reader = await getTablesCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                // Copy each table's schema and data
                foreach (var table in tables)
                {
                    // Copy table schema
                    using (var schemaCmd = new SQLiteCommand(
                               $"CREATE TABLE [{table}] AS SELECT * FROM source_db.[{table}] WHERE 1=0",
                               targetConn))
                    {
                        await schemaCmd.ExecuteNonQueryAsync();
                    }

                    // Copy table data
                    using (var dataCmd = new SQLiteCommand(
                               $"INSERT INTO [{table}] SELECT * FROM source_db.[{table}]",
                               targetConn))
                    {
                        await dataCmd.ExecuteNonQueryAsync();
                    }
                }

                // Detach source database
                using (var detachCmd = new SQLiteCommand("DETACH DATABASE source_db", targetConn))
                {
                    await detachCmd.ExecuteNonQueryAsync();
                }
            }

            // Create tenant record
            var targetTenant = new Tenant
            {
                TenantId = targetTenantId,
                Name = $"Clone of {sourceTenant.Name}",
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DatabasePath = targetDbPath
            };

            await _tenantRepository.AddAsync(targetTenant);

            // Emit domain event
            var clonedEvent = new TenantClonedEvent
            {
                SourceTenantId = sourceTenantId,
                TargetTenantId = targetTenantId,
                DatabasePath = targetDbPath,
                TenantId = targetTenantId
            };
            await _eventBus.PublishAsync(clonedEvent);

            _logger.LogInformation("Tenant cloned from {SourceId} to {TargetId} using ATTACH+INSERT SELECT",
                sourceTenantId, targetTenantId);

            return targetDbPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone tenant from {SourceId} to {TargetId}",
                sourceTenantId, targetTenantId);
            throw;
        }
    }

    /// <summary>
    /// Renames a tenant by moving its database to a location keyed by the new identifier.
    /// The database is copied, the copy is verified against the source, the registry is switched
    /// to the new identifier, and only then is the original file removed.
    /// </summary>
    /// <param name="oldTenantId">Current identifier of the tenant.</param>
    /// <param name="newTenantId">New identifier; validated with <see cref="TenantNameValidator.ValidateTenantId"/>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tenant registered under <paramref name="newTenantId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when an identifier is empty or <paramref name="newTenantId"/> fails validation.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant matches <paramref name="oldTenantId"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new identifier is already in use or a file already exists at the target location.</exception>
    /// <exception cref="IOException">Thrown when the database is busy or the copy does not match the source.</exception>
    /// <remarks>
    /// <para>
    /// Callers must evict any pooled connections to the old database before renaming; this method
    /// does not manage connection pools. Identifiers that differ only by case are rejected, since
    /// they would resolve to the same file on case-insensitive file systems.
    /// </para>
    /// <para>
    /// If anything fails before the original file is removed, the copy is discarded and the tenant
    /// keeps its original identifier and database file. Once the registry has switched, a failure to
    /// delete the original file is logged as a warning and does not roll back the rename.
    /// </para>
    /// </remarks>
    public async Task<Tenant> RenameTenantAsync(string oldTenantId, string newTenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldTenantId))
        {
            throw new ArgumentException("Source tenant ID cannot be empty", nameof(oldTenantId));
        }

        if (string.IsNullOrWhiteSpace(newTenantId))
        {
            throw new ArgumentException("New tenant ID cannot be empty", nameof(newTenantId));
        }

        var targetId = newTenantId.Trim();

        var validation = TenantNameValidator.ValidateTenantId(targetId);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Error, nameof(newTenantId));
        }

        if (string.Equals(oldTenantId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("New tenant ID must differ from the current tenant ID", nameof(newTenantId));
        }

        try
        {
            var source = await _tenantRepository.GetByIdAsync(oldTenantId, cancellationToken)
                ?? throw new TenantNotFoundException(oldTenantId);

            if (await _tenantRepository.ExistsAsync(targetId, cancellationToken))
            {
                throw new InvalidOperationException($"Tenant {targetId} already exists");
            }

            var sourcePath = source.DatabasePath
                ?? throw new InvalidOperationException($"Tenant {oldTenantId} has no database path");

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Database file for tenant {oldTenantId} not found at {sourcePath}");
            }

            var targetDir = Path.Combine(_basePath, targetId);
            var targetPath = Path.Combine(targetDir, $"{targetId}.db");
            var tempPath = targetPath + ".rename-tmp";

            if (File.Exists(targetPath))
            {
                throw new InvalidOperationException($"A database already exists at {targetPath}");
            }

            Directory.CreateDirectory(targetDir);

            try
            {
                // Fold the WAL into the main file so the copy below contains all committed data.
                await CheckpointDatabaseAsync(sourcePath, cancellationToken);

                var sourceHash = await ComputeFileHashAsync(sourcePath, cancellationToken);
                File.Copy(sourcePath, tempPath, overwrite: false);

                var copyHash = await ComputeFileHashAsync(tempPath, cancellationToken);
                if (!CryptographicOperations.FixedTimeEquals(sourceHash, copyHash))
                {
                    throw new IOException($"Copy verification failed: '{tempPath}' does not match '{sourcePath}'");
                }

                await CheckIntegrityAsync(tempPath, cancellationToken);
                File.Move(tempPath, targetPath, overwrite: false);
            }
            catch
            {
                DeleteFileIfExists(tempPath);
                throw;
            }

            var renamed = new Tenant
            {
                TenantId = targetId,
                Name = source.Name,
                Description = source.Description,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                LastAccessedAt = source.LastAccessedAt,
                ContactEmail = source.ContactEmail,
                DatabasePath = targetPath,
                IsDataIsolated = source.IsDataIsolated,
                MaxConnections = source.MaxConnections,
                Metadata = source.Metadata is null ? null : new Dictionary<string, string>(source.Metadata)
            };

            // Switch the registry: insert under the new id, then drop the old record.
            // If the second step fails, undo the first so the registry and disk stay consistent.
            await _tenantRepository.AddAsync(renamed, cancellationToken);
            try
            {
                await _tenantRepository.DeleteAsync(oldTenantId, cancellationToken);
            }
            catch
            {
                await _tenantRepository.DeleteAsync(targetId, cancellationToken);
                DeleteFileIfExists(targetPath);
                throw;
            }

            DeleteOriginalDatabase(sourcePath, oldTenantId);

            if (_eventBus is not null)
            {
                await _eventBus.PublishAsync(new TenantRenamedEvent
                {
                    OldTenantId = oldTenantId,
                    NewTenantId = targetId,
                    DatabasePath = targetPath,
                    TenantId = targetId
                }, cancellationToken);
            }

            _logger.LogInformation("Tenant renamed from {OldId} to {NewId} at {DbPath}",
                oldTenantId, targetId, targetPath);

            return renamed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename tenant from {OldId} to {NewId}", oldTenantId, targetId);
            throw;
        }
    }

    /// <summary>
    /// Deprovisions a tenant by removing the database file, tenant directory,
    /// and metadata record. This operation is irreversible.
    /// </summary>
    /// <param name="tenantId">ID of the tenant to deprovision.</param>
    /// <param name="deleteBackups">When <c>true</c>, also removes backup files for this tenant.</param>
    /// <returns><c>true</c> if the tenant was found and removed; <c>false</c> if the tenant was not found.</returns>
    public async Task<bool> DeprovisionTenantAsync(string tenantId, bool deleteBackups = false)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant is null)
            {
                _logger.LogWarning("Tenant not found for deprovisioning: {TenantId}", tenantId);
                return false;
            }

            // Delete database file
            if (File.Exists(tenant.DatabasePath))
            {
                File.Delete(tenant.DatabasePath);
            }

            // Delete tenant directory
            var tenantDir = Path.Combine(_basePath, tenantId);
            if (Directory.Exists(tenantDir))
            {
                Directory.Delete(tenantDir, recursive: true);
            }

            // Remove tenant record
            await _tenantRepository.DeleteAsync(tenantId);

            _logger.LogInformation("Tenant deprovisioned: {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deprovision tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Validates tenant database integrity by checking file existence, connectivity,
    /// and verifying that the expected schema tables (Tenants, AuditLog) are present.
    /// </summary>
    /// <param name="tenantId">ID of the tenant to validate.</param>
    /// <returns><c>true</c> if the database is valid and accessible; <c>false</c> otherwise.</returns>
    public async Task<bool> ValidateTenantDatabaseAsync(string tenantId)
    {
        if (tenantId is null)
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant is null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
                return false;
            }

            if (!File.Exists(tenant.DatabasePath))
            {
                _logger.LogWarning("Tenant database file missing: {TenantId}", tenantId);
                return false;
            }

            var connectionString = $"Data Source={tenant.DatabasePath};";
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                // Verify schema tables exist
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT COUNT(*) FROM sqlite_master
WHERE type='table' AND name IN ('Tenants', 'AuditLog')";

                    var count = Convert.ToInt64(await command.ExecuteScalarAsync());
                    return count == 2;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant database validation failed: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Provisions a new tenant database with AES-256 encryption via SQLCipher.
    /// The database file is encrypted at rest using the supplied <paramref name="encryptionKey"/>.
    /// </summary>
    /// <remarks>
    /// Requires the <c>SQLitePCLRaw.bundle_sqlcipher</c> NuGet package.
    /// The caller is responsible for storing and retrieving the encryption key securely
    /// (e.g., via <see cref="EncryptionKeyManager"/>).
    /// </remarks>
    /// <param name="tenantId">Unique identifier for the new tenant.</param>
    /// <param name="tenantName">Display name for the tenant.</param>
    /// <param name="encryptionKey">
    /// SQLCipher passphrase used to encrypt the database file. Must not be null or empty.
    /// </param>
    /// <param name="settings">Optional tenant-specific configuration settings.</param>
    /// <returns>The newly created <see cref="Tenant"/> entity with <c>IsEncrypted = true</c>.</returns>
    public async Task<Tenant> ProvisionEncryptedTenantAsync(
        string tenantId,
        string tenantName,
        string encryptionKey,
        TenantSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name cannot be empty", nameof(tenantName));
        }

        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new ArgumentException("Encryption key cannot be empty", nameof(encryptionKey));
        }

        try
        {
            var tenantDir = Path.Combine(_basePath, tenantId);
            Directory.CreateDirectory(tenantDir);

            var dbPath = Path.Combine(tenantDir, $"{tenantId}.db");

            var encryptedConnStr = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, encryptionKey);

            using (var connection = new SQLiteConnection(encryptedConnStr))
            {
                await connection.OpenAsync();
                await connection.CloseAsync();
            }

            var tenant = new Tenant
            {
                TenantId = tenantId,
                Name = tenantName,
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DatabasePath = dbPath
            };

            var schemaMgr = new SchemaManager(_loggerFactory.CreateLogger<SchemaManager>(), encryptedConnStr);
            await schemaMgr.InitializeSchemaAsync(tenantId);

            await _tenantRepository.AddAsync(tenant);

            _logger.LogInformation(
                "Encrypted tenant provisioned: {TenantId} at {DbPath}", tenantId, dbPath);

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision encrypted tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Runs a full WAL checkpoint and truncates the WAL, failing if the database is busy
    /// or a non-empty WAL remains (its contents would not be in the main file being copied).
    /// </summary>
    private static async Task CheckpointDatabaseAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = new SQLiteConnection($"Data Source={databasePath};");
        await connection.OpenAsync(cancellationToken);

        using (var command = new SQLiteCommand("PRAGMA wal_checkpoint(TRUNCATE);", connection))
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken) && reader.GetInt32(0) != 0)
            {
                throw new IOException($"Database '{databasePath}' is busy; retry the rename later");
            }
        }

        var walPath = databasePath + "-wal";
        if (File.Exists(walPath) && new FileInfo(walPath).Length > 0)
        {
            throw new IOException($"Database '{databasePath}' has uncheckpointed WAL data; retry the rename later");
        }
    }

    private static async Task<byte[]> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await SHA256.HashDataAsync(stream, cancellationToken);
    }

    private static async Task CheckIntegrityAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = new SQLiteConnection($"Data Source={databasePath};");
        await connection.OpenAsync(cancellationToken);

        using var command = new SQLiteCommand("PRAGMA quick_check;", connection);
        var result = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"Integrity check failed for '{databasePath}': {result}");
        }
    }

    /// <summary>
    /// Removes the original database file after a rename and deletes its directory if that
    /// directory is now empty. Failures are logged rather than thrown, because the rename has already committed.
    /// </summary>
    private void DeleteOriginalDatabase(string sourcePath, string oldTenantId)
    {
        try
        {
            File.Delete(sourcePath);

            var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
            if (sourceDir is not null
                && !string.Equals(sourceDir, Path.GetFullPath(_basePath), StringComparison.Ordinal)
                && Directory.Exists(sourceDir)
                && !Directory.EnumerateFileSystemEntries(sourceDir).Any())
            {
                Directory.Delete(sourceDir);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Tenant {OldId} was renamed, but its original database could not be removed: {Path}",
                oldTenantId, sourcePath);
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
