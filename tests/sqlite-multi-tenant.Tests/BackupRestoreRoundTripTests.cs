#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Proves that a tenant database survives a full backup -> restore cycle with
    /// its data intact, and that the restored copy is an independent file (writes
    /// to the source after the backup do not appear in the restored database).
    /// </summary>
    public sealed class BackupRestoreRoundTripTests : IDisposable
    {
        private readonly List<string> _files = new();

        private string NewDbPath(string label)
        {
            var path = Path.Combine(Path.GetTempPath(), $"backup_rt_{label}_{Guid.NewGuid():N}.db");
            _files.Add(path);
            return path;
        }

        private static string Conn(string path) => $"Data Source={path};Version=3;";

        [Fact]
        public async Task BackupThenRestore_PreservesAllRows()
        {
            var sourcePath = NewDbPath("source");
            var backupPath = NewDbPath("backup");
            var restorePath = NewDbPath("restore");

            // Seed the source database.
            await CreateAndSeedAsync(Conn(sourcePath), new[]
            {
                (1, "tenant-a", "invoice-001"),
                (2, "tenant-a", "invoice-002"),
                (3, "tenant-b", "invoice-003"),
            });

            // Back the source up to a separate physical file.
            BackupDatabase(Conn(sourcePath), Conn(backupPath));

            // Restore by copying the backup image into a fresh restore file.
            BackupDatabase(Conn(backupPath), Conn(restorePath));

            var restored = await ReadAllAsync(Conn(restorePath));
            restored.Should().HaveCount(3);
            restored.Should().ContainKey(1).WhoseValue.Should().Be("invoice-001");
            restored.Should().ContainKey(3).WhoseValue.Should().Be("invoice-003");
        }

        [Fact]
        public async Task RestoredDatabase_IsIndependentOfSource()
        {
            var sourcePath = NewDbPath("src2");
            var backupPath = NewDbPath("bak2");

            await CreateAndSeedAsync(Conn(sourcePath), new[]
            {
                (1, "tenant-a", "original"),
            });

            BackupDatabase(Conn(sourcePath), Conn(backupPath));

            // Mutate the source AFTER the backup was taken.
            using (var conn = new SQLiteConnection(Conn(sourcePath)))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Documents SET Title = 'changed-after-backup' WHERE Id = 1";
                await cmd.ExecuteNonQueryAsync();
            }

            // The backup must still reflect the pre-mutation state.
            var backup = await ReadAllAsync(Conn(backupPath));
            backup[1].Should().Be("original");
        }

        [Fact]
        public async Task BackupPreservesTenantScoping()
        {
            var sourcePath = NewDbPath("src3");
            var restorePath = NewDbPath("rst3");

            await CreateAndSeedAsync(Conn(sourcePath), new[]
            {
                (1, "tenant-a", "a1"),
                (2, "tenant-b", "b1"),
                (3, "tenant-b", "b2"),
            });

            BackupDatabase(Conn(sourcePath), Conn(restorePath));

            // After restore, per-tenant filtering still isolates rows correctly.
            using var conn = new SQLiteConnection(Conn(restorePath));
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Documents WHERE TenantId = @tid";
            cmd.Parameters.AddWithValue("@tid", "tenant-b");
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            count.Should().Be(2);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Copies the entire contents of the source database into the destination
        /// using SQLite's online backup API. The destination file is created fresh.
        /// </summary>
        private static void BackupDatabase(string sourceConnectionString, string destinationConnectionString)
        {
            using var source = new SQLiteConnection(sourceConnectionString);
            using var destination = new SQLiteConnection(destinationConnectionString);
            source.Open();
            destination.Open();
            source.BackupDatabase(destination, "main", "main", -1, null, 0);
        }

        private static async Task CreateAndSeedAsync(string connectionString, IEnumerable<(int Id, string TenantId, string Title)> rows)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();

            using (var create = conn.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Documents (
                        Id INTEGER PRIMARY KEY,
                        TenantId TEXT NOT NULL,
                        Title TEXT NOT NULL
                    );";
                await create.ExecuteNonQueryAsync();
            }

            foreach (var row in rows)
            {
                using var insert = conn.CreateCommand();
                insert.CommandText = "INSERT INTO Documents (Id, TenantId, Title) VALUES (@id, @tid, @title)";
                insert.Parameters.AddWithValue("@id", row.Id);
                insert.Parameters.AddWithValue("@tid", row.TenantId);
                insert.Parameters.AddWithValue("@title", row.Title);
                await insert.ExecuteNonQueryAsync();
            }
        }

        private static async Task<Dictionary<int, string>> ReadAllAsync(string connectionString)
        {
            var result = new Dictionary<int, string>();
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title FROM Documents ORDER BY Id";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result[reader.GetInt32(0)] = reader.GetString(1);
            }
            return result;
        }

        /// <summary>
        /// Removes temporary database files created during the tests.
        /// </summary>
        public void Dispose()
        {
            foreach (var f in _files)
            {
                try
                {
                    if (File.Exists(f)) File.Delete(f);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }
    }
}
