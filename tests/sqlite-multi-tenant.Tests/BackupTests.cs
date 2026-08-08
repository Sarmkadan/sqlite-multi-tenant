using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;
using FluentAssertions;

namespace SqliteMultiTenant.Tests
{
    public sealed class BackupTests
    {
        [Fact]
        public void MarkAsCompleted_WithOriginalSizeSet_ComputesCompressionRatio()
        {
            // Arrange: original 1000 bytes, compressed to 600 → 40% ratio
            var backup = new Backup { BackupId = "bkp-compress", DatabaseId = "db-001", BackupPath = "/data/bkp-compress.sqlite", OriginalSizeBytes = 1000 };

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
            var backup = new Backup { BackupId = "bkp-fail", DatabaseId = "db-001", BackupPath = "/data/bkp-fail.sqlite" };

            backup.MarkAsFailed("Disk quota exceeded");

            backup.Status.Should().Be(BackupStatus.Failed);
            backup.ErrorMessage.Should().Be("Disk quota exceeded");
        }

        [Fact]
        public void AddTag_WithMultipleTagCalls_BuildsCommaDelimitedStringAndParsesBack()
        {
            var backup = new Backup { BackupId = "bkp-tags", DatabaseId = "db-001", BackupPath = "/data/bkp-tags.sqlite" };

            backup.AddTag("daily");
            backup.AddTag("critical");
            backup.AddTag("2024-q1");

            backup.Tags.Should().Be("daily,critical,2024-q1");
            backup.GetTags().Should().HaveCount(3).And.ContainInOrder("daily", "critical", "2024-q1");
        }

        [Fact]
        public void IsExpired_WhenExpirationDateIsInFuture_ReturnsFalse()
        {
            var backup = new Backup { BackupId = "bkp-expiry", DatabaseId = "db-001", BackupPath = "/data/bkp-expiry.sqlite" };
            backup.SetExpiration(DateTime.UtcNow.AddDays(30));

            backup.IsExpired.Should().BeFalse();
        }
    }
}
