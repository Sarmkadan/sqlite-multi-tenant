using System;
using System.Collections.Generic;
using SqliteMultiTenant.Models;
using Xunit;

namespace sqlite_multi_tenant.Tests
{
    public class TenantDatabaseTests
    {
        [Fact]
        public void Validate_ReturnsTrue_WhenAllPropertiesAreValid()
        {
            // Arrange
            var db = new TenantDatabase
            {
                DatabaseId = "db1",
                TenantId   = "tenant1",
                Name       = "Main DB",
                FilePath   = "/var/data/db1.sqlite",
                SizeBytes  = 1024,
                SchemaVersion = 1,
                ActiveConnectionCount = 0
            };

            // Act
            var result = db.Validate(out List<string> errors);

            // Assert
            Assert.True(result);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ReturnsFalse_AndPopulatesErrors_OnInvalidProperties()
        {
            // Arrange: intentionally break several rules
            var db = new TenantDatabase
            {
                DatabaseId = "",               // required
                TenantId   = null!,            // required (null)
                Name       = "   ",            // whitespace only
                FilePath   = new string('a', 261), // exceeds 260 chars
                SizeBytes  = -10,              // negative
                SchemaVersion = 0,             // must be >0
                ActiveConnectionCount = -5    // negative
            };

            // Act
            var result = db.Validate(out List<string> errors);

            // Assert
            Assert.False(result);
            Assert.Contains("DatabaseId is required", errors);
            Assert.Contains("TenantId is required", errors);
            Assert.Contains("Name is required", errors);
            Assert.Contains("FilePath exceeds maximum path length", errors);
            Assert.Contains("SizeBytes cannot be negative", errors);
            Assert.Contains("SchemaVersion must be greater than zero", errors);
            Assert.Contains("ActiveConnectionCount cannot be negative", errors);
        }

        [Fact]
        public void UpdateLastBackupTime_Sets_LastBackupAt_And_Updates_UpdatedAt()
        {
            // Arrange
            var db = new TenantDatabase();
            var beforeBackup = db.LastBackupAt;
            var beforeUpdated = db.UpdatedAt;

            // Act
            db.UpdateLastBackupTime();

            // Assert
            Assert.NotNull(db.LastBackupAt);
            Assert.True(db.LastBackupAt > beforeUpdated);
            Assert.True(db.UpdatedAt > beforeUpdated);
        }

        [Fact]
        public void UpdateSize_UpdatesSizeAndUpdatedAt_WhenSizeIsNonNegative()
        {
            // Arrange
            var db = new TenantDatabase { SizeBytes = 500 };
            var beforeUpdated = db.UpdatedAt;

            // Act
            db.UpdateSize(1234);

            // Assert
            Assert.Equal(1234, db.SizeBytes);
            Assert.True(db.UpdatedAt > beforeUpdated);
        }

        [Fact]
        public void UpdateSize_DoesNotChangeAnything_WhenSizeIsNegative()
        {
            // Arrange
            var db = new TenantDatabase { SizeBytes = 500 };
            var beforeSize = db.SizeBytes;
            var beforeUpdated = db.UpdatedAt;

            // Act
            db.UpdateSize(-1);

            // Assert
            Assert.Equal(beforeSize, db.SizeBytes);
            Assert.Equal(beforeUpdated, db.UpdatedAt);
        }

        [Fact]
        public void IncrementConnectionCount_StopsAtMaximumOf100()
        {
            // Arrange
            var db = new TenantDatabase { ActiveConnectionCount = 99 };

            // Act
            db.IncrementConnectionCount(); // should become 100
            db.IncrementConnectionCount(); // should stay 100

            // Assert
            Assert.Equal(100, db.ActiveConnectionCount);
        }

        [Fact]
        public void DecrementConnectionCount_StopsAtZero()
        {
            // Arrange
            var db = new TenantDatabase { ActiveConnectionCount = 1 };

            // Act
            db.DecrementConnectionCount(); // becomes 0
            db.DecrementConnectionCount(); // stays 0

            // Assert
            Assert.Equal(0, db.ActiveConnectionCount);
        }

        [Fact]
        public void IsEncrypted_ReturnsTrue_WhenEncryptionKeyIsSet_AndFalseOtherwise()
        {
            // Encryption key set
            var dbWithKey = new TenantDatabase { EncryptionKey = "secret" };
            Assert.True(dbWithKey.IsEncrypted);

            // No encryption key
            var dbWithoutKey = new TenantDatabase { EncryptionKey = null };
            Assert.False(dbWithoutKey.IsEncrypted);
        }
    }
}
