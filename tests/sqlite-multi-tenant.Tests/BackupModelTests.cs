// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class BackupModelTests
{
    private static Backup CreateBackup(string id = "bkp-001") =>
        new() { BackupId = id, DatabaseId = "db-001", BackupPath = $"/data/{id}.sqlite" };

    [Fact]
    public void MarkAsCompleted_WithOriginalSizeSet_ComputesCompressionRatio()
    {
        // Arrange: original 1000 bytes, compressed to 600 → 40% ratio
        var backup = CreateBackup("bkp-compress");
        backup.OriginalSizeBytes = 1000;

        // Act
        backup.MarkAsCompleted(sizeBytes: 600, durationMs: 4500);

        // Assert
        backup.Status.Should().Be(BackupStatus.Completed);
        backup.SizeBytes.Should().Be(600);
        backup.DurationMs.Should().Be(4500);
        backup.CompressionRatio.Should().Be(40);
        backup.CompletedAt.Should().NotBeNull();
        backup.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_WithErrorMessage_SetsFailedStatusAndPreservesMessage()
    {
        var backup = CreateBackup("bkp-fail");

        backup.MarkAsFailed("Disk quota exceeded");

        backup.Status.Should().Be(BackupStatus.Failed);
        backup.ErrorMessage.Should().Be("Disk quota exceeded");
    }

    [Fact]
    public void AddTag_WithMultipleTagCalls_BuildsCommaDelimitedStringAndParsesBack()
    {
        var backup = CreateBackup("bkp-tags");

        backup.AddTag("daily");
        backup.AddTag("critical");
        backup.AddTag("2024-q1");

        backup.Tags.Should().Be("daily,critical,2024-q1");
        backup.GetTags().Should().HaveCount(3).And.ContainInOrder("daily", "critical", "2024-q1");
    }

    [Fact]
    public void IsExpired_WhenExpirationDateIsInFuture_ReturnsFalse()
    {
        var backup = CreateBackup("bkp-expiry");
        backup.SetExpiration(DateTime.UtcNow.AddDays(30));

        backup.IsExpired.Should().BeFalse();
    }
}
