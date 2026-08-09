using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Tenants;
using SqliteMultiTenant.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantRecoveryServiceExtensionsTests
    {
        private readonly ITenantRepository _mockRepository;
        private readonly ILogger<TenantRecoveryService> _mockLogger;
        private readonly TenantRecoveryService _service;

        public TenantRecoveryServiceExtensionsTests()
        {
            _mockRepository = Substitute.For<ITenantRepository>();
            _mockLogger = Substitute.For<ILogger<TenantRecoveryService>>();
            _service = new TenantRecoveryService(_mockRepository, _mockLogger);
        }

        [Fact]
        public async Task RepairDatabasesAsync_WithValidParameters_ReturnsSuccessCount()
        {
            // Arrange
            var tenantIds = new[] { "tenant1", "tenant2" };
            _mockRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<Tenant?>(new Tenant { TenantId = "tenant1", DatabasePath = ":memory:" }));
            _service.RepairDatabaseAsync(Arg.Any<string>()).Returns(true);

            // Act
            var result = await _service.RepairDatabasesAsync(tenantIds);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task RepairDatabasesAsync_WithNullService_ThrowsArgumentNullException()
        {
            // Arrange
            TenantRecoveryService service = null!;
            var tenantIds = new[] { "tenant1" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.RepairDatabasesAsync(tenantIds));
        }

        [Fact]
        public async Task RepairDatabasesAsync_WithNullTenantIds_ThrowsArgumentNullException()
        {
            // Arrange
            string[] tenantIds = null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RepairDatabasesAsync(tenantIds));
        }

        [Fact]
        public async Task RepairDatabasesAsync_WithEmptyTenantIds_ThrowsArgumentException()
        {
            // Arrange
            var tenantIds = Array.Empty<string>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.RepairDatabasesAsync(tenantIds));
        }

        [Fact]
        public async Task RestoreFromBackupsAsync_WithValidParameters_ReturnsSuccessCount()
        {
            // Arrange
            var restoreSpecs = new[] { ("tenant1", "/path/to/backup1"), ("tenant2", "/path/to/backup2") };
            _mockRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<Tenant?>(new Tenant { TenantId = "tenant1", DatabasePath = ":memory:" }));
            _service.RestoreFromBackupAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

            // Act
            var result = await _service.RestoreFromBackupsAsync(restoreSpecs);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task RestoreFromBackupsAsync_WithNullService_ThrowsArgumentNullException()
        {
            // Arrange
            TenantRecoveryService service = null!;
            var restoreSpecs = new[] { ("tenant1", "/path/to/backup") };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.RestoreFromBackupsAsync(restoreSpecs));
        }

        [Fact]
        public async Task RestoreFromBackupsAsync_WithNullRestoreSpecs_ThrowsArgumentNullException()
        {
            // Arrange
            (string TenantId, string BackupPath)[] restoreSpecs = null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RestoreFromBackupsAsync(restoreSpecs));
        }

        [Fact]
        public async Task RestoreFromBackupsAsync_WithEmptyRestoreSpecs_ThrowsArgumentException()
        {
            // Arrange
            var restoreSpecs = Array.Empty<(string TenantId, string BackupPath)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.RestoreFromBackupsAsync(restoreSpecs));
        }

        [Fact]
        public async Task PointInTimeRecoveryAsync_WithValidParameters_ReturnsSuccessCount()
        {
            // Arrange
            var recoveryRequests = new[] { ("tenant1", DateTime.UtcNow, "/backup/dir"), ("tenant2", DateTime.UtcNow, "/backup/dir") };
            _mockRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<Tenant?>(new Tenant { TenantId = "tenant1", DatabasePath = ":memory:" }));
            _service.PointInTimeRecoveryAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>()).Returns(true);

            // Act
            var result = await _service.PointInTimeRecoveryAsync(recoveryRequests);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task PointInTimeRecoveryAsync_WithNullService_ThrowsArgumentNullException()
        {
            // Arrange
            TenantRecoveryService service = null!;
            var recoveryRequests = new[] { ("tenant1", DateTime.UtcNow, "/backup/dir") };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.PointInTimeRecoveryAsync(recoveryRequests));
        }

        [Fact]
        public async Task PointInTimeRecoveryAsync_WithNullRecoveryRequests_ThrowsArgumentNullException()
        {
            // Arrange
            (string TenantId, DateTime TargetTime, string BackupDirectory)[] recoveryRequests = null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.PointInTimeRecoveryAsync(recoveryRequests));
        }

        [Fact]
        public async Task PointInTimeRecoveryAsync_WithEmptyRecoveryRequests_ThrowsArgumentException()
        {
            // Arrange
            var recoveryRequests = Array.Empty<(string TenantId, DateTime TargetTime, string BackupDirectory)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.PointInTimeRecoveryAsync(recoveryRequests));
        }

        [Fact]
        public async Task CleanupStaleBackupsAsync_WithValidParameters_ReturnsTotalDeleted()
        {
            // Arrange
            var tenantIds = new[] { "tenant1", "tenant2" };
            _mockRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<Tenant?>(new Tenant { TenantId = "tenant1", DatabasePath = ":memory:" }));
            _service.CleanupStaleBackupsAsync(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(1);

            // Act
            var result = await _service.CleanupStaleBackupsAsync(tenantIds, TimeSpan.FromDays(30));

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task CleanupStaleBackupsAsync_WithNullService_ThrowsArgumentNullException()
        {
            // Arrange
            TenantRecoveryService service = null!;
            var tenantIds = new[] { "tenant1" };
            var retentionPeriod = TimeSpan.FromDays(30);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CleanupStaleBackupsAsync(tenantIds, retentionPeriod));
        }

        [Fact]
        public async Task CleanupStaleBackupsAsync_WithNullTenantIds_ThrowsArgumentNullException()
        {
            // Arrange
            string[] tenantIds = null!;
            var retentionPeriod = TimeSpan.FromDays(30);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CleanupStaleBackupsAsync(tenantIds, retentionPeriod));
        }

        [Fact]
        public async Task CleanupStaleBackupsAsync_WithEmptyTenantIds_ThrowsArgumentException()
        {
            // Arrange
            var tenantIds = Array.Empty<string>();
            var retentionPeriod = TimeSpan.FromDays(30);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CleanupStaleBackupsAsync(tenantIds, retentionPeriod));
        }
    }
}