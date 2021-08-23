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
/// SQLite implementation of the tenant repository
/// </summary>
public sealed class TenantRepository : ITenantRepository {
    private readonly string _connectionString;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(string connectionString, ILogger<TenantRepository> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDatabase();
    }

    public async Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                WHERE TenantId = @TenantId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapTenant(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                WHERE Name = @Name";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Name", name);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            return reader.Read() ? MapTenant(reader) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving tenant by name {Name}: {Message}", name, ex.Message);
            throw;
        }
    }

    public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var tenants = new List<Tenant>();
            while (reader.Read())
            {
                tenants.Add(MapTenant(reader));
            }
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving all tenants: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync((int)TenantStatus.Active, cancellationToken);
    }

    public async Task<List<Tenant>> GetByStatusAsync(int status, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                WHERE Status = @Status
                ORDER BY CreatedAt DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Status", status);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var tenants = new List<Tenant>();
            while (reader.Read())
            {
                tenants.Add(MapTenant(reader));
            }
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving tenants by status {Status}: {Message}", status, ex.Message);
            throw;
        }
    }

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!tenant.Validate(out var errors))
                throw new ArgumentException($"Tenant validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO Tenants (TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                                   LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata)
                VALUES (@TenantId, @Name, @Description, @Status, @CreatedAt, @UpdatedAt,
                        @LastAccessedAt, @ContactEmail, @DatabasePath, @IsDataIsolated, @MaxConnections, @Metadata)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenant.TenantId);
            command.Parameters.AddWithValue("@Name", tenant.Name);
            command.Parameters.AddWithValue("@Description", tenant.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)tenant.Status);
            command.Parameters.AddWithValue("@CreatedAt", tenant.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", tenant.UpdatedAt);
            command.Parameters.AddWithValue("@LastAccessedAt", tenant.LastAccessedAt ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ContactEmail", tenant.ContactEmail ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DatabasePath", tenant.DatabasePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsDataIsolated", tenant.IsDataIsolated);
            command.Parameters.AddWithValue("@MaxConnections", tenant.MaxConnections);
            command.Parameters.AddWithValue("@Metadata", tenant.Metadata is not null ? System.Text.Json.JsonSerializer.Serialize(tenant.Metadata) : (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding tenant: {Message}", ex.Message);
            throw;
        }
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!tenant.Validate(out var errors))
                throw new ArgumentException($"Tenant validation failed: {string.Join(", ", errors)}");

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                UPDATE Tenants
                SET Name = @Name, Description = @Description, Status = @Status,
                    UpdatedAt = @UpdatedAt, LastAccessedAt = @LastAccessedAt, ContactEmail = @ContactEmail,
                    DatabasePath = @DatabasePath, IsDataIsolated = @IsDataIsolated,
                    MaxConnections = @MaxConnections, Metadata = @Metadata
                WHERE TenantId = @TenantId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenant.TenantId);
            command.Parameters.AddWithValue("@Name", tenant.Name);
            command.Parameters.AddWithValue("@Description", tenant.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)tenant.Status);
            command.Parameters.AddWithValue("@UpdatedAt", tenant.UpdatedAt);
            command.Parameters.AddWithValue("@LastAccessedAt", tenant.LastAccessedAt ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ContactEmail", tenant.ContactEmail ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DatabasePath", tenant.DatabasePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsDataIsolated", tenant.IsDataIsolated);
            command.Parameters.AddWithValue("@MaxConnections", tenant.MaxConnections);
            command.Parameters.AddWithValue("@Metadata", tenant.Metadata is not null ? System.Text.Json.JsonSerializer.Serialize(tenant.Metadata) : (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating tenant {TenantId}: {Message}", tenant.TenantId, ex.Message);
            throw;
        }
    }

    public async Task DeleteAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "DELETE FROM Tenants WHERE TenantId = @TenantId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Tenants WHERE TenantId = @TenantId";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && (long)result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking tenant existence {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT COUNT(*) FROM Tenants";

            using var command = new SQLiteCommand(query, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null ? (int)(long)result : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting tenant count: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Tenant>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                WHERE Name LIKE @SearchTerm OR ContactEmail LIKE @SearchTerm
                ORDER BY Name ASC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var tenants = new List<Tenant>();
            while (reader.Read())
            {
                tenants.Add(MapTenant(reader));
            }
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error searching tenants: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Tenant>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            int offset = (pageNumber - 1) * pageSize;
            const string query = @"
                SELECT TenantId, Name, Description, Status, CreatedAt, UpdatedAt,
                       LastAccessedAt, ContactEmail, DatabasePath, IsDataIsolated, MaxConnections, Metadata
                FROM Tenants
                ORDER BY CreatedAt DESC
                LIMIT @PageSize OFFSET @Offset";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@Offset", offset);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var tenants = new List<Tenant>();
            while (reader.Read())
            {
                tenants.Add(MapTenant(reader));
            }
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving paged tenants: {Message}", ex.Message);
            throw;
        }
    }

    private Tenant MapTenant(System.Data.Common.DbDataReader reader)
    {
        var tenant = new Tenant
        {
            TenantId = reader.GetString(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Status = (TenantStatus)reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5),
            LastAccessedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            ContactEmail = reader.IsDBNull(7) ? null : reader.GetString(7),
            DatabasePath = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsDataIsolated = reader.GetBoolean(9),
            MaxConnections = reader.GetInt32(10)
        };

        if (!reader.IsDBNull(11))
        {
            var metadataJson = reader.GetString(11);
            tenant.Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
        }

        return tenant;
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Tenants (
                    TenantId TEXT PRIMARY KEY,
                    Name TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    LastAccessedAt TEXT,
                    ContactEmail TEXT,
                    DatabasePath TEXT,
                    IsDataIsolated INTEGER NOT NULL DEFAULT 1,
                    MaxConnections INTEGER NOT NULL DEFAULT 10,
                    Metadata TEXT
                )";

            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();

            _logger.LogInformation("Tenant repository database initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error initializing tenant repository database: {Message}", ex.Message);
            throw;
        }
    }
}
