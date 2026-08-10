using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests
{
    public class ValidationExtensionsTests
    {
        [Fact]
        public void IsValidEmail_ValidEmail_ReturnsTrue()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = email.IsValidEmail();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidEmail_InvalidEmail_ReturnsFalse()
        {
            // Arrange
            var email = "invalid-email";

            // Act
            var result = email.IsValidEmail();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidEmail_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string email = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => email.IsValidEmail());
        }

        [Fact]
        public void IsValidEmail_EmptyOrWhitespace_ReturnsFalse()
        {
            // Arrange
            var email = "   ";

            // Act
            var result = email.IsValidEmail();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidUuid_ValidUuidWithHyphens_ReturnsTrue()
        {
            // Arrange
            var uuid = "123e4567-e89b-12d3-a456-426614174000";

            // Act
            var result = uuid.IsValidUuid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidUuid_ValidUuidWithoutHyphens_ReturnsTrue()
        {
            // Arrange
            var uuid = "123e4567e89b12d3a456426614174000";

            // Act
            var result = uuid.IsValidUuid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidUuid_InvalidUuid_ReturnsFalse()
        {
            // Arrange
            var uuid = "not-a-uuid";

            // Act
            var result = uuid.IsValidUuid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidUuid_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string uuid = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => uuid.IsValidUuid());
        }

        [Fact]
        public void IsValidSemanticVersion_ValidVersion_ReturnsTrue()
        {
            // Arrange
            var version = "1.0.0";

            // Act
            var result = version.IsValidSemanticVersion();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSemanticVersion_ValidVersionWithPrerelease_ReturnsTrue()
        {
            // Arrange
            var version = "1.0.0-beta";

            // Act
            var result = version.IsValidSemanticVersion();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSemanticVersion_ValidVersionWithBuildMetadata_ReturnsTrue()
        {
            // Arrange
            var version = "1.0.0+build.1";

            // Act
            var result = version.IsValidSemanticVersion();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSemanticVersion_InvalidVersion_ReturnsFalse()
        {
            // Arrange
            var version = "1.0";

            // Act
            var result = version.IsValidSemanticVersion();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidSemanticVersion_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string version = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => version.IsValidSemanticVersion());
        }

        [Fact]
        public void IsValidDatabaseName_ValidName_ReturnsTrue()
        {
            // Arrange
            var name = "my_database";

            // Act
            var result = name.IsValidDatabaseName();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidDatabaseName_InvalidNameStartingWithNumber_ReturnsFalse()
        {
            // Arrange
            var name = "123invalid";

            // Act
            var result = name.IsValidDatabaseName();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidDatabaseName_InvalidNameWithSpecialChars_ReturnsFalse()
        {
            // Arrange
            var name = "my-db";

            // Act
            var result = name.IsValidDatabaseName();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidDatabaseName_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string name = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => name.IsValidDatabaseName());
        }

        [Fact]
        public void IsValidTenantName_ValidName_ReturnsTrue()
        {
            // Arrange
            var name = "mytenant";

            // Act
            var result = name.IsValidTenantName();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidTenantName_TooShort_ReturnsFalse()
        {
            // Arrange
            var name = "ab";

            // Act
            var result = name.IsValidTenantName();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidTenantName_TooLong_ReturnsFalse()
        {
            // Arrange
            var name = new string('a', 256);

            // Act
            var result = name.IsValidTenantName();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidTenantName_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string name = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => name.IsValidTenantName());
        }

        [Fact]
        public void IsValidRelativePath_ValidPath_ReturnsTrue()
        {
            // Arrange
            var path = "folder/subfolder/file.txt";

            // Act
            var result = path.IsValidRelativePath();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidRelativePath_PathWithDoubleDots_ReturnsFalse()
        {
            // Arrange
            var path = "folder/../file.txt";

            // Act
            var result = path.IsValidRelativePath();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidRelativePath_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string path = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => path.IsValidRelativePath());
        }

        [Fact]
        public void IsValidSqlScript_ValidScript_ReturnsTrue()
        {
            // Arrange
            var script = "SELECT * FROM users WHERE id = 1";

            // Act
            var result = script.IsValidSqlScript();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSqlScript_ScriptWithDropDatabase_ReturnsFalse()
        {
            // Arrange
            var script = "DROP DATABASE users";

            // Act
            var result = script.IsValidSqlScript();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidSqlScript_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string script = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => script.IsValidSqlScript());
        }

        [Fact]
        public void IsValidPort_ValidPort_ReturnsTrue()
        {
            // Arrange
            var port = 8080;

            // Act
            var result = port.IsValidPort();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidPort_PortTooLow_ReturnsFalse()
        {
            // Arrange
            var port = 0;

            // Act
            var result = port.IsValidPort();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPort_PortTooHigh_ReturnsFalse()
        {
            // Arrange
            var port = 65536;

            // Act
            var result = port.IsValidPort();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidConnectionString_ValidConnectionString_ReturnsTrue()
        {
            // Arrange
            var connectionString = "Data Source=mydb.sqlite";

            // Act
            var result = connectionString.IsValidConnectionString();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidConnectionString_ValidConnectionStringWithFilename_ReturnsTrue()
        {
            // Arrange
            var connectionString = "Filename=mydb.sqlite";

            // Act
            var result = connectionString.IsValidConnectionString();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidConnectionString_InvalidConnectionString_ReturnsFalse()
        {
            // Arrange
            var connectionString = "Server=localhost;Port=5432";

            // Act
            var result = connectionString.IsValidConnectionString();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidConnectionString_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string connectionString = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => connectionString.IsValidConnectionString());
        }

        [Fact]
        public void IsValidBackupTag_ValidTag_ReturnsTrue()
        {
            // Arrange
            var tag = "daily-backup";

            // Act
            var result = tag.IsValidBackupTag();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidBackupTag_InvalidTagWithSpecialChars_ReturnsFalse()
        {
            // Arrange
            var tag = "backup@tag";

            // Act
            var result = tag.IsValidBackupTag();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidBackupTag_TooLong_ReturnsFalse()
        {
            // Arrange
            var tag = new string('a', 101);

            // Act
            var result = tag.IsValidBackupTag();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidBackupTag_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string tag = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tag.IsValidBackupTag());
        }
    }
}