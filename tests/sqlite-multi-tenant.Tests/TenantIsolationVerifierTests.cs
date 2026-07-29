using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Tenants;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class TenantIsolationVerifierTests
    {
        private static SQLiteConnection CreateInMemoryConnection()
        {
            var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
            connection.Open();

            // Minimal schema required for the verifier
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Tenants (TenantId TEXT NOT NULL);
                    CREATE TABLE AuditLog (Id INTEGER PRIMARY KEY AUTOINCREMENT, TenantId TEXT NOT NULL, Action TEXT);
                ";
                cmd.ExecuteNonQuery();
            }

            return connection;
        }

        [Fact]
        public async Task VerifyTenantIsolationAsync_HappyPath_ReturnsIsolatedResult()
        {
            // Arrange
            using var connection = CreateInMemoryConnection();
            const string tenantId = "tenant-123";

            // Insert a matching audit log entry (no cross‑tenant rows)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO AuditLog (TenantId, Action) VALUES (@tid, 'login')";
                cmd.Parameters.AddWithValue("@tid", tenantId);
                cmd.ExecuteNonQuery();
            }

            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);

            // Act
            IsolationVerificationResult result = await verifier.VerifyTenantIsolationAsync(connection, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tenantId, result.TenantId);
            Assert.True(result.IsIsolated);
            Assert.True(result.AuditLogIsolationValid);
            Assert.True(result.ConnectionRestrictionValid);
            Assert.True(result.QueryIsolationValid);
            Assert.True(result.VerifiedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task VerifyTenantIsolationAsync_NullConnection_ThrowsArgumentNullException()
        {
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await verifier.VerifyTenantIsolationAsync(null!, "any"));
        }

        [Fact]
        public async Task VerifyTenantIsolationAsync_EmptyTenantId_ThrowsArgumentException()
        {
            using var connection = CreateInMemoryConnection();
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await verifier.VerifyTenantIsolationAsync(connection, string.Empty));
        }

        [Fact]
        public async Task DetectPotentialDataLeaksAsync_FindsMissingTenantColumnAndSensitiveColumn()
        {
            // Arrange
            using var connection = CreateInMemoryConnection();

            // Table without TenantId column
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE NoTenant (Id INTEGER PRIMARY KEY, Name TEXT)";
                cmd.ExecuteNonQuery();
            }

            // Table with a sensitive column name
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY, Username TEXT, Password TEXT)";
                cmd.ExecuteNonQuery();
            }

            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);

            // Act
            List<DataLeakageSuspicion> suspicions = await verifier.DetectPotentialDataLeaksAsync(connection);

            // Assert
            Assert.NotEmpty(suspicions);
            Assert.Contains(suspicions, s => s.Type == "MissingTenantContext" && s.Description.Contains("NoTenant"));
            Assert.Contains(suspicions, s => s.Type == "PotentiallyUnencryptedSensitiveData" && s.Description.Contains("password"));
        }

        [Fact]
        public async Task DetectPotentialDataLeaksAsync_NullConnection_ReturnsEmptyList()
        {
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            List<DataLeakageSuspicion> result = await verifier.DetectPotentialDataLeaksAsync(null!);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ValidateQueryTenantIsolationAsync_HappyPath_ReturnsSafeResult()
        {
            using var connection = CreateInMemoryConnection();
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            const string tenantId = "t1";
            const string query = "SELECT Id, Name FROM Users WHERE TenantId = @tid";

            QueryValidationResult result = await verifier.ValidateQueryTenantIsolationAsync(connection, query, tenantId);

            Assert.True(result.ContainsTenantFilter);
            Assert.False(result.ContainsWildcardSelect);
            Assert.True(result.IsParameterized);
            Assert.True(result.HasNoUnionBypass);
            Assert.True(result.HasNoSubqueryBypass);
            Assert.True(result.IsIsolationSafe);
        }

        [Fact]
        public async Task ValidateQueryTenantIsolationAsync_QueryMissingTenantFilter_ReturnsUnsafe()
        {
            using var connection = CreateInMemoryConnection();
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            const string tenantId = "t1";
            const string query = "SELECT * FROM Users";

            QueryValidationResult result = await verifier.ValidateQueryTenantIsolationAsync(connection, query, tenantId);

            Assert.False(result.ContainsTenantFilter);
            Assert.True(result.ContainsWildcardSelect);
            Assert.False(result.IsIsolationSafe);
        }

        [Fact]
        public async Task ValidateQueryTenantIsolationAsync_NullConnection_ThrowsArgumentNullException()
        {
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await verifier.ValidateQueryTenantIsolationAsync(null!, "SELECT 1", "t"));
        }

        [Fact]
        public async Task ValidateQueryTenantIsolationAsync_EmptyQuery_ThrowsArgumentException()
        {
            using var connection = CreateInMemoryConnection();
            var verifier = new TenantIsolationVerifier(NullLogger<TenantIsolationVerifier>.Instance);
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await verifier.ValidateQueryTenantIsolationAsync(connection, string.Empty, "t"));
        }
    }
}
