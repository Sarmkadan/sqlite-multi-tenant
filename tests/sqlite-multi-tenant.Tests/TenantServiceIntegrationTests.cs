#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentAssertions;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Configuration;
using Microsoft.Extensions.Options;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantServiceIntegrationTests : IDisposable {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TenantContext> _dbContextOptions;
        private readonly ILogger<TenantService> _logger;
        private readonly ITenantRepository _tenantRepository;
        private readonly TenantService _tenantService;
        private readonly IOptionsSnapshot<MultiTenantOptions> _multiTenantOptions;

        public TenantServiceIntegrationTests()
        {
            // Setup in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbContextOptions = new DbContextOptionsBuilder<TenantContext>()
                .UseSqlite(_connection)
                .Options;

            // Ensure the database is created and seeded
            using (var context = new TenantContext(_dbContextOptions))
            {
                context.Database.EnsureCreated();
                SeedData(context);
            }

            _logger = Substitute.For<ILogger<TenantService>>();
            _tenantRepository = new TenantRepository(new TenantContext(_dbContextOptions)); // Use concrete repository for integration
            _multiTenantOptions = Substitute.For<IOptionsSnapshot<MultiTenantOptions>>();
            _multiTenantOptions.Value.Returns(new MultiTenantOptions { DefaultSchema = "public" });

            _tenantService = new TenantService(_logger, _tenantRepository, _multiTenantOptions);
        }

        private void SeedData(TenantContext context)
        {
            if (!context.Tenants.Any())
            {
                context.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "TenantA", ConnectionString = "DataSource=tenantA.db" });
                context.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "TenantB", ConnectionString = "DataSource=tenantB.db" });
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAllTenantsAsync_ShouldReturnAllSeededTenants()
        {
            // Arrange
            // Act
            var tenants = await _tenantService.GetAllTenantsAsync();

            // Assert
            tenants.Should().NotBeNull();
            tenants.Should().HaveCount(2);
            tenants.Should().Contain(t => t.Name == "TenantA");
            tenants.Should().Contain(t => t.Name == "TenantB");
        }

        [Fact]
        public async Task GetTenantByIdAsync_ShouldReturnCorrectTenant()
        {
            // Arrange
            Guid tenantAId;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantAId = context.Tenants.First(t => t.Name == "TenantA").Id;
            }

            // Act
            var tenant = await _tenantService.GetTenantByIdAsync(tenantAId);

            // Assert
            tenant.Should().NotBeNull();
            tenant.Name.Should().Be("TenantA");
        }

        [Fact]
        public async Task GetTenantByIdAsync_ShouldReturnNullForNonExistingTenant()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var tenant = await _tenantService.GetTenantByIdAsync(nonExistingId);

            // Assert
            tenant.Should().BeNull();
        }

        [Fact]
        public async Task CreateTenantAsync_ShouldAddTenantToDatabase()
        {
            // Arrange
            var newTenant = new Tenant { Id = Guid.NewGuid(), Name = "TenantC", ConnectionString = "DataSource=tenantC.db" };

            // Act
            var createdTenant = await _tenantService.CreateTenantAsync(newTenant);

            // Assert
            createdTenant.Should().NotBeNull();
            createdTenant.Name.Should().Be("TenantC");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == newTenant.Id);
                tenantInDb.Should().NotBeNull();
                tenantInDb.Name.Should().Be("TenantC");
            }
        }

        [Fact]
        public async Task UpdateTenantAsync_ShouldUpdateTenantInDatabase()
        {
            // Arrange
            Tenant tenantToUpdate;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantToUpdate = await context.Tenants.FirstAsync(t => t.Name == "TenantA");
                tenantToUpdate.ConnectionString = "DataSource=updatedTenantA.db";
            }

            // Act
            var updatedTenant = await _tenantService.UpdateTenantAsync(tenantToUpdate);

            // Assert
            updatedTenant.Should().NotBeNull();
            updatedTenant.ConnectionString.Should().Be("DataSource=updatedTenantA.db");

            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantToUpdate.Id);
                tenantInDb.Should().NotBeNull();
                tenantInDb.ConnectionString.Should().Be("DataSource=updatedTenantA.db");
            }
        }

        [Fact]
        public async Task DeleteTenantAsync_ShouldRemoveTenantFromDatabase()
        {
            // Arrange
            Guid tenantBId;
            using (var context = new TenantContext(_dbContextOptions))
            {
                tenantBId = context.Tenants.First(t => t.Name == "TenantB").Id;
            }

            // Act
            await _tenantService.DeleteTenantAsync(tenantBId);

            // Assert
            using (var context = new TenantContext(_dbContextOptions))
            {
                var tenantInDb = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantBId);
                tenantInDb.Should().BeNull();
            }
        }

        [Fact]
        public async Task CreateTenantAsync_ShouldThrowExceptionIfTenantNameAlreadyExists()
        {
            // Arrange
            var existingTenantName = "TenantA";
            var newTenant = new Tenant { Id = Guid.NewGuid(), Name = existingTenantName, ConnectionString = "DataSource=tenantDuplicate.db" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _tenantService.CreateTenantAsync(newTenant));
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
