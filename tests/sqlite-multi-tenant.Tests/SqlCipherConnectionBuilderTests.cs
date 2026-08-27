using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SqliteMultiTenant.Security;
using Xunit;

namespace SqliteMultiTenant.Tests.Security
{
    /// <summary>
    /// Contains unit tests for the SqlCipherConnectionBuilder class.
    /// Tests cover connection string building, encryption key application, and rekeying functionality.
    /// </summary>
    public class SqlCipherConnectionBuilderTests
    {
        #region BuildConnectionString

        /// <summary>
        /// Tests that BuildConnectionString returns a connection string containing the expected database path and password when given valid parameters.
        /// </summary>
        [Fact]
        public void BuildConnectionString_WithValidParameters_ReturnsExpectedString()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".db");
            var key = "my-secret-key";

            // Act
            var connectionString = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, key);

            // Assert
            connectionString.Should().Contain($"Data Source={dbPath}");
            connectionString.Should().Contain($"Password={key}");
        }

        /// <summary>
        /// Tests that BuildConnectionString throws an ArgumentException when the database path is null, empty, or whitespace.
        /// </summary>
        /// <param name="dbPath">The database path to test (null, empty, or whitespace).</param>
        /// <param name="key">The encryption key (a valid, non-empty string).</param>
        [Theory]
        [InlineData(null, "validKey")]
        [InlineData("", "validKey")]
        [InlineData("   ", "validKey")]
        public void BuildConnectionString_InvalidDatabasePath_ThrowsArgumentException(string dbPath, string key)
        {
            // Act
            Action act = () => SqlCipherConnectionBuilder.BuildConnectionString(dbPath!, key);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Database path cannot be empty*")
                .Where(e => e.ParamName == "databasePath");
        }

        /// <summary>
        /// Tests that BuildConnectionString throws an ArgumentException when the encryption key is null, empty, or whitespace.
        /// </summary>
        /// <param name="dbPath">The database path (a valid, non-empty string).</param>
        /// <param name="key">The encryption key to test (null, empty, or whitespace).</param>
        [Theory]
        [InlineData("validPath.db", null)]
        [InlineData("validPath.db", "")]
        [InlineData("validPath.db", "   ")]
        public void BuildConnectionString_InvalidEncryptionKey_ThrowsArgumentException(string dbPath, string key)
        {
            // Act
            Action act = () => SqlCipherConnectionBuilder.BuildConnectionString(dbPath, key!);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Encryption key cannot be empty*")
                .Where(e => e.ParamName == "encryptionKey");
        }

        #endregion

        #region ApplyEncryptionKeyAsync

        /// <summary>
        /// Tests that ApplyEncryptionKeyAsync executes without throwing when given a valid open connection and a non-empty key.
        /// </summary>
        [Fact]
        public async Task ApplyEncryptionKeyAsync_WithValidConnection_ExecutesWithoutException()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var connStr = new SQLiteConnectionStringBuilder
            {
                DataSource = tempFile,
                Version = 3
            }.ToString();

            await using var connection = new SQLiteConnection(connStr);
            await connection.OpenAsync();

            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(connection, "test-key");

            // Assert
            await act.Should().NotThrowAsync();

            await connection.CloseAsync();
            File.Delete(tempFile);
        }

        /// <summary>
        /// Tests that ApplyEncryptionKeyAsync throws an ArgumentNullException when the connection parameter is null.
        /// </summary>
        [Fact]
        public async Task ApplyEncryptionKeyAsync_NullConnection_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(null!, "key");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .Where(e => e.ParamName == "connection");
        }

        /// <summary>
        /// Tests that ApplyEncryptionKeyAsync throws an ArgumentException when the encryption key is null, empty, or whitespace.
        /// </summary>
        /// <param name="key">The encryption key to test (null, empty, or whitespace).</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ApplyEncryptionKeyAsync_InvalidKey_ThrowsArgumentException(string key)
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var connStr = new SQLiteConnectionStringBuilder
            {
                DataSource = tempFile,
                Version = 3
            }.ToString();

            await using var connection = new SQLiteConnection(connStr);
            await connection.OpenAsync();

            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(connection, key);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Encryption key cannot be empty*")
                .Where(e => e.ParamName == "encryptionKey");

            await connection.CloseAsync();
            File.Delete(tempFile);
        }

        #endregion

        #region RekeyAsync

        /// <summary>
        /// Tests that RekeyAsync executes without throwing when given a valid open connection and a non-empty new key.
        /// </summary>
        [Fact]
        public async Task RekeyAsync_WithValidConnection_ExecutesWithoutException()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var connStr = new SQLiteConnectionStringBuilder
            {
                DataSource = tempFile,
                Version = 3
            }.ToString();

            await using var connection = new SQLiteConnection(connStr);
            await connection.OpenAsync();

            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.RekeyAsync(connection, "new-key");

            // Assert
            await act.Should().NotThrowAsync();

            await connection.CloseAsync();
            File.Delete(tempFile);
        }

        /// <summary>
        /// Tests that RekeyAsync throws an ArgumentNullException when the connection parameter is null.
        /// </summary>
        [Fact]
        public async Task RekeyAsync_NullConnection_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.RekeyAsync(null!, "new-key");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .Where(e => e.ParamName == "connection");
        }

        /// <summary>
        /// Tests that RekeyAsync throws an ArgumentException when the new encryption key is null, empty, or whitespace.
        /// </summary>
        /// <param name="newKey">The new encryption key to test (null, empty, or whitespace).</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RekeyAsync_InvalidNewKey_ThrowsArgumentException(string newKey)
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var connStr = new SQLiteConnectionStringBuilder
            {
                DataSource = tempFile,
                Version = 3
            }.ToString();

            await using var connection = new SQLiteConnection(connStr);
            await connection.OpenAsync();

            // Act
            Func<Task> act = async () => await SqlCipherConnectionBuilder.RekeyAsync(connection, newKey);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*New encryption key cannot be empty*")
                .Where(e => e.ParamName == "newKey");

            await connection.CloseAsync();
            File.Delete(tempFile);
        }

        #endregion
    }
}
