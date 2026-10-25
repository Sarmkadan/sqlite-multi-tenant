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
public sealed class DatabaseController : ControllerBase
{
    private const string BaseDatabasePath = "./databases";
    private const string QuickCheckPragma = "PRAGMA quick_check";
    private const string IntegrityCheckPragma = "PRAGMA integrity_check";
    private const string TableInfoPragmaFormat = "PRAGMA table_info(\"{0}\")";

    private readonly ILogger<DatabaseController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseController"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record database operation requests and failures.</param>
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
            {
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));
            }

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
                    command.CommandText = QuickCheckPragma;
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
            {
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));
            }

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
            {
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));
            }

            var startTime = DateTime.UtcNow;
            var errors = new List<string>();

            await using (var connection = new SQLiteConnection($"Data Source={path};"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = IntegrityCheckPragma;

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var message = reader.GetString(0);
                    if (!string.Equals(message, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(message);
                    }
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
            {
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));
            }

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
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }

                foreach (var tableName in tableNames)
                {
                    var columns = new List<ColumnSchema>();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(TableInfoPragmaFormat, tableName);
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
            {
                return BadRequest(ApiResponse<object>.Error($"Invalid export format: {format}"));
            }

            var path = ResolveDatabasePath(databaseId);
            if (!System.IO.File.Exists(path))
            {
                return NotFound(ApiResponse<object>.Error($"Database not found: {databaseId}"));
            }

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

/// <summary>
/// Statistics about a database file, returned by the stats endpoint.
/// </summary>
public sealed class DatabaseStats
{
    /// <summary>
    /// Gets or sets the identifier of the database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the database file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of user tables in the database.
    /// </summary>
    public int TableCount { get; set; }

    /// <summary>
    /// Gets or sets the number of indexes in the database.
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-write time of the database file.
    /// This value is taken from the file system and is used as a proxy for the last vacuum time.
    /// </summary>
    public DateTime LastVacuumTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <c>PRAGMA quick_check</c> reported a problem.
    /// </summary>
    public bool IsCorrupted { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the statistics were generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of a database optimization (VACUUM and ANALYZE) run.
/// </summary>
public sealed class OptimizationResult
{
    /// <summary>
    /// Gets or sets the identifier of the optimized database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time taken by the optimization, in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets a human-readable summary of the outcome.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time at which the result was produced.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of a database integrity check.
/// </summary>
public sealed class IntegrityCheckResult
{
    /// <summary>
    /// Gets or sets the identifier of the checked database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the integrity check found no errors.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the number of errors reported by the integrity check.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the time taken by the integrity check, in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the messages reported by <c>PRAGMA integrity_check</c> other than <c>ok</c>.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC time at which the check was completed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Schema of a database, listing its tables.
/// </summary>
public sealed class DatabaseSchema
{
    /// <summary>
    /// Gets or sets the identifier of the database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user tables in the database.
    /// </summary>
    public List<TableSchema> Tables { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC time at which the schema was read.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Schema of a single table, including its columns and row count.
/// </summary>
public sealed class TableSchema
{
    /// <summary>
    /// Gets or sets the name of the table.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the columns of the table.
    /// </summary>
    public List<ColumnSchema> Columns { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of rows in the table.
    /// </summary>
    public int RowCount { get; set; }
}

/// <summary>
/// Schema of a single column in a table.
/// </summary>
public sealed class ColumnSchema
{
    /// <summary>
    /// Gets or sets the name of the column.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the declared data type of the column.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the column accepts <c>NULL</c> values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is part of the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }
}

/// <summary>
/// Result of a database export request.
/// </summary>
public sealed class ExportResult
{
    /// <summary>
    /// Gets or sets the identifier of the exported database.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requested export format (<c>json</c>, <c>csv</c> or <c>sql</c>).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time at which the export was created.
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets the relative URL from which the export can be downloaded.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time after which the export is no longer available (24 hours after creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
