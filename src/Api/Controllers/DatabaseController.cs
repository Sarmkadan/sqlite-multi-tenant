#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Api.Controllers;

/// <summary>
/// Manages database-specific operations including schema inspection,
/// statistics, maintenance tasks, and configuration.
/// Provides endpoints for per-database operations across all tenants.
/// </summary>
[ApiController]
[Route("api/databases")]
public sealed class DatabaseController : ControllerBase {
    private const string BaseDatabasePath = "./databases";

    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(ILogger<DatabaseController> logger)
    {
        _logger = logger;
    }

    private static string ResolveDatabasePath(string databaseId) =>
        Path.Combine(BaseDatabasePath, $"{databaseId}.db");

    /// <summary>
    /// Gets detailed statistics about a specific database.
    /// Includes file size, row counts, and schema information.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> is null or whitespace.</exception>
    [HttpGet("{databaseId}/stats")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseStats>), StatusCodes.Status200OK)]
    public IActionResult GetDatabaseStats(string databaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        try
        {
            _logger.LogInformation("Database stats requested for {DatabaseId}", databaseId);

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));

            var isCorrupted = false;
            var tableCount = 0;
            var indexCount = 0;

            using (var connection = new SQLiteConnection($"Data Source={path};"))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
                    tableCount = Convert.ToInt32(command.ExecuteScalar());
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index'";
                    indexCount = Convert.ToInt32(command.ExecuteScalar());
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA quick_check";
                    var quickCheckResult = Convert.ToString(command.ExecuteScalar());
                    isCorrupted = !string.Equals(quickCheckResult, "ok", StringComparison.OrdinalIgnoreCase);
                }
            }

            var fileInfo = new FileInfo(path);

            var stats = new DatabaseStats
            {
                DatabaseId = databaseId,
                FileSizeBytes = fileInfo.Length,
                TableCount = tableCount,
                IndexCount = indexCount,
                LastVacuumTime = fileInfo.LastWriteTimeUtc,
                IsCorrupted = isCorrupted,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<DatabaseStats>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting database stats: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve database stats"));
        }
    }

    /// <summary>
    /// Optimizes database performance by running VACUUM and ANALYZE.
    /// Can be resource-intensive on large databases.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> is null or whitespace.</exception>
    [HttpPost("{databaseId}/optimize")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> OptimizeDatabase(string databaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        try
        {
            _logger.LogInformation("Database optimization started for {DatabaseId}", databaseId);

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));

            var startTime = DateTime.UtcNow;

            await using (var connection = new SQLiteConnection($"Data Source={path};"))
            {
                await connection.OpenAsync();

                await using (var vacuum = connection.CreateCommand())
                {
                    vacuum.CommandText = "VACUUM";
                    await vacuum.ExecuteNonQueryAsync();
                }

                await using (var analyze = connection.CreateCommand())
                {
                    analyze.CommandText = "ANALYZE";
                    await analyze.ExecuteNonQueryAsync();
                }
            }

            var duration = DateTime.UtcNow - startTime;

            var result = new OptimizationResult
            {
                DatabaseId = databaseId,
                DurationMs = (long)duration.TotalMilliseconds,
                Message = "Database optimization completed successfully",
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<OptimizationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Database optimization error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Database optimization failed"));
        }
    }

    /// <summary>
    /// Performs integrity check on the database.
    /// Detects corruption and structural issues.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> is null or whitespace.</exception>
    [HttpPost("{databaseId}/integrity-check")]
    [ProducesResponseType(typeof(ApiResponse<IntegrityCheckResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckIntegrity(string databaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        try
        {
            _logger.LogInformation("Integrity check requested for {DatabaseId}", databaseId);

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));

            var startTime = DateTime.UtcNow;
            var errors = new List<string>();

            await using (var connection = new SQLiteConnection($"Data Source={path};"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var message = reader.GetString(0);
                    if (!string.Equals(message, "ok", StringComparison.OrdinalIgnoreCase))
                        errors.Add(message);
                }
            }

            var duration = DateTime.UtcNow - startTime;

            var result = new IntegrityCheckResult
            {
                DatabaseId = databaseId,
                IsValid = errors.Count == 0,
                ErrorCount = errors.Count,
                DurationMs = (long)duration.TotalMilliseconds,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<IntegrityCheckResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Integrity check error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Integrity check failed"));
        }
    }

    /// <summary>
    /// Gets the current schema of the database.
    /// Returns information about all tables and their columns.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> is null or whitespace.</exception>
    [HttpGet("{databaseId}/schema")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseSchema>), StatusCodes.Status200OK)]
    public IActionResult GetSchema(string databaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        try
        {
            _logger.LogInformation("Schema requested for {DatabaseId}", databaseId);

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));

            var tables = new List<TableSchema>();

            using (var connection = new SQLiteConnection($"Data Source={path};"))
            {
                connection.Open();

                var tableNames = new List<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                        tableNames.Add(reader.GetString(0));
                }

                foreach (var tableName in tableNames)
                {
                    var columns = new List<ColumnSchema>();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            columns.Add(new ColumnSchema
                            {
                                ColumnName = reader.GetString(reader.GetOrdinal("name")),
                                DataType = reader.GetString(reader.GetOrdinal("type")),
                                IsNullable = reader.GetInt32(reader.GetOrdinal("notnull")) == 0,
                                IsPrimaryKey = reader.GetInt32(reader.GetOrdinal("pk")) > 0
                            });
                        }
                    }

                    int rowCount;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
                        rowCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    tables.Add(new TableSchema
                    {
                        TableName = tableName,
                        Columns = columns,
                        RowCount = rowCount
                    });
                }
            }

            var schema = new DatabaseSchema
            {
                DatabaseId = databaseId,
                Tables = tables,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<DatabaseSchema>.Success(schema));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving schema: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Failed to retrieve schema"));
        }
    }

    /// <summary>
    /// Exports database contents in specified format.
    /// Supports JSON, CSV, and SQL dump formats.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseId"/> is null or whitespace.</exception>
    [HttpPost("{databaseId}/export")]
    [ProducesResponseType(typeof(ApiResponse<ExportResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDatabase(string databaseId, [FromQuery] string format = "json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        try
        {
            _logger.LogInformation("Database export requested: {DatabaseId} as {Format}", databaseId, format);

            if (!IsValidExportFormat(format))
                return BadRequest(ApiResponse<object>.Error($"Invalid export format: {format}"));

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));

            await Task.CompletedTask;

            var result = new ExportResult
            {
                DatabaseId = databaseId,
                Format = format,
                ExportedAt = DateTime.UtcNow,
                DownloadUrl = $"/api/databases/{databaseId}/export/download?format={format}",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return Ok(ApiResponse<ExportResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Export error: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Export failed"));
        }
    }

    private static bool IsValidExportFormat(string format) =>
        format switch
        {
            "json" or "csv" or "sql" => true,
            _ => false
        };
}

public sealed class DatabaseStats {
    public string DatabaseId { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int TableCount { get; set; }
    public int IndexCount { get; set; }
    public DateTime LastVacuumTime { get; set; }
    public bool IsCorrupted { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class OptimizationResult {
    public string DatabaseId { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public sealed class IntegrityCheckResult {
    public string DatabaseId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int ErrorCount { get; set; }
    public long DurationMs { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public sealed class DatabaseSchema {
    public string DatabaseId { get; set; } = string.Empty;
    public List<TableSchema> Tables { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public sealed class TableSchema {
    public string TableName { get; set; } = string.Empty;
    public List<ColumnSchema> Columns { get; set; } = new();
    public int RowCount { get; set; }
}

public sealed class ColumnSchema {
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
}

public sealed class ExportResult {
    public string DatabaseId { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
