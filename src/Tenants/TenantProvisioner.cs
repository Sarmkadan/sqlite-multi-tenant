// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tenants
{
    // Handles the complete lifecycle of tenant database provisioning
    // Creates isolated SQLite databases for each tenant with proper initialization
    public class TenantProvisioner
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly SchemaManager _schemaManager;
        private readonly ILogger<TenantProvisioner> _logger;
        private readonly string _basePath;

        public TenantProvisioner(ITenantRepository tenantRepository, SchemaManager schemaManager,
            ILogger<TenantProvisioner> logger, string basePath)
        {
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        // Provisions a new tenant database with schema initialization
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
                    Id = tenantId,
                    Name = tenantName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DatabasePath = dbPath
                };

                // Initialize schema for the new tenant database
                var connectionString = $"Data Source={dbPath};";
                var schemaMgr = new SchemaManager(_logger, connectionString);
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

        // Clones an existing tenant database for backup or replication
        public async Task<string> CloneTenantAsync(string sourceTenantId, string targetTenantId)
        {
            if (string.IsNullOrWhiteSpace(sourceTenantId))
                throw new ArgumentException("Source tenant ID cannot be empty", nameof(sourceTenantId));

            if (string.IsNullOrWhiteSpace(targetTenantId))
                throw new ArgumentException("Target tenant ID cannot be empty", nameof(targetTenantId));

            try
            {
                var sourceTenant = await _tenantRepository.GetByIdAsync(sourceTenantId);
                if (sourceTenant == null)
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

        // Deprovisiones a tenant and cleans up all associated resources
        public async Task<bool> DeprovisionTenantAsync(string tenantId, bool deleteBackups = false)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant == null)
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

        // Validates tenant database integrity and accessibility
        public async Task<bool> ValidateTenantDatabaseAsync(string tenantId)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (tenant == null)
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
    }
}
