#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;
using System;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class CacheInvalidationServiceTests {
        private readonly ICacheService _mockCacheService;
        private readonly ILogger<CacheInvalidationService> _mockLogger;
        private readonly CacheInvalidationService _sut;

        public CacheInvalidationServiceTests()
        {
            _mockCacheService = Substitute.For<ICacheService>();
            _mockLogger = Substitute.For<ILogger<CacheInvalidationService>>();
            _sut = new CacheInvalidationService(_mockCacheService, _mockLogger);
        }

        [Fact]
        public void CacheInvalidationService_Constructor_ThrowsArgumentNullException_WhenCacheServiceIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new CacheInvalidationService(null, _mockLogger))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("cache");
        }

        [Fact]
        public void CacheInvalidationService_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new CacheInvalidationService(_mockCacheService, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void InvalidateTenant_ShouldCallRemoveForTenantAndAllTenantsKeys()
        {
            // Arrange
            var tenantId = "tenant123";
            var tenantKey = CacheKeys.TenantKey(tenantId);
            var allTenantsKey = CacheKeys.AllTenantsKey();

            // Act
            _sut.InvalidateTenant(tenantId);

            // Assert
            _mockCacheService.Received(1).Remove(tenantKey);
            _mockCacheService.Received(1).Remove(allTenantsKey);
            _mockLogger.Received(1).LogInformation("Cache invalidated for tenant: {TenantId}", tenantId);
        }

        [Fact]
        public void InvalidateBackups_ShouldCallRemoveForBackupsForDatabaseKey()
        {
            // Arrange
            var databaseId = "db456";
            var backupsKey = CacheKeys.BackupsForDatabase(databaseId);

            // Act
            _sut.InvalidateBackups(databaseId);

            // Assert
            _mockCacheService.Received(1).Remove(backupsKey);
            _mockLogger.Received(1).LogInformation("Cache invalidated for backups in database: {DatabaseId}", databaseId);
        }

        [Fact]
        public void InvalidateMigrations_ShouldCallRemoveForPendingAndAppliedMigrationsKeys()
        {
            // Arrange
            var databaseId = "db789";
            var pendingMigrationsKey = CacheKeys.PendingMigrationsKey(databaseId);
            var appliedMigrationsKey = CacheKeys.AppliedMigrationsKey(databaseId);

            // Act
            _sut.InvalidateMigrations(databaseId);

            // Assert
            _mockCacheService.Received(1).Remove(pendingMigrationsKey);
            _mockCacheService.Received(1).Remove(appliedMigrationsKey);
            _mockLogger.Received(1).LogInformation("Cache invalidated for migrations in database: {DatabaseId}", databaseId);
        }

        [Fact]
        public void InvalidateHealthCheck_ShouldCallRemoveForHealthCheckKey()
        {
            // Arrange
            var healthCheckKey = CacheKeys.HealthCheckKey();

            // Act
            _sut.InvalidateHealthCheck();

            // Assert
            _mockCacheService.Received(1).Remove(healthCheckKey);
            _mockLogger.Received(1).LogInformation("Health check cache invalidated");
        }
    }
}
