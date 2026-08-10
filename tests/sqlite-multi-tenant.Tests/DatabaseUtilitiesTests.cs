using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests
{
    public class DatabaseUtilitiesTests
    {
        private const string TestDbPath = "test_database.db";

        public DatabaseUtilitiesTests()
        {
            // Clean up any existing test database before each test
            if (File.Exists(TestDbPath))
            {
                File.Delete(TestDbPath);
            }
        }

        [Fact]
        public async Task ConfigureOptimalSettingsAsync_ThrowsArgumentNullException_WhenConnectionIsNull()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DatabaseUtilities.ConfigureOptimalSettingsAsync(connection!));
        }

        [Fact]
        public async Task ConfigureOptimalSettingsAsync_ConfiguresSettingsSuccessfully_WhenConnectionIsProvided()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Act
            await DatabaseUtilities.ConfigureOptimalSettingsAsync(connection);

            // Assert - Connection should still be open and usable
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }

        [Fact]
        public void GetDatabaseSize_ThrowsArgumentException_WhenDatabasePathIsNullOrEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => DatabaseUtilities.GetDatabaseSize(null!));
            Assert.Throws<ArgumentException>(() => DatabaseUtilities.GetDatabaseSize(string.Empty));
        }

        [Fact]
        public void GetDatabaseSize_ReturnsCorrectSize_WhenDatabaseFileExists()
        {
            // Arrange
            File.WriteAllText(TestDbPath, new string('x', 1024)); // 1KB file

            // Act
            var size = DatabaseUtilities.GetDatabaseSize(TestDbPath);

            // Assert
            Assert.Equal(1024, size);
        }

        [Fact]
        public void GetDatabaseSize_ReturnsZero_WhenDatabaseFileDoesNotExist()
        {
            // Act
            var size = DatabaseUtilities.GetDatabaseSize("nonexistent.db");

            // Assert
            Assert.Equal(0, size);
        }

        [Fact]
        public void GetDatabaseSizeFormatted_ReturnsFormattedString_ForVariousSizes()
        {
            // Act & Assert
            Assert.Equal("0 B", DatabaseUtilities.GetDatabaseSizeFormatted("nonexistent.db"));

            File.WriteAllText(TestDbPath, new string('x', 1023)); // 1023 bytes
            Assert.Equal("1023 B", DatabaseUtilities.GetDatabaseSizeFormatted(TestDbPath));

            File.WriteAllText(TestDbPath, new string('x', 1024)); // 1KB
            Assert.Equal("1 KB", DatabaseUtilities.GetDatabaseSizeFormatted(TestDbPath));

            File.WriteAllText(TestDbPath, new string('x', 1024 * 1024 * 2)); // 2MB
            Assert.Equal("2 MB", DatabaseUtilities.GetDatabaseSizeFormatted(TestDbPath));
        }

        [Fact]
        public async Task CompactDatabaseAsync_ThrowsArgumentNullException_WhenConnectionIsNull()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DatabaseUtilities.CompactDatabaseAsync(connection!));
        }

        [Fact]
        public async Task CompactDatabaseAsync_ExecutesSuccessfully_WhenConnectionIsProvided()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a table and insert some data to make the database non-empty
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS Test (Id INTEGER PRIMARY KEY, Name TEXT)";
            await createTableCmd.ExecuteNonQueryAsync();

            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Test (Name) VALUES ('Test1'), ('Test2')";
            await insertCmd.ExecuteNonQueryAsync();

            // Act
            await DatabaseUtilities.CompactDatabaseAsync(connection);

            // Assert - Connection should still be open
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }

        [Fact]
        public async Task AnalyzeQueryPerformanceAsync_ThrowsArgumentNullException_WhenConnectionIsNull()
        {
            // Arrange
            SQLiteConnection? connection = null;
            const string query = "SELECT 1";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DatabaseUtilities.AnalyzeQueryPerformanceAsync(connection!, query));
        }

        [Fact]
        public async Task AnalyzeQueryPerformanceAsync_ThrowsArgumentException_WhenQueryIsNullOrEmpty()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                DatabaseUtilities.AnalyzeQueryPerformanceAsync(connection, null!));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                DatabaseUtilities.AnalyzeQueryPerformanceAsync(connection, string.Empty));
        }

        [Fact]
        public async Task AnalyzeQueryPerformanceAsync_ExecutesSuccessfully_WhenParametersAreValid()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a simple table for testing
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS Test (Id INTEGER PRIMARY KEY, Name TEXT)";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            await DatabaseUtilities.AnalyzeQueryPerformanceAsync(connection, "SELECT * FROM Test");

            // Assert - No exception thrown means success
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }

        [Fact]
        public async Task GetDatabaseStatisticsAsync_ReturnsDefaultStatistics_WhenConnectionIsNull()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act
            var stats = await DatabaseUtilities.GetDatabaseStatisticsAsync(connection!);

            // Assert
            Assert.Equal(0L, stats.TableCount);
            Assert.Equal(0L, stats.IndexCount);
            Assert.Equal(0L, stats.PageCount);
            Assert.Equal(0L, stats.PageSize);
            Assert.Equal(0L, stats.EstimatedSize);
        }

        [Fact]
        public async Task GetDatabaseStatisticsAsync_ReturnsPopulatedStatistics_WhenConnectionIsValid()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create tables and indexes
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY,
                    UserId INTEGER,
                    Amount REAL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );
                CREATE INDEX IF NOT EXISTS IX_Orders_UserId ON Orders(UserId);
            ";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            var stats = await DatabaseUtilities.GetDatabaseStatisticsAsync(connection);

            // Assert
            Assert.Equal(2L, stats.TableCount); // Users and Orders tables
            Assert.Equal(2L, stats.IndexCount); // IX_Orders_UserId index + auto-created index for Email UNIQUE constraint
            Assert.True(stats.PageCount > 0);
            Assert.True(stats.PageSize > 0);
            Assert.Equal(stats.PageCount * stats.PageSize, stats.EstimatedSize);
        }

        [Fact]
        public async Task TableExistsAsync_ReturnsFalse_WhenConnectionIsNullOrTableNameIsEmpty()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act & Assert
            Assert.False(await DatabaseUtilities.TableExistsAsync(connection, "AnyTable"));
            Assert.False(await DatabaseUtilities.TableExistsAsync(new SQLiteConnection($"Data Source={TestDbPath};Version=3;"), string.Empty));
        }

        [Fact]
        public async Task TableExistsAsync_ReturnsFalse_WhenTableDoesNotExist()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Act
            var exists = await DatabaseUtilities.TableExistsAsync(connection, "NonExistentTable");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task TableExistsAsync_ReturnsTrue_WhenTableExists()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a table
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT)";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            var exists = await DatabaseUtilities.TableExistsAsync(connection, "TestTable");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ColumnExistsAsync_ReturnsFalse_WhenAnyParameterIsNullOrEmpty()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act & Assert
            Assert.False(await DatabaseUtilities.ColumnExistsAsync(connection, "Table", "Column"));
            Assert.False(await DatabaseUtilities.ColumnExistsAsync(new SQLiteConnection($"Data Source={TestDbPath};Version=3;"), string.Empty, "Column"));
            Assert.False(await DatabaseUtilities.ColumnExistsAsync(new SQLiteConnection($"Data Source={TestDbPath};Version=3;"), "Table", string.Empty));
        }

        [Fact]
        public async Task ColumnExistsAsync_ReturnsFalse_WhenColumnDoesNotExist()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a table without the test column
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT)";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            var exists = await DatabaseUtilities.ColumnExistsAsync(connection, "TestTable", "NonExistentColumn");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ColumnExistsAsync_ReturnsTrue_WhenColumnExists()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a table with the test column
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT, Age INTEGER)";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            var exists = await DatabaseUtilities.ColumnExistsAsync(connection, "TestTable", "Age");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task GetTableColumnsAsync_ReturnsEmptyList_WhenConnectionIsNullOrTableNameIsEmpty()
        {
            // Arrange
            SQLiteConnection? connection = null;

            // Act & Assert
            Assert.Empty(await DatabaseUtilities.GetTableColumnsAsync(connection, "AnyTable"));
            Assert.Empty(await DatabaseUtilities.GetTableColumnsAsync(new SQLiteConnection($"Data Source={TestDbPath};Version=3;"), string.Empty));
        }

        [Fact]
        public async Task GetTableColumnsAsync_ReturnsEmptyList_WhenTableDoesNotExist()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Act
            var columns = await DatabaseUtilities.GetTableColumnsAsync(connection, "NonExistentTable");

            // Assert
            Assert.Empty(columns);
        }

        [Fact]
        public async Task GetTableColumnsAsync_ReturnsColumnInformation_WhenTableExists()
        {
            // Arrange
            await using var connection = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
            await connection.OpenAsync();

            // Create a table with specific columns
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TestTable (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL DEFAULT 'Unknown',
                    Age INTEGER NULL,
                    Email TEXT UNIQUE
                );
            ";
            await createTableCmd.ExecuteNonQueryAsync();

            // Act
            var columns = await DatabaseUtilities.GetTableColumnsAsync(connection, "TestTable");

            // Assert
            Assert.Equal(4, columns.Count);

            // Check Id column
            var idColumn = columns.Find(c => c.Name == "Id");
            Assert.NotNull(idColumn);
            Assert.Equal("INTEGER", idColumn.Type);
            Assert.False(idColumn.NotNull); // PRIMARY KEY columns are nullable in SQLite's PRAGMA table_info
            Assert.Null(idColumn.DefaultValue);
            Assert.True(idColumn.PrimaryKey);

            // Check Name column
            var nameColumn = columns.Find(c => c.Name == "Name");
            Assert.NotNull(nameColumn);
            Assert.Equal("TEXT", nameColumn.Type);
            Assert.True(nameColumn.NotNull);
            Assert.Equal("'Unknown'", nameColumn.DefaultValue);
            Assert.False(nameColumn.PrimaryKey);

            // Check Age column
            var ageColumn = columns.Find(c => c.Name == "Age");
            Assert.NotNull(ageColumn);
            Assert.Equal("INTEGER", ageColumn.Type);
            Assert.False(ageColumn.NotNull);
            Assert.Null(ageColumn.DefaultValue);
            Assert.False(ageColumn.PrimaryKey);

            // Check Email column
            var emailColumn = columns.Find(c => c.Name == "Email");
            Assert.NotNull(emailColumn);
            Assert.Equal("TEXT", emailColumn.Type);
            Assert.False(emailColumn.NotNull);
            Assert.Null(emailColumn.DefaultValue);
            Assert.False(emailColumn.PrimaryKey);
        }
    }
}