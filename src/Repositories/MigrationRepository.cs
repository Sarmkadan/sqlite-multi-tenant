#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using System.Data;
using System.Data.SQLite;

namespace SqliteMultiTenant.Repositories;

/// <summary>
/// SQLite implementation of the migration repository
/// </summary>
public sealed class MigrationRepository : IMigrationRepository {
    private readonly string _connectionString;
    private readonly ILogger<MigrationRepository> _logger;

    public MigrationRepository(string connectionString, ILogger<MigrationRepository> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDatabase();
    }

    public async Task<Migration?> GetByIdAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE MigrationId = @MigrationId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@MigrationId", migrationId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapMigration(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving migration {migrationId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Migration>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE DatabaseId = @DatabaseId
                ORDER BY ExecutionOrder ASC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var migrations = new List<Migration>();
            while (reader.Read())
            {
                migrations.Add(MapMigration(reader));
            }
            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving migrations for database {databaseId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE DatabaseId = @DatabaseId AND Status = @Status
                ORDER BY ExecutionOrder ASC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Status", (int)MigrationStatus.Pending);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var migrations = new List<Migration>();
            while (reader.Read())
            {
                migrations.Add(MapMigration(reader));
            }
            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving pending migrations: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE DatabaseId = @DatabaseId AND Status = @Status
                ORDER BY ExecutionOrder ASC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Status", (int)MigrationStatus.Completed);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var migrations = new List<Migration>();
            while (reader.Read())
            {
                migrations.Add(MapMigration(reader));
            }
            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving applied migrations: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE DatabaseId = @DatabaseId AND Status = @Status
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Status", (int)MigrationStatus.Failed);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var migrations = new List<Migration>();
            while (reader.Read())
            {
                migrations.Add(MapMigration(reader));
            }
            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving failed migrations: {ex.Message}");
            throw;
        }
    }

    public async Task<Migration?> GetByVersionAsync(string databaseId, string version, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable
                FROM Migrations
                WHERE DatabaseId = @DatabaseId AND Version = @Version";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Version", version);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapMigration(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving migration by version: {ex.Message}");
            throw;
        }
    }

    public async Task<Migration> AddAsync(Migration migration, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!migration.Validate(out var errors))
                throw new ArgumentException($"Migration validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO Migrations (MigrationId, DatabaseId, Version, Name, Description, UpScript, DownScript,
                                       Status, CreatedAt, ExecutedAt, CompletedAt, RolledBackAt, ExecutedBy,
                                       ErrorMessage, ExecutionTimeMs, ExecutionOrder, IsRollbackable)
                VALUES (@MigrationId, @DatabaseId, @Version, @Name, @Description, @UpScript, @DownScript,
                        @Status, @CreatedAt, @ExecutedAt, @CompletedAt, @RolledBackAt, @ExecutedBy,
                        @ErrorMessage, @ExecutionTimeMs, @ExecutionOrder, @IsRollbackable)";

            using var command = new SQLiteCommand(query, connection);
            AddMigrationParameters(command, migration);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return migration;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding migration: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Migration migration, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!migration.Validate(out var errors))
                throw new ArgumentException($"Migration validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                UPDATE Migrations
                SET DatabaseId = @DatabaseId, Version = @Version, Name = @Name, Description = @Description,
                    UpScript = @UpScript, DownScript = @DownScript, Status = @Status, ExecutedAt = @ExecutedAt,
                    CompletedAt = @CompletedAt, RolledBackAt = @RolledBackAt, ExecutedBy = @ExecutedBy,
                    ErrorMessage = @ErrorMessage, ExecutionTimeMs = @ExecutionTimeMs, IsRollbackable = @IsRollbackable
                WHERE MigrationId = @MigrationId";

            using var command = new SQLiteCommand(query, connection);
            AddMigrationParameters(command, migration);
            command.Parameters.AddWithValue("@MigrationId", migration.MigrationId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating migration: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "DELETE FROM Migrations WHERE MigrationId = @MigrationId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@MigrationId", migrationId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting migration: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Migrations WHERE MigrationId = @MigrationId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@MigrationId", migrationId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && (long)result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking migration existence: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Migrations WHERE DatabaseId = @DatabaseId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null ? (int)(long)result : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting migration count: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Migration>> GetOrderedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        return await GetByDatabaseAsync(databaseId, cancellationToken);
    }

    private Migration MapMigration(SQLiteDataReader reader)
    {
        return new Migration
        {
            MigrationId = reader.GetString(0),
            DatabaseId = reader.GetString(1),
            Version = reader.GetString(2),
            Name = reader.GetString(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            UpScript = reader.GetString(5),
            DownScript = reader.IsDBNull(6) ? null : reader.GetString(6),
            Status = (MigrationStatus)reader.GetInt32(7),
            CreatedAt = reader.GetDateTime(8),
            ExecutedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            CompletedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            RolledBackAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            ExecutedBy = reader.IsDBNull(12) ? null : reader.GetString(12),
            ErrorMessage = reader.IsDBNull(13) ? null : reader.GetString(13),
            ExecutionTimeMs = reader.GetInt64(14),
            ExecutionOrder = reader.GetInt32(15),
            IsRollbackable = reader.GetBoolean(16)
        };
    }

    private void AddMigrationParameters(SQLiteCommand command, Migration migration)
    {
        command.Parameters.AddWithValue("@MigrationId", migration.MigrationId);
        command.Parameters.AddWithValue("@DatabaseId", migration.DatabaseId);
        command.Parameters.AddWithValue("@Version", migration.Version);
        command.Parameters.AddWithValue("@Name", migration.Name);
        command.Parameters.AddWithValue("@Description", migration.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@UpScript", migration.UpScript);
        command.Parameters.AddWithValue("@DownScript", migration.DownScript ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", (int)migration.Status);
        command.Parameters.AddWithValue("@CreatedAt", migration.CreatedAt);
        command.Parameters.AddWithValue("@ExecutedAt", migration.ExecutedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAt", migration.CompletedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RolledBackAt", migration.RolledBackAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ExecutedBy", migration.ExecutedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", migration.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ExecutionTimeMs", migration.ExecutionTimeMs);
        command.Parameters.AddWithValue("@ExecutionOrder", migration.ExecutionOrder);
        command.Parameters.AddWithValue("@IsRollbackable", migration.IsRollbackable);
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Migrations (
                    MigrationId TEXT PRIMARY KEY,
                    DatabaseId TEXT NOT NULL,
                    Version TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    UpScript TEXT NOT NULL,
                    DownScript TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    ExecutedAt TEXT,
                    CompletedAt TEXT,
                    RolledBackAt TEXT,
                    ExecutedBy TEXT,
                    ErrorMessage TEXT,
                    ExecutionTimeMs INTEGER DEFAULT 0,
                    ExecutionOrder INTEGER NOT NULL DEFAULT 0,
                    IsRollbackable INTEGER NOT NULL DEFAULT 1
                )";

            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();

            _logger.LogInformation("Migration repository database initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initializing migration repository database: {ex.Message}");
            throw;
        }
    }
}
