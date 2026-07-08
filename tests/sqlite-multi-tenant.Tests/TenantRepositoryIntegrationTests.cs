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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantRepositoryIntegrationTests : IDisposable {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly TenantRepository _tenantRepository;

        public TenantRepositoryIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"tenant_repo_tests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";

            _tenantRepository = new TenantRepository(_connectionString, NullLogger<TenantRepository>.Instance);

            SeedData();
        }

        private void SeedData()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            InsertTenant(connection, "repo-tenant-a", "RepositoryTenantA", "repo_tenantA.db");
            InsertTenant(connection, "repo-tenant-b", "RepositoryTenantB", "repo_tenantB.db");
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
        public async Task GetAllAsync_ShouldReturnAllTenants()
        {
            // Act
            var tenants = await _tenantRepository.GetAllAsync();

            // Assert
            tenants.Should().NotBeNull();
            tenants.Should().HaveCount(2);
            tenants.Should().Contain(t => t.Name == "RepositoryTenantA");
            tenants.Should().Contain(t => t.Name == "RepositoryTenantB");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectTenant_WhenTenantExists()
        {
            // Arrange
            var tenantId = "repo-tenant-a";

            // Act
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            // Assert
            tenant.Should().NotBeNull();
            tenant!.Name.Should().Be("RepositoryTenantA");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenTenantDoesNotExist()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid().ToString();

            // Act
            var tenant = await _tenantRepository.GetByIdAsync(nonExistingId);

            // Assert
            tenant.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddTenantToDatabase()
        {
            // Arrange
            var newTenant = new Tenant { TenantId = "repo-tenant-c", Name = "RepositoryTenantC", DatabasePath = "repo_tenantC.db" };

            // Act
            var addedTenant = await _tenantRepository.AddAsync(newTenant);

            // Assert
            addedTenant.Should().NotBeNull();
            addedTenant.Name.Should().Be("RepositoryTenantC");

            var tenantInDb = await _tenantRepository.GetByIdAsync(newTenant.TenantId);
            tenantInDb.Should().NotBeNull();
            tenantInDb!.Name.Should().Be("RepositoryTenantC");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTenantInDatabase()
        {
            // Arrange
            var tenantToUpdate = await _tenantRepository.GetByIdAsync("repo-tenant-a");
            tenantToUpdate.Should().NotBeNull();
            tenantToUpdate!.DatabasePath = "updated_repo_tenantA.db";

            // Act
            await _tenantRepository.UpdateAsync(tenantToUpdate);

            // Assert
            var tenantInDb = await _tenantRepository.GetByIdAsync("repo-tenant-a");
            tenantInDb.Should().NotBeNull();
            tenantInDb!.DatabasePath.Should().Be("updated_repo_tenantA.db");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTenantFromDatabase()
        {
            // Arrange
            var tenantIdToDelete = "repo-tenant-b";

            // Act
            await _tenantRepository.DeleteAsync(tenantIdToDelete);

            // Assert
            var tenantInDb = await _tenantRepository.GetByIdAsync(tenantIdToDelete);
            tenantInDb.Should().BeNull();
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
