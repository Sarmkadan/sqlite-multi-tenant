// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Tests
{
    public class TenantRepositoryIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TenantContext> _dbContextOptions;
        private readonly TenantRepository _tenantRepository;

        public TenantRepositoryIntegrationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbContextOptions = new DbContextOptionsBuilder<TenantContext>()
                .UseSqlite(_connection)
                .Options;

            using (var context = new TenantContext(_dbContextOptions))
            {
                context.Database.EnsureCreated();
                SeedData(context);
            }

            _tenantRepository = new TenantRepository(new TenantContext(_dbContextOptions));
        }

        private void SeedData(TenantContext context)
        {
            if (!context.Tenants.Any())
            {
                context.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "RepositoryTenantA", ConnectionString = "DataSource=repo_tenantA.db" });
                context.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "RepositoryTenantB", ConnectionString = "DataSource=repo_tenantB.db" });
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTenants()
        {
            // Arrange
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
            Guid tenantId;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantId = context.Tenants.First(t => t.Name == "RepositoryTenantA").Id;
            }

            // Act
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            // Assert
            tenant.Should().NotBeNull();
            tenant.Name.Should().Be("RepositoryTenantA");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenTenantDoesNotExist()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var tenant = await _tenantRepository.GetByIdAsync(nonExistingId);

            // Assert
            tenant.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddTenantToDatabase()
        {
            // Arrange
            var newTenant = new Tenant { Id = Guid.NewGuid(), Name = "RepositoryTenantC", ConnectionString = "DataSource=repo_tenantC.db" };

            // Act
            var addedTenant = await _tenantRepository.AddAsync(newTenant);

            // Assert
            addedTenant.Should().NotBeNull();
            addedTenant.Name.Should().Be("RepositoryTenantC");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == newTenant.Id);
                tenantInDb.Should().NotBeNull();
                tenantInDb.Name.Should().Be("RepositoryTenantC");
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTenantInDatabase()
        {
            // Arrange
            Tenant tenantToUpdate;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantToUpdate = await context.Tenants.FirstAsync(t => t.Name == "RepositoryTenantA");
                tenantToUpdate.ConnectionString = "DataSource=updated_repo_tenantA.db";
            }

            // Act
            var updatedTenant = await _tenantRepository.UpdateAsync(tenantToUpdate);

            // Assert
            updatedTenant.Should().NotBeNull();
            updatedTenant.ConnectionString.Should().Be("DataSource=updated_repo_tenantA.db");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantToUpdate.Id);
                tenantInDb.Should().NotBeNull();
                tenantInDb.ConnectionString.Should().Be("DataSource=updated_repo_tenantA.db");
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTenantFromDatabase()
        {
            // Arrange
            Guid tenantIdToDelete;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantIdToDelete = context.Tenants.First(t => t.Name == "RepositoryTenantB").Id;
            }

            // Act
            await _tenantRepository.DeleteAsync(tenantIdToDelete);

            // Assert
            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantIdToDelete);
                tenantInDb.Should().BeNull();
            }
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
