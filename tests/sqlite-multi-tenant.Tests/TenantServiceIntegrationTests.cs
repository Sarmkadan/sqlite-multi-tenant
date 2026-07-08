#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantServiceIntegrationTests : IDisposable {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly ILogger<TenantService> _logger;
        private readonly ITenantRepository _tenantRepository;
        private readonly TenantService _tenantService;

        public TenantServiceIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"tenant_service_tests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";

            _logger = NullLogger<TenantService>.Instance;
            _tenantRepository = new TenantRepository(_connectionString, NullLogger<TenantRepository>.Instance); // Use concrete repository for integration

            SeedData();

            _tenantService = new TenantService(_tenantRepository, _logger);
        }

        private void SeedData()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            InsertTenant(connection, "tenant-a", "TenantA", "tenantA.db");
            InsertTenant(connection, "tenant-b", "TenantB", "tenantB.db");
        }

        private static void InsertTenant(SQLiteConnection connection, string tenantId, string name, string databasePath)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tenants (TenantId, Name, Status, CreatedAt, UpdatedAt, DatabasePath, IsDataIsolated, MaxConnections)
                VALUES (@TenantId, @Name, 0, @CreatedAt, @CreatedAt, @DatabasePath, 1, 10)";
            command.Parameters.AddWithValue("@TenantId", tenantId);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@DatabasePath", databasePath);
            command.ExecuteNonQuery();
        }

        [Fact]
        public async Task GetAllTenantsAsync_ShouldReturnAllSeededTenants()
        {
            // Act
            var tenants = await _tenantService.GetAllTenantsAsync();

            // Assert
            tenants.Should().NotBeNull();
            tenants.Should().HaveCount(2);
            tenants.Should().Contain(t => t.Name == "TenantA");
            tenants.Should().Contain(t => t.Name == "TenantB");
        }

        [Fact]
        public async Task GetTenantAsync_ShouldReturnCorrectTenant()
        {
            // Act
            var tenant = await _tenantService.GetTenantAsync("tenant-a");

            // Assert
            tenant.Should().NotBeNull();
            tenant!.Name.Should().Be("TenantA");
        }

        [Fact]
        public async Task GetTenantAsync_ShouldReturnNullForNonExistingTenant()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid().ToString();

            // Act
            var tenant = await _tenantService.GetTenantAsync(nonExistingId);

            // Assert
            tenant.Should().BeNull();
        }

        [Fact]
        public async Task CreateTenantAsync_ShouldAddTenantToDatabase()
        {
            // Act
            var createdTenant = await _tenantService.CreateTenantAsync("TenantC");

            // Assert
            createdTenant.Should().NotBeNull();
            createdTenant.Name.Should().Be("TenantC");

            var tenantInDb = await _tenantRepository.GetByIdAsync(createdTenant.TenantId);
            tenantInDb.Should().NotBeNull();
            tenantInDb!.Name.Should().Be("TenantC");
        }

        [Fact]
        public async Task UpdateTenantAsync_ShouldUpdateTenantInDatabase()
        {
            // Arrange
            var tenantToUpdate = await _tenantRepository.GetByIdAsync("tenant-a");
            tenantToUpdate.Should().NotBeNull();
            tenantToUpdate!.DatabasePath = "updatedTenantA.db";

            // Act
            await _tenantService.UpdateTenantAsync(tenantToUpdate);

            // Assert
            var tenantInDb = await _tenantRepository.GetByIdAsync("tenant-a");
            tenantInDb.Should().NotBeNull();
            tenantInDb!.DatabasePath.Should().Be("updatedTenantA.db");
        }

        [Fact]
        public async Task DeleteTenantAsync_ShouldRemoveTenantFromDatabase()
        {
            // Act
            await _tenantService.DeleteTenantAsync("tenant-b");

            // Assert
            var tenantInDb = await _tenantRepository.GetByIdAsync("tenant-b");
            tenantInDb.Should().BeNull();
        }

        [Fact]
        public async Task CreateTenantAsync_ShouldThrowExceptionIfTenantNameAlreadyExists()
        {
            // Arrange
            var existingTenantName = "TenantA";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _tenantService.CreateTenantAsync(existingTenantName));
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
    }
}
