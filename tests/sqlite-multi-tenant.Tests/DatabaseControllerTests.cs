using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Api.Controllers;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class DatabaseControllerTests : IDisposable
    {
        private const string TestDatabaseId = "testdb";
        private readonly string _databasesFolder;
        private readonly string _testDbPath;
        private readonly DatabaseController _controller;

        public DatabaseControllerTests()
        {
            // Ensure the relative ./databases folder exists
            _databasesFolder = Path.Combine(Directory.GetCurrentDirectory(), "databases");
            Directory.CreateDirectory(_databasesFolder);

            _testDbPath = Path.Combine(_databasesFolder, $"{TestDatabaseId}.db");
            CreateTestDatabase(_testDbPath);

            // Controller requires an ILogger – use the NullLogger implementation
            _controller = new DatabaseController(NullLogger<DatabaseController>.Instance);
        }

        private void CreateTestDatabase(string path)
        {
            // Create a simple SQLite file with one table and a few rows
            using var connection = new System.Data.SQLite.SQLiteConnection($"Data Source={path};");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Sample (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL
                );
                INSERT INTO Sample (Name) VALUES ('Alice'), ('Bob');
            ";
            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void GetDatabaseStats_HappyPath_ReturnsOkResult()
        {
            // Act
            IActionResult result = _controller.GetDatabaseStats(TestDatabaseId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // The controller wraps the payload in ApiResponse<T>.Success(...)
            // We only need to verify the inner payload type and its DatabaseId.
            var payload = okResult.Value.GetType().GetProperty("Data")?.GetValue(okResult.Value);
            Assert.NotNull(payload);
            var dbStats = Assert.IsType<DatabaseStats>(payload);
            Assert.Equal(TestDatabaseId, dbStats.DatabaseId);
        }

        [Fact]
        public void GetDatabaseStats_NullOrWhiteSpace_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _controller.GetDatabaseStats(null!));
            Assert.Throws<ArgumentException>(() => _controller.GetDatabaseStats(string.Empty));
            Assert.Throws<ArgumentException>(() => _controller.GetDatabaseStats("   "));
        }

        [Fact]
        public async Task OptimizeDatabase_HappyPath_ReturnsOkResult()
        {
            // Act
            IActionResult result = await _controller.OptimizeDatabase(TestDatabaseId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = okResult.Value.GetType().GetProperty("Data")?.GetValue(okResult.Value);
            Assert.NotNull(payload);
            var optResult = Assert.IsType<OptimizationResult>(payload);
            Assert.Equal(TestDatabaseId, optResult.DatabaseId);
            Assert.True(optResult.DurationMs > 0);
        }

        [Fact]
        public async Task OptimizeDatabase_NonExistingDatabase_ReturnsNotFound()
        {
            // Act
            IActionResult result = await _controller.OptimizeDatabase("nonexistent");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var error = notFoundResult.Value?.GetType().GetProperty("Error")?.GetValue(notFoundResult.Value);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task CheckIntegrity_HappyPath_ReturnsOkResult()
        {
            // Act
            IActionResult result = await _controller.CheckIntegrity(TestDatabaseId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = okResult.Value.GetType().GetProperty("Data")?.GetValue(okResult.Value);
            Assert.NotNull(payload);
            var integrity = Assert.IsType<IntegrityCheckResult>(payload);
            Assert.Equal(TestDatabaseId, integrity.DatabaseId);
            Assert.True(integrity.IsValid);
            Assert.Equal(0, integrity.ErrorCount);
        }

        [Fact]
        public void GetSchema_HappyPath_ReturnsOkResult()
        {
            // Act
            IActionResult result = _controller.GetSchema(TestDatabaseId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = okResult.Value.GetType().GetProperty("Data")?.GetValue(okResult.Value);
            Assert.NotNull(payload);
            var schema = Assert.IsType<DatabaseSchema>(payload);
            Assert.Equal(TestDatabaseId, schema.DatabaseId);
            Assert.NotEmpty(schema.Tables);
        }

        [Fact]
        public async Task ExportDatabase_InvalidFormat_ReturnsBadRequest()
        {
            // Act
            IActionResult result = await _controller.ExportDatabase(TestDatabaseId, "xml");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequest.Value?.GetType().GetProperty("Error")?.GetValue(badRequest.Value);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task ExportDatabase_HappyPath_ReturnsOkResult()
        {
            // Act
            IActionResult result = await _controller.ExportDatabase(TestDatabaseId, "json");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = okResult.Value.GetType().GetProperty("Data")?.GetValue(okResult.Value);
            Assert.NotNull(payload);
            var export = Assert.IsType<ExportResult>(payload);
            Assert.Equal(TestDatabaseId, export.DatabaseId);
            Assert.Equal("json", export.Format);
        }

        public void Dispose()
        {
            // Cleanup the test database file
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }

            // Optionally remove the databases folder if empty
            if (Directory.Exists(_databasesFolder) && Directory.GetFiles(_databasesFolder).Length == 0)
            {
                Directory.Delete(_databasesFolder);
            }
        }
    }
}
