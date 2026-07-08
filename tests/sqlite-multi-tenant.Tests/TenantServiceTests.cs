#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class TenantServiceTests {
    private readonly ITenantRepository _mockRepository;
    private readonly ILogger<TenantService> _mockLogger;
    private readonly TenantService _service;

    public TenantServiceTests()
    {
        _mockRepository = Substitute.For<ITenantRepository>();
        _mockLogger = Substitute.For<ILogger<TenantService>>();
        _service = new TenantService(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task GetTenantAsync_WithBlankId_ThrowsArgumentException()
    {
        Func<Task> act = async () => await _service.GetTenantAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetTenantAsync_WhenTenantFound_InvokesRepositoryUpdateAndReturnsResult()
    {
        // Arrange
        var tenant = new Tenant { TenantId = "tenant-1", Name = "Test Corp", MaxConnections = 10 };
        _mockRepository
            .GetByIdAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(tenant));
        _mockRepository
            .UpdateAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetTenantAsync("tenant-1");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be("tenant-1");
        // Retrieving a tenant must persist the access timestamp
        await _mockRepository.Received(1).UpdateAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantAsync_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = new Tenant { TenantId = "existing-1", Name = "Acme Corp", MaxConnections = 10 };
        _mockRepository
            .GetByNameAsync("Acme Corp", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(existing));

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
            .GetByIdAsync("ghost-tenant", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(null));

        // Act
        Func<Task> act = async () => await _service.DeleteTenantAsync("ghost-tenant");

        // Assert
        await act.Should().ThrowAsync<TenantNotFoundException>();
    }
}
