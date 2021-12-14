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

/// <summary>
/// Tests for the TenantService class.
/// </summary>
public sealed class TenantServiceTests {
    private readonly ITenantRepository _mockRepository;
    private readonly ILogger<TenantService> _mockLogger;
    private readonly TenantService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantServiceTests"/> class.
    /// </summary>
    public TenantServiceTests() {
        _mockRepository = Substitute.For<ITenantRepository>();
        _mockLogger = Substitute.For<ILogger<TenantService>>();
        _service = new TenantService(_mockRepository, _mockLogger);
    }

    /// <summary>
    /// Verifies that an <see cref="ArgumentException"/> is thrown when an empty tenant ID is passed to <see cref="TenantService.GetTenantAsync(string)"/>.
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_WithBlankId_ThrowsArgumentException() {
        Func<Task> act = async () => await _service.GetTenantAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that <see cref="TenantService.GetTenantAsync(string)"/> invokes <see cref="ITenantRepository.UpdateAsync(Tenant, CancellationToken)"/> and returns the result when a tenant is found.
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_WhenTenantFound_InvokesRepositoryUpdateAndReturnsResult() {
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

    /// <summary>
    /// Verifies that an <see cref="InvalidOperationException"/> is thrown when attempting to create a tenant with a name that already exists.
    /// </summary>
    [Fact]
    public async Task CreateTenantAsync_WhenNameAlreadyExists_ThrowsInvalidOperationException() {
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

    /// <summary>
    /// Verifies that a <see cref="TenantNotFoundException"/> is thrown when attempting to delete a tenant that does not exist.
    /// </summary>
    [Fact]
    public async Task DeleteTenantAsync_WhenTenantNotFound_ThrowsTenantNotFoundException() {
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
