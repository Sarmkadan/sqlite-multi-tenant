#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Security;

namespace SqliteMultiTenant.Tenants
{
    /// <summary>
    /// Handles the complete lifecycle of tenant database provisioning.
    /// Creates isolated SQLite databases for each tenant with schema initialization,
    /// supports cloning for replication, and manages deprovisioning with cleanup.
    /// </summary>
    public sealed class TenantProvisioner {
        private readonly ITenantRepository _tenantRepository;
        private readonly SchemaManager _schemaManager;
        private readonly ILogger<TenantProvisioner> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _basePath;

        public TenantProvisioner(ITenantRepository tenantRepository, SchemaManager schemaManager,
            ILogger<TenantProvisioner> logger, string basePath, ILoggerFactory? loggerFactory = null)
        {
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
            _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
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
            TenantSettings settings = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentException("Tenant name cannot be empty", nameof(tenantName));

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
        /// Clones an existing tenant database by copying the SQLite file to a new tenant directory.
        /// Useful for backup, testing, or replication scenarios.
        /// </summary>
        /// <param name="sourceTenantId">ID of the tenant to clone from.</param>
        /// <param name="targetTenantId">ID for the new cloned tenant.</param>
        /// <returns>The filesystem path to the cloned database file.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the source tenant is not found.</exception>
        public async Task<string> CloneTenantAsync(string sourceTenantId, string targetTenantId)
        {
            if (string.IsNullOrWhiteSpace(sourceTenantId))
                throw new ArgumentException("Source tenant ID cannot be empty", nameof(sourceTenantId));

            if (string.IsNullOrWhiteSpace(targetTenantId))
                throw new ArgumentException("Target tenant ID cannot be empty", nameof(targetTenantId));

            try
            {
                var sourceTenant = await _tenantRepository.GetByIdAsync(sourceTenantId);
                if (sourceTenant is null)
                    throw new InvalidOperationException($"Source tenant {sourceTenantId} not found");

                var targetDir = Path.Combine(_basePath, targetTenantId);
                Directory.CreateDirectory(targetDir);

                var sourceDbPath = sourceTenant.DatabasePath;
                var targetDbPath = Path.Combine(targetDir, $"{targetTenantId}.db");

                // Copy database file
                File.Copy(sourceDbPath, targetDbPath, overwrite: true);

                _logger.LogInformation("Tenant cloned from {SourceId} to {TargetId}",
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
        /// Deprovisions a tenant by removing the database file, tenant directory,
        /// and metadata record. This operation is irreversible.
        /// </summary>
        /// <param name="tenantId">ID of the tenant to deprovision.</param>
        /// <param name="deleteBackups">When <c>true</c>, also removes backup files for this tenant.</param>
        /// <returns><c>true</c> if the tenant was found and removed; <c>false</c> if the tenant was not found.</returns>
        public async Task<bool> DeprovisionTenantAsync(string tenantId, bool deleteBackups = false)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

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
                throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

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

                        var count = (long)await command.ExecuteScalarAsync();
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
        /// SQLCipher passphrase used to encrypt the database file.  Must not be null or empty.
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
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(tenantName))
                throw new ArgumentException("Tenant name cannot be empty", nameof(tenantName));

            if (string.IsNullOrWhiteSpace(encryptionKey))
                throw new ArgumentException("Encryption key cannot be empty", nameof(encryptionKey));

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
    }
}
