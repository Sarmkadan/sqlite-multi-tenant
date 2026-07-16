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
    /// End-to-end tests proving that a tenant can never read another tenant's rows.
    /// Two isolation strategies are exercised:
    ///  - connection-per-tenant: each tenant lives in its own physical SQLite file;
    ///  - shared-schema: all tenants share one file and one table, separated by a
    ///    mandatory TenantId predicate.
    /// </summary>
    public sealed class TenantIsolationEnforcementTests : IDisposable
    {
        private readonly List<string> _files = new();

        private string NewDbPath(string label)
        {
            var path = Path.Combine(Path.GetTempPath(), $"iso_{label}_{Guid.NewGuid():N}.db");
            _files.Add(path);
            return path;
        }

        private static string Conn(string path) => $"Data Source={path};Version=3;";

        // -------------------------------------------------------------------------
        // Connection-per-tenant: physical file separation
        // -------------------------------------------------------------------------

        /// <summary>
        /// In connection-per-tenant mode a tenant's connection is bound to its own
        /// database file, so a query issued on tenant A's connection can only ever
        /// see tenant A's rows - tenant B's rows are physically in another file.
        /// </summary>
        [Fact]
        public async Task ConnectionPerTenant_TenantCannotSeeOtherTenantRows()
        {
            var pathA = NewDbPath("cpt_a");
            var pathB = NewDbPath("cpt_b");

            await CreateDocumentsTableAsync(Conn(pathA));
            await CreateDocumentsTableAsync(Conn(pathB));

            await InsertDocumentAsync(Conn(pathA), 1, "tenant-a", "A-secret-invoice");
            await InsertDocumentAsync(Conn(pathB), 1, "tenant-b", "B-secret-invoice");

            // Tenant A only ever holds a connection to its own file.
            var visibleToA = await ReadAllTitlesAsync(Conn(pathA));
            visibleToA.Should().ContainSingle().Which.Should().Be("A-secret-invoice");
            visibleToA.Should().NotContain("B-secret-invoice");

            // Tenant B likewise.
            var visibleToB = await ReadAllTitlesAsync(Conn(pathB));
            visibleToB.Should().ContainSingle().Which.Should().Be("B-secret-invoice");
            visibleToB.Should().NotContain("A-secret-invoice");
        }

        /// <summary>
        /// Even a deliberately hostile query on tenant A's connection cannot reach
        /// tenant B's data, because tenant B's table does not exist in A's file.
        /// </summary>
        [Fact]
        public async Task ConnectionPerTenant_ForeignTableIsNotReachable()
        {
            var pathA = NewDbPath("cpt_iso_a");
            var pathB = NewDbPath("cpt_iso_b");

            await CreateDocumentsTableAsync(Conn(pathA));
            // B keeps its data in a differently named table in a separate file.
            using (var connB = new SQLiteConnection(Conn(pathB)))
            {
                await connB.OpenAsync();
                using var cmd = connB.CreateCommand();
                cmd.CommandText = "CREATE TABLE SecretB (Id INTEGER PRIMARY KEY, Title TEXT);"
                                + "INSERT INTO SecretB (Id, Title) VALUES (1, 'B-only');";
                await cmd.ExecuteNonQueryAsync();
            }

            using var connA = new SQLiteConnection(Conn(pathA));
            await connA.OpenAsync();
            using var hostile = connA.CreateCommand();
            hostile.CommandText = "SELECT Title FROM SecretB";

            Func<Task> act = async () => await hostile.ExecuteScalarAsync();
            await act.Should().ThrowAsync<SQLiteException>();
        }

        // -------------------------------------------------------------------------
        // Shared-schema: single file, TenantId discriminator
        // -------------------------------------------------------------------------

        /// <summary>
        /// In shared-schema mode all tenants share one table. A repository that
        /// always scopes reads by TenantId must never return another tenant's rows.
        /// </summary>
        [Fact]
        public async Task SharedSchema_ScopedReadReturnsOnlyOwnRows()
        {
            var path = NewDbPath("shared");
            await CreateDocumentsTableAsync(Conn(path));

            await InsertDocumentAsync(Conn(path), 1, "tenant-a", "A-doc-1");
            await InsertDocumentAsync(Conn(path), 2, "tenant-a", "A-doc-2");
            await InsertDocumentAsync(Conn(path), 3, "tenant-b", "B-doc-1");

            var aRows = await ReadTitlesForTenantAsync(Conn(path), "tenant-a");
            aRows.Should().BeEquivalentTo(new[] { "A-doc-1", "A-doc-2" });
            aRows.Should().NotContain("B-doc-1");

            var bRows = await ReadTitlesForTenantAsync(Conn(path), "tenant-b");
            bRows.Should().ContainSingle().Which.Should().Be("B-doc-1");
        }

        /// <summary>
        /// Guards against the classic shared-schema leak: fetching a row by primary
        /// key without also constraining TenantId. The scoped lookup must return
        /// nothing when the row belongs to a different tenant, even though the key
        /// exists in the shared table.
        /// </summary>
        [Fact]
        public async Task SharedSchema_ByIdLookupIsTenantScoped()
        {
            var path = NewDbPath("shared_byid");
            await CreateDocumentsTableAsync(Conn(path));

            await InsertDocumentAsync(Conn(path), 42, "tenant-b", "B-private");

            // Tenant A asks for row id 42 (which really belongs to B) - scoped
            // query must refuse to hand it over.
            var leaked = await ReadTitleByIdForTenantAsync(Conn(path), 42, "tenant-a");
            leaked.Should().BeNull();

            // The owning tenant still gets its row.
            var owned = await ReadTitleByIdForTenantAsync(Conn(path), 42, "tenant-b");
            owned.Should().Be("B-private");
        }

        /// <summary>
        /// A scoped DELETE issued by one tenant must not touch another tenant's rows.
        /// </summary>
        [Fact]
        public async Task SharedSchema_ScopedDeleteDoesNotAffectOtherTenant()
        {
            var path = NewDbPath("shared_del");
            await CreateDocumentsTableAsync(Conn(path));

            await InsertDocumentAsync(Conn(path), 1, "tenant-a", "A-doc");
            await InsertDocumentAsync(Conn(path), 2, "tenant-b", "B-doc");

            using (var conn = new SQLiteConnection(Conn(path)))
            {
                await conn.OpenAsync();
                using var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM Documents WHERE TenantId = @tid";
                del.Parameters.AddWithValue("@tid", "tenant-a");
                var affected = await del.ExecuteNonQueryAsync();
                affected.Should().Be(1);
            }

            var bRows = await ReadTitlesForTenantAsync(Conn(path), "tenant-b");
            bRows.Should().ContainSingle().Which.Should().Be("B-doc");
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static async Task CreateDocumentsTableAsync(string connectionString)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INTEGER PRIMARY KEY,
                    TenantId TEXT NOT NULL,
                    Title TEXT NOT NULL
                );";
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task InsertDocumentAsync(string connectionString, int id, string tenantId, string title)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Documents (Id, TenantId, Title) VALUES (@id, @tid, @title)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@tid", tenantId);
            cmd.Parameters.AddWithValue("@title", title);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<List<string>> ReadAllTitlesAsync(string connectionString)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Title FROM Documents ORDER BY Id";
            return await ReadTitlesAsync(cmd);
        }

        private static async Task<List<string>> ReadTitlesForTenantAsync(string connectionString, string tenantId)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Title FROM Documents WHERE TenantId = @tid ORDER BY Id";
            cmd.Parameters.AddWithValue("@tid", tenantId);
            return await ReadTitlesAsync(cmd);
        }

        private static async Task<string?> ReadTitleByIdForTenantAsync(string connectionString, int id, string tenantId)
        {
            using var conn = new SQLiteConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Title FROM Documents WHERE Id = @id AND TenantId = @tid";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@tid", tenantId);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }

        private static async Task<List<string>> ReadTitlesAsync(SQLiteCommand cmd)
        {
            var titles = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                titles.Add(reader.GetString(0));
            }
            return titles;
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
