using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Operations;
using Xunit;

namespace sqlite_multi_tenant.Tests
{
    public class ConflictResolutionServiceTests
    {
        private readonly ConflictResolutionService _service;

        public ConflictResolutionServiceTests()
        {
            // Use a null logger – the service only logs, it doesn't require a real logger.
            _service = new ConflictResolutionService(NullLogger<ConflictResolutionService>.Instance);
        }

        [Fact]
        public void DetectConflicts_HappyPath_ReturnsExpectedConflicts()
        {
            // Arrange
            var local = new Dictionary<string, object>
            {
                ["Id"] = 1,
                ["Name"] = "Local",
                ["OnlyLocal"] = "L"
            };
            var remote = new Dictionary<string, object>
            {
                ["Id"] = 1,
                ["Name"] = "Remote",
                ["OnlyRemote"] = "R"
            };

            // Act
            var result = _service.DetectConflicts(local, remote);

            // Assert
            Assert.True(result.HasConflicts);
            Assert.Equal(3, result.Conflicts.Count);

            // Value difference
            var nameConflict = result.Conflicts.Find(c => c.Field == "Name");
            Assert.NotNull(nameConflict);
            Assert.Equal(ConflictType.ValueDifference, nameConflict.ConflictType);
            Assert.Equal("Local", nameConflict.LocalValue);
            Assert.Equal("Remote", nameConflict.RemoteValue);

            // Deleted remotely (present locally only)
            var onlyLocal = result.Conflicts.Find(c => c.Field == "OnlyLocal");
            Assert.NotNull(onlyLocal);
            Assert.Equal(ConflictType.DeletedRemotely, onlyLocal.ConflictType);
            Assert.Equal("L", onlyLocal.LocalValue);
            Assert.Null(onlyLocal.RemoteValue);

            // Created remotely (present remote only)
            var onlyRemote = result.Conflicts.Find(c => c.Field == "OnlyRemote");
            Assert.NotNull(onlyRemote);
            Assert.Equal(ConflictType.CreatedRemotely, onlyRemote.ConflictType);
            Assert.Null(onlyRemote.LocalValue);
            Assert.Equal("R", onlyRemote.RemoteValue);
        }

        [Fact]
        public void DetectConflicts_NullInputs_ReturnsEmptyResult()
        {
            // Act
            var result = _service.DetectConflicts(null, null);

            // Assert
            Assert.False(result.HasConflicts);
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public async Task ResolveConflictsAsync_PreferLocal_ReturnsLocalValues()
        {
            // Arrange
            var detection = new ConflictDetectionResult();
            detection.AddConflict(new DataConflict
            {
                Field = "Name",
                ConflictType = ConflictType.ValueDifference,
                LocalValue = "Local",
                RemoteValue = "Remote"
            });
            detection.AddConflict(new DataConflict
            {
                Field = "Count",
                ConflictType = ConflictType.ValueDifference,
                LocalValue = 10,
                RemoteValue = 20
            });

            // Act
            var result = await _service.ResolveConflictsAsync(detection, ConflictResolutionStrategy.PreferLocal);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Null(result.Error);
            Assert.Equal(2, result.ResolvedValues.Count);
            Assert.Equal("Local", result.ResolvedValues["Name"]);
            Assert.Equal(10, result.ResolvedValues["Count"]);
        }

        [Fact]
        public async Task ResolveConflictsAsync_EmptyConflicts_ReturnsEmptyResult()
        {
            // Arrange
            var emptyDetection = new ConflictDetectionResult();

            // Act
            var result = await _service.ResolveConflictsAsync(emptyDetection, ConflictResolutionStrategy.PreferRemote);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Null(result.Error);
            Assert.Empty(result.ResolvedValues);
        }

        [Fact]
        public async Task ApplyResolutionAsync_HappyPath_UpdatesDatabase()
        {
            // Arrange: create in‑memory SQLite DB with a simple table
            using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE TestTable (
                                        Id INTEGER PRIMARY KEY,
                                        Name TEXT,
                                        Count INTEGER
                                    );";
                cmd.ExecuteNonQuery();
            }

            // Insert a row to be updated
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO TestTable (Id, Name, Count) VALUES (1, 'Old', 5);";
                cmd.ExecuteNonQuery();
            }

            var resolution = new ConflictResolutionResult();
            resolution.ResolvedValues["Name"] = "New";
            resolution.ResolvedValues["Count"] = 42;

            // Act
            var success = await _service.ApplyResolutionAsync(connection, "TestTable", "Id", 1, resolution);

            // Assert
            Assert.True(success);

            using var query = connection.CreateCommand();
            query.CommandText = "SELECT Name, Count FROM TestTable WHERE Id = 1;";
            using var reader = query.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("New", reader.GetString(0));
            Assert.Equal(42, reader.GetInt32(1));
        }

        [Fact]
        public async Task ApplyResolutionAsync_InvalidParameters_ReturnsFalse()
        {
            // Arrange
            var resolution = new ConflictResolutionResult();
            resolution.ResolvedValues["Name"] = "X";

            // Act
            var result1 = await _service.ApplyResolutionAsync(null!, "Table", "Id", 1, resolution);
            var result2 = await _service.ApplyResolutionAsync(new SQLiteConnection(), "", "Id", 1, resolution);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
        }
    }
}
