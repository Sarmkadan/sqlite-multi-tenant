#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Tenants;
using Xunit;

/// <summary>
/// Tests for <see cref="TenantProvisioner.RenameTenantAsync"/> using real SQLite files and a mocked registry.
/// </summary>
public sealed class TenantProvisionerRenameTests : IDisposable
{
    private const string OldId = "acme-corp";
    private const string NewId = "acme-holdings";

    private readonly ITenantRepository _tenantRepository;
    private readonly IEventBus _eventBus;
    private readonly string _basePath;
    private readonly TenantProvisioner _provisioner;

    public TenantProvisionerRenameTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"tenant_rename_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_basePath);

        _tenantRepository = Substitute.For<ITenantRepository>();
        _eventBus = Substitute.For<IEventBus>();
        var schemaManager = new SchemaManager(Substitute.For<ILogger<SchemaManager>>(), "Data Source=:memory:;Version=3;");

        _provisioner = new TenantProvisioner(
            _tenantRepository,
            schemaManager,
            Substitute.For<ILogger<TenantProvisioner>>(),
            _basePath,
            eventBus: _eventBus);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    /// <summary>
    /// Tests that a rename moves the database, registers the tenant under the new id, removes the original, and publishes an event.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WithValidId_MovesDatabaseUpdatesRegistryAndPublishesEvent()
    {
        // Arrange
        var sourcePath = CreateTenantDatabase(OldId, rows: 3);
        var source = CreateTenant(OldId, sourcePath);
        _tenantRepository.GetByIdAsync(OldId, Arg.Any<CancellationToken>()).Returns(source);
        _tenantRepository.ExistsAsync(NewId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var renamed = await _provisioner.RenameTenantAsync(OldId, NewId);

        // Assert
        var expectedPath = Path.Combine(_basePath, NewId, $"{NewId}.db");
        renamed.TenantId.Should().Be(NewId);
        renamed.DatabasePath.Should().Be(expectedPath);
        renamed.Name.Should().Be(source.Name);

        File.Exists(expectedPath).Should().BeTrue();
        File.Exists(sourcePath).Should().BeFalse();
        CountRows(expectedPath).Should().Be(3);

        await _tenantRepository.Received(1).AddAsync(
            Arg.Is<Tenant>(t => t.TenantId == NewId && t.DatabasePath == expectedPath),
            Arg.Any<CancellationToken>());
        await _tenantRepository.Received(1).DeleteAsync(OldId, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TenantRenamedEvent>(e => e.OldTenantId == OldId && e.NewTenantId == NewId && e.DatabasePath == expectedPath),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that a reserved identifier is rejected by the shared tenant id validation before any file is touched.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WithReservedId_ThrowsArgumentException()
    {
        // Arrange
        var sourcePath = CreateTenantDatabase(OldId, rows: 1);

        // Act
        var act = async () => await _provisioner.RenameTenantAsync(OldId, "admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        File.Exists(sourcePath).Should().BeTrue();
        await _tenantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    /// <summary>
    /// Tests that renaming to an identifier with different letter case is rejected.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WithCaseOnlyChange_ThrowsArgumentException()
    {
        // Arrange
        CreateTenantDatabase(OldId, rows: 1);

        // Act
        var act = async () => await _provisioner.RenameTenantAsync(OldId, "ACME-CORP");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that an unknown source tenant raises <see cref="TenantNotFoundException"/>.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WithUnknownTenant_ThrowsTenantNotFoundException()
    {
        // Arrange
        _tenantRepository.GetByIdAsync(OldId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        // Act
        var act = async () => await _provisioner.RenameTenantAsync(OldId, NewId);

        // Assert
        await act.Should().ThrowAsync<TenantNotFoundException>();
    }

    /// <summary>
    /// Tests that an existing target registration blocks the rename and leaves the source database in place.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WhenNewIdAlreadyExists_ThrowsAndKeepsSource()
    {
        // Arrange
        var sourcePath = CreateTenantDatabase(OldId, rows: 2);
        _tenantRepository.GetByIdAsync(OldId, Arg.Any<CancellationToken>()).Returns(CreateTenant(OldId, sourcePath));
        _tenantRepository.ExistsAsync(NewId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = async () => await _provisioner.RenameTenantAsync(OldId, NewId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(sourcePath).Should().BeTrue();
        File.Exists(Path.Combine(_basePath, NewId, $"{NewId}.db")).Should().BeFalse();
        await _eventBus.DidNotReceiveWithAnyArgs().PublishAsync(default(TenantRenamedEvent)!);
    }

    /// <summary>
    /// Tests that a failure while removing the old registry record rolls back the new record and the moved file.
    /// </summary>
    [Fact]
    public async Task RenameTenantAsync_WhenRegistryDeleteFails_RollsBackNewRecordAndFile()
    {
        // Arrange
        var sourcePath = CreateTenantDatabase(OldId, rows: 2);
        var targetPath = Path.Combine(_basePath, NewId, $"{NewId}.db");
        _tenantRepository.GetByIdAsync(OldId, Arg.Any<CancellationToken>()).Returns(CreateTenant(OldId, sourcePath));
        _tenantRepository.ExistsAsync(NewId, Arg.Any<CancellationToken>()).Returns(false);
        _tenantRepository.DeleteAsync(OldId, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("registry unavailable"));

        // Act
        var act = async () => await _provisioner.RenameTenantAsync(OldId, NewId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _tenantRepository.Received(1).DeleteAsync(NewId, Arg.Any<CancellationToken>());
        File.Exists(targetPath).Should().BeFalse();
        File.Exists(sourcePath).Should().BeTrue();
        CountRows(sourcePath).Should().Be(2);
        await _eventBus.DidNotReceiveWithAnyArgs().PublishAsync(default(TenantRenamedEvent)!);
    }

    private string CreateTenantDatabase(string tenantId, int rows)
    {
        var dir = Path.Combine(_basePath, tenantId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{tenantId}.db");

        using var connection = new SQLiteConnection($"Data Source={path};");
        connection.Open();
        using (var create = new SQLiteCommand("CREATE TABLE Items (Id INTEGER PRIMARY KEY, Value TEXT);", connection))
        {
            create.ExecuteNonQuery();
        }

        for (var i = 0; i < rows; i++)
        {
            using var insert = new SQLiteCommand($"INSERT INTO Items (Value) VALUES ('row-{i}');", connection);
            insert.ExecuteNonQuery();
        }

        return path;
    }

    private static int CountRows(string path)
    {
        using var connection = new SQLiteConnection($"Data Source={path};");
        connection.Open();
        using var command = new SQLiteCommand("SELECT COUNT(*) FROM Items;", connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static Tenant CreateTenant(string tenantId, string databasePath) => new()
    {
        TenantId = tenantId,
        Name = "Acme Corporation",
        Status = TenantStatus.Active,
        DatabasePath = databasePath
    };
}
