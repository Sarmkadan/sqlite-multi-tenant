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
/// SQLite implementation of the backup repository
/// </summary>
public sealed class BackupRepository : IBackupRepository {
    private readonly string _connectionString;
    private readonly ILogger<BackupRepository> _logger;

    public BackupRepository(string connectionString, ILogger<BackupRepository> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDatabase();
    }

    public async Task<Backup?> GetByIdAsync(string backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE BackupId = @BackupId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@BackupId", backupId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapBackup(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving backup {BackupId}: {Message}", backupId, ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving backups for database {DatabaseId}: {Message}", databaseId, ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId AND Status = @Status
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Status", (int)BackupStatus.Completed);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving completed backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetVerifiedBackupsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId AND IsVerified = 1
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving verified backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetFailedBackupsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId AND Status = @Status
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@Status", (int)BackupStatus.Failed);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving failed backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId
                ORDER BY CreatedAt DESC
                LIMIT 1";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapBackup(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving latest backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Backup> AddAsync(Backup backup, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!backup.Validate(out var errors))
                throw new ArgumentException($"Backup validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO Backups (BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                                    VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                                    VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags)
                VALUES (@BackupId, @DatabaseId, @BackupPath, @BackupType, @Status, @CreatedAt, @CompletedAt,
                        @VerifiedAt, @SizeBytes, @OriginalSizeBytes, @CompressionRatio, @CreatedBy,
                        @VerifiedBy, @ErrorMessage, @DurationMs, @IsEncrypted, @IsVerified, @ExpiresAt, @Tags)";

            using var command = new SQLiteCommand(query, connection);
            AddBackupParameters(command, backup);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return backup;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task UpdateAsync(Backup backup, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!backup.Validate(out var errors))
                throw new ArgumentException($"Backup validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                UPDATE Backups
                SET DatabaseId = @DatabaseId, BackupPath = @BackupPath, BackupType = @BackupType,
                    Status = @Status, CompletedAt = @CompletedAt, VerifiedAt = @VerifiedAt,
                    SizeBytes = @SizeBytes, OriginalSizeBytes = @OriginalSizeBytes,
                    CompressionRatio = @CompressionRatio, CreatedBy = @CreatedBy, VerifiedBy = @VerifiedBy,
                    ErrorMessage = @ErrorMessage, DurationMs = @DurationMs, IsEncrypted = @IsEncrypted,
                    IsVerified = @IsVerified, ExpiresAt = @ExpiresAt, Tags = @Tags
                WHERE BackupId = @BackupId";

            using var command = new SQLiteCommand(query, connection);
            AddBackupParameters(command, backup);
            command.Parameters.AddWithValue("@BackupId", backup.BackupId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task DeleteAsync(string backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "DELETE FROM Backups WHERE BackupId = @BackupId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@BackupId", backupId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Backups WHERE BackupId = @BackupId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@BackupId", backupId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && (long)result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking backup existence: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Backups WHERE DatabaseId = @DatabaseId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null ? (int)(long)result : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting backup count: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE ExpiresAt IS NOT NULL AND ExpiresAt < @Now
                ORDER BY ExpiresAt ASC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Now", DateTime.UtcNow);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving expired backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetPagedAsync(string databaseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            int offset = (pageNumber - 1) * pageSize;
            const string query = @"
                SELECT BackupId, DatabaseId, BackupPath, BackupType, Status, CreatedAt, CompletedAt,
                       VerifiedAt, SizeBytes, OriginalSizeBytes, CompressionRatio, CreatedBy,
                       VerifiedBy, ErrorMessage, DurationMs, IsEncrypted, IsVerified, ExpiresAt, Tags
                FROM Backups
                WHERE DatabaseId = @DatabaseId
                ORDER BY CreatedAt DESC
                LIMIT @PageSize OFFSET @Offset";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseId", databaseId);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@Offset", offset);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var backups = new List<Backup>();
            while (reader.Read())
            {
                backups.Add(MapBackup(reader));
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving paged backups: {Message}", ex.Message);
            throw;
        }
    }

    private Backup MapBackup(System.Data.Common.DbDataReader reader)
    {
        return new Backup
        {
            BackupId = reader.GetString(0),
            DatabaseId = reader.GetString(1),
            BackupPath = reader.GetString(2),
            BackupType = (BackupType)reader.GetInt32(3),
            Status = (BackupStatus)reader.GetInt32(4),
            CreatedAt = reader.GetDateTime(5),
            CompletedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            VerifiedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            SizeBytes = reader.GetInt64(8),
            OriginalSizeBytes = reader.GetInt64(9),
            CompressionRatio = reader.GetInt32(10),
            CreatedBy = reader.IsDBNull(11) ? null : reader.GetString(11),
            VerifiedBy = reader.IsDBNull(12) ? null : reader.GetString(12),
            ErrorMessage = reader.IsDBNull(13) ? null : reader.GetString(13),
            DurationMs = reader.GetInt64(14),
            IsEncrypted = reader.GetBoolean(15),
            IsVerified = reader.GetBoolean(16),
            ExpiresAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
            Tags = reader.IsDBNull(18) ? null : reader.GetString(18)
        };
    }

    private void AddBackupParameters(SQLiteCommand command, Backup backup)
    {
        command.Parameters.AddWithValue("@BackupId", backup.BackupId);
        command.Parameters.AddWithValue("@DatabaseId", backup.DatabaseId);
        command.Parameters.AddWithValue("@BackupPath", backup.BackupPath);
        command.Parameters.AddWithValue("@BackupType", (int)backup.BackupType);
        command.Parameters.AddWithValue("@Status", (int)backup.Status);
        command.Parameters.AddWithValue("@CreatedAt", backup.CreatedAt);
        command.Parameters.AddWithValue("@CompletedAt", backup.CompletedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VerifiedAt", backup.VerifiedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SizeBytes", backup.SizeBytes);
        command.Parameters.AddWithValue("@OriginalSizeBytes", backup.OriginalSizeBytes);
        command.Parameters.AddWithValue("@CompressionRatio", backup.CompressionRatio);
        command.Parameters.AddWithValue("@CreatedBy", backup.CreatedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VerifiedBy", backup.VerifiedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", backup.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DurationMs", backup.DurationMs);
        command.Parameters.AddWithValue("@IsEncrypted", backup.IsEncrypted);
        command.Parameters.AddWithValue("@IsVerified", backup.IsVerified);
        command.Parameters.AddWithValue("@ExpiresAt", backup.ExpiresAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Tags", backup.Tags ?? (object)DBNull.Value);
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Backups (
                    BackupId TEXT PRIMARY KEY,
                    DatabaseId TEXT NOT NULL,
                    BackupPath TEXT NOT NULL UNIQUE,
                    BackupType INTEGER NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    CompletedAt TEXT,
                    VerifiedAt TEXT,
                    SizeBytes INTEGER DEFAULT 0,
                    OriginalSizeBytes INTEGER DEFAULT 0,
                    CompressionRatio INTEGER DEFAULT 0,
                    CreatedBy TEXT,
                    VerifiedBy TEXT,
                    ErrorMessage TEXT,
                    DurationMs INTEGER DEFAULT 0,
                    IsEncrypted INTEGER NOT NULL DEFAULT 0,
                    IsVerified INTEGER NOT NULL DEFAULT 0,
                    ExpiresAt TEXT,
                    Tags TEXT
                )";

            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();

            _logger.LogInformation("Backup repository database initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error initializing backup repository database: {Message}", ex.Message);
            throw;
        }
    }
}
