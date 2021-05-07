#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class TenantServiceTests {
    private readonly Mock<ITenantRepository> _mockRepository;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _service;

    public TenantServiceTests()
    {
        _mockRepository = new Mock<ITenantRepository>();
        _mockLogger = new Mock<ILogger<TenantService>>();
        _service = new TenantService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTenantAsync_WithBlankId_ThrowsArgumentException()
    {
        Func<Task> act = async () => await _service.GetTenantAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Tenant ID cannot be empty*");
    }

    [Fact]
    public async Task GetTenantAsync_WhenTenantFound_InvokesRepositoryUpdateAndReturnsResult()
    {
        // Arrange
        var tenant = new Tenant { TenantId = "tenant-1", Name = "Test Corp", MaxConnections = 10 };
        _mockRepository
            .Setup(r => r.GetByIdAsync("tenant-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetTenantAsync("tenant-1");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be("tenant-1");
        // Retrieving a tenant must persist the access timestamp
        _mockRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTenantAsync_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = new Tenant { TenantId = "existing-1", Name = "Acme Corp", MaxConnections = 10 };
        _mockRepository
            .Setup(r => r.GetByNameAsync("Acme Corp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        Func<Task> act = async () => await _service.CreateTenantAsync("Acme Corp");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteTenantAsync_WhenTenantNotFound_ThrowsTenantNotFoundException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync("ghost-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        Func<Task> act = async () => await _service.DeleteTenantAsync("ghost-tenant");

        // Assert
        await act.Should().ThrowAsync<TenantNotFoundException>();
    }
}
