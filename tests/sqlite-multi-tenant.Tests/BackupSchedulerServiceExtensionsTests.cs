#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests.BackgroundWorkers;

/// <summary>
/// Contains unit tests for the <see cref="BackupSchedulerServiceExtensions"/> class.
/// The extension methods keep schedule state in a static dictionary keyed by database path,
/// so every test uses its own unique path to stay isolated from the others.
/// </summary>
public sealed class BackupSchedulerServiceExtensionsTests
{
    /// <summary>
    /// Creates a scheduler with mocked dependencies. The extension methods only use the instance
    /// as a receiver, so the scheduler is never started.
    /// </summary>
    private static BackupSchedulerService CreateService() =>
        new(
            Substitute.For<IBackupService>(),
            Substitute.For<ITenantService>(),
            Substitute.For<ILogger<BackupSchedulerService>>());

    /// <summary>
    /// Returns a database path that no other test uses.
    /// </summary>
    private static string UniquePath() => $"/data/tenants/{Guid.NewGuid():N}.db";

    [Fact]
    public void ScheduleBackup_ThenGetNextScheduledBackupTime_ReturnsCurrentUtcTime()
    {
        // Arrange
        var service = CreateService();
        var path = UniquePath();

        // Act
        var before = DateTime.UtcNow;
        service.ScheduleBackup(path);
        var after = DateTime.UtcNow;
        var scheduled = service.GetNextScheduledBackupTime(path);

        // Assert
        scheduled.Should().NotBeNull();
        scheduled!.Value.Kind.Should().Be(DateTimeKind.Utc);
        scheduled.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ScheduleBackup_CalledTwice_KeepsOnlyLatestScheduledTime()
    {
        // Arrange
        var service = CreateService();
        var path = UniquePath();
        service.ScheduleBackup(path);
        var first = service.GetNextScheduledBackupTime(path);

        // Act
        service.ScheduleBackup(path);
        var second = service.GetNextScheduledBackupTime(path);

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.Value.Should().BeOnOrAfter(first!.Value);
    }

    [Fact]
    public void CancelScheduledBackup_RemovesOnlyTheCancelledPath()
    {
        // Arrange
        var service = CreateService();
        var cancelledPath = UniquePath();
        var otherPath = UniquePath();
        service.ScheduleBackup(cancelledPath);
        service.ScheduleBackup(otherPath);

        // Act
        service.CancelScheduledBackup(cancelledPath);

        // Assert
        service.GetNextScheduledBackupTime(cancelledPath).Should().BeNull();
        service.GetNextScheduledBackupTime(otherPath).Should().NotBeNull();
    }

    [Fact]
    public void CancelScheduledBackup_NotScheduledPath_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        var path = UniquePath();

        // Act
        var act = () => service.CancelScheduledBackup(path);

        // Assert
        act.Should().NotThrow();
        service.GetNextScheduledBackupTime(path).Should().BeNull();
    }

    [Fact]
    public void GetNextScheduledBackupTime_NeverScheduledPath_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetNextScheduledBackupTime(UniquePath());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Methods_NullService_ThrowArgumentNullExceptionForParameterService()
    {
        // Arrange
        BackupSchedulerService? service = null;
        var path = UniquePath();

        // Act
        var schedule = () => service!.ScheduleBackup(path);
        var get = () => service!.GetNextScheduledBackupTime(path);
        var cancel = () => service!.CancelScheduledBackup(path);

        // Assert
        schedule.Should().Throw<ArgumentNullException>().WithParameterName("service");
        get.Should().Throw<ArgumentNullException>().WithParameterName("service");
        cancel.Should().Throw<ArgumentNullException>().WithParameterName("service");
    }

    [Fact]
    public void Methods_NullOrEmptyPath_ThrowForParameterDatabasePath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var nullPath = () => service.ScheduleBackup(null!);
        var emptyPath = () => service.GetNextScheduledBackupTime(string.Empty);
        var emptyCancel = () => service.CancelScheduledBackup(string.Empty);

        // Assert
        nullPath.Should().Throw<ArgumentNullException>().WithParameterName("databasePath");
        emptyPath.Should().Throw<ArgumentException>().WithParameterName("databasePath");
        emptyCancel.Should().Throw<ArgumentException>().WithParameterName("databasePath");
    }
}
