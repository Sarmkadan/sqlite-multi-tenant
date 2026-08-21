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
    /// <summary>
    /// Integration tests for <see cref="TenantService"/> that verify tenant management operations
    /// against a real SQLite database. Tests cover CRUD operations and ensure proper integration
    /// between the service layer and repository layer.
    /// </summary>
    public sealed class TenantServiceIntegrationTests : IDisposable {
            /// <summary>
    /// Gets the path to the temporary SQLite database file used for testing.
    /// </summary>
    private readonly string _dbPath;
            /// <summary>
    /// Gets the connection string for the test SQLite database.
    /// </summary>
    private readonly string _connectionString;
            /// <summary>
    /// Gets the logger instance used for testing <see cref="TenantService"/> operations.
    /// </summary>
    private readonly ILogger<TenantService> _logger;
            /// <summary>
    /// Gets the tenant repository instance used to interact with the database.
    /// </summary>
    private readonly ITenantRepository _tenantRepository;
            /// <summary>
    /// Gets the tenant service instance being tested.
    /// </summary>
    private readonly TenantService _tenantService;

            /// <summary>
    /// Initializes a new instance of the <see cref="TenantServiceIntegrationTests"/> class.
    /// Sets up a temporary SQLite database with seed data for integration testing.
    /// </summary>
    public TenantServiceIntegrationTests()
            {
                _logger.LogInformation("Starting initialization of TenantServiceIntegrationTests");
                _dbPath = Path.Combine(Path.GetTempPath(), $"tenant_service_tests_{Guid.NewGuid():N}.db");
                _connectionString = $"Data Source={_dbPath};Version=3;";

                _logger = NullLogger<TenantService>.Instance;
                _tenantRepository = new TenantRepository(_connectionString, NullLogger<TenantRepository>.Instance); // Use concrete repository for integration

                SeedData();

                _tenantService = new TenantService(_tenantRepository, _logger);
                _logger.LogInformation("Finished initialization of TenantServiceIntegrationTests for {DbPath}", _dbPath);
            }

            /// <summary>
    /// Seeds the test database with initial tenant data for integration tests.
    /// Creates two test tenants: "tenant-a" and "tenant-b" with sample data.
    /// </summary>
    private void SeedData()
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                InsertTenant(connection, "tenant-a", "TenantA", "tenantA.db");
                InsertTenant(connection, "tenant-b", "TenantB", "tenantB.db");
            }

            /// <summary>
    /// Inserts a tenant record into the database for testing purposes.
    /// </summary>
    /// <param name="connection">The SQLite database connection to use for insertion.</param>
    /// <param name="tenantId">The unique identifier for the tenant.</param>
    /// <param name="name">The display name of the tenant.</param>
    /// <param name="databasePath">The path to the tenant's database file.</param>
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

            /// <summary>
    /// Verifies that <see cref="TenantService.GetAllTenantsAsync"/> returns all seeded tenants from the database.
    /// Ensures the service correctly retrieves all tenant records.
    /// </summary>
    [Fact]
    public async Task GetAllTenantsAsync_ShouldReturnAllSeededTenants()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(GetAllTenantsAsync_ShouldReturnAllSeededTenants));
                // Act
                var tenants = await _tenantService.GetAllTenantsAsync();

                // Assert
                tenants.Should().NotBeNull();
                tenants.Should().HaveCount(2);
                tenants.Should().Contain(t => t.Name == "TenantA");
                tenants.Should().Contain(t => t.Name == "TenantB");
                _logger.LogInformation("Finished test {TestName}", nameof(GetAllTenantsAsync_ShouldReturnAllSeededTenants));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.GetTenantAsync(string)"/> returns the correct tenant for a valid tenant ID.
    /// Verifies the service can retrieve a specific tenant by its identifier.
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_ShouldReturnCorrectTenant()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(GetTenantAsync_ShouldReturnCorrectTenant));
                // Act
                var tenant = await _tenantService.GetTenantAsync("tenant-a");

                // Assert
                tenant.Should().NotBeNull();
                tenant!.Name.Should().Be("TenantA");
                _logger.LogInformation("Finished test {TestName}", nameof(GetTenantAsync_ShouldReturnCorrectTenant));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.GetTenantAsync(string)"/> returns null when querying for a non-existent tenant ID.
    /// Ensures the service handles missing tenant records gracefully.
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_ShouldReturnNullForNonExistingTenant()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(GetTenantAsync_ShouldReturnNullForNonExistingTenant));
                // Arrange
                var nonExistingId = Guid.NewGuid().ToString();

                // Act
                var tenant = await _tenantService.GetTenantAsync(nonExistingId);

                // Assert
                tenant.Should().BeNull();
                _logger.LogInformation("Finished test {TestName}", nameof(GetTenantAsync_ShouldReturnNullForNonExistingTenant));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.CreateTenantAsync(string)"/> successfully adds a new tenant to the database.
    /// Verifies the service creates a new tenant record and returns the created tenant.
    /// </summary>
    [Fact]
    public async Task CreateTenantAsync_ShouldAddTenantToDatabase()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(CreateTenantAsync_ShouldAddTenantToDatabase));
                // Act
                var createdTenant = await _tenantService.CreateTenantAsync("TenantC");

                // Assert
                createdTenant.Should().NotBeNull();
                createdTenant.Name.Should().Be("TenantC");

                var tenantInDb = await _tenantRepository.GetByIdAsync(createdTenant.TenantId);
                tenantInDb.Should().NotBeNull();
                tenantInDb!.Name.Should().Be("TenantC");
                _logger.LogInformation("Finished test {TestName}", nameof(CreateTenantAsync_ShouldAddTenantToDatabase));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.UpdateTenantAsync(Tenant)"/> successfully updates an existing tenant in the database.
    /// Verifies the service persists changes to tenant records correctly.
    /// </summary>
    [Fact]
    public async Task UpdateTenantAsync_ShouldUpdateTenantInDatabase()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(UpdateTenantAsync_ShouldUpdateTenantInDatabase));
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
                _logger.LogInformation("Finished test {TestName}", nameof(UpdateTenantAsync_ShouldUpdateTenantInDatabase));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.DeleteTenantAsync(string)"/> successfully removes a tenant from the database.
    /// Verifies the service can delete tenant records and they are no longer retrievable.
    /// </summary>
    [Fact]
    public async Task DeleteTenantAsync_ShouldRemoveTenantFromDatabase()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(DeleteTenantAsync_ShouldRemoveTenantFromDatabase));
                // Act
                await _tenantService.DeleteTenantAsync("tenant-b");

                // Assert
                var tenantInDb = await _tenantRepository.GetByIdAsync("tenant-b");
                tenantInDb.Should().BeNull();
                _logger.LogInformation("Finished test {TestName}", nameof(DeleteTenantAsync_ShouldRemoveTenantFromDatabase));
            }

            /// <summary>
    /// Tests that <see cref="TenantService.CreateTenantAsync(string)"/> throws an exception when attempting to create a tenant with a name that already exists.
    /// Verifies the service enforces unique tenant name constraints.
    /// </summary>
    [Fact]
    public async Task CreateTenantAsync_ShouldThrowExceptionIfTenantNameAlreadyExists()
            {
                _logger.LogInformation("Starting test {TestName}", nameof(CreateTenantAsync_ShouldThrowExceptionIfTenantNameAlreadyExists));
                // Arrange
                var existingTenantName = "TenantA";

                // Act & Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() => _tenantService.CreateTenantAsync(existingTenantName));
                _logger.LogInformation("Finished test {TestName}", nameof(CreateTenantAsync_ShouldThrowExceptionIfTenantNameAlreadyExists));
            }

            /// <summary>
    /// Cleans up the test environment by deleting the temporary database file.
    /// Implements <see cref="IDisposable.Dispose"/> to ensure proper resource cleanup.
    /// </summary>
    public void Dispose()
            {
                _logger.LogInformation("Starting disposal of TenantServiceIntegrationTests for {DbPath}", _dbPath);
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
                _logger.LogInformation("Finished disposal of TenantServiceIntegrationTests");
            }
    }
}